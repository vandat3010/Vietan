<#
.SYNOPSIS
  Ensures local Redis is running for WEB_TLN (native Windows Redis preferred; Docker optional).

.DESCRIPTION
  Order:
  1) If localhost:6379 already accepts connections → done
  2) Start existing redis-server.exe if installed (winget redis-windows / PATH)
  3) Install via winget (taizod1024.redis-windows-fork) then start — no Docker required
  4) Fallback: Docker Compose container tln-redis (if Docker Desktop is available)
#>
[CmdletBinding()]
param(
    [string]$ComposeFile = "",
    [switch]$PreferDocker
)

$ErrorActionPreference = "Stop"

function Refresh-Path {
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("Path", "User")
}

function Test-RedisPing {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $iar = $tcp.BeginConnect("127.0.0.1", 6379, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne(1200, $false)
        if (-not $ok) { try { $tcp.Close() } catch {}; return $false }
        $connected = $tcp.Connected
        try { $tcp.Close() } catch {}
        return $connected
    } catch {
        return $false
    }
}

function Find-RedisServer {
    Refresh-Path
    $cmd = Get-Command redis-server -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $wingetRoot = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages"
    if (Test-Path $wingetRoot) {
        $found = Get-ChildItem -Path $wingetRoot -Recurse -Filter "redis-server.exe" -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
        if ($found) { return $found }
    }
    return $null
}

function Start-NativeRedis {
    $exe = Find-RedisServer
    if (-not $exe) { return $false }

    $dir = Split-Path $exe -Parent
    Write-Host "Starting native Redis: $exe"
    # Explicit bind/port avoids msys redis.conf quirks on Windows.
    Start-Process -FilePath $exe `
        -ArgumentList @("--port", "6379", "--bind", "127.0.0.1") `
        -WorkingDirectory $dir `
        -WindowStyle Hidden

    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        if (Test-RedisPing) { return $true }
        Start-Sleep -Milliseconds 400
    }
    return $false
}

function Install-NativeRedis {
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        Write-Host "winget not found; skip native install."
        return $false
    }

    Write-Host "Installing Redis for Windows via winget (no Docker)..."
    winget install -e --id taizod1024.redis-windows-fork --source winget `
        --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne -1978335189) {
        # -1978335189 often means already installed
        Write-Warning "winget install exited with code $LASTEXITCODE"
    }

    Refresh-Path
    return [bool](Find-RedisServer)
}

function Find-ComposeFile {
    param([string]$Hint)
    if ($Hint -and (Test-Path $Hint)) { return (Resolve-Path $Hint).Path }

    $here = $PSScriptRoot
    $candidates = @(
        (Join-Path $here "..\..\docker-compose.yml"),
        (Join-Path $here "..\docker-compose.yml"),
        (Join-Path (Get-Location) "docker-compose.yml")
    )
    foreach ($c in $candidates) {
        $full = [System.IO.Path]::GetFullPath($c)
        if (Test-Path $full) { return $full }
    }
    return $null
}

function Start-DockerRedis {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { return $false }
    docker info 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) { return $false }

    $compose = Find-ComposeFile -Hint $ComposeFile
    if (-not $compose) { return $false }

    Write-Host "Starting Redis via Docker Compose: $compose"
    $composeDir = Split-Path $compose -Parent
    Push-Location $composeDir
    try {
        docker compose -f $compose up -d redis
        if ($LASTEXITCODE -ne 0) { return $false }
    } finally {
        Pop-Location
    }

    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        if (Test-RedisPing) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

Write-Host "==> WEB_TLN ensure Redis"

if (Test-RedisPing) {
    Write-Host "Redis already running at localhost:6379"
    exit 0
}

if (-not $PreferDocker) {
    if (Start-NativeRedis) {
        Write-Host "Redis is ready at localhost:6379 (native)"
        exit 0
    }

    if (Install-NativeRedis) {
        if (Start-NativeRedis) {
            Write-Host "Redis is ready at localhost:6379 (native, freshly installed)"
            exit 0
        }
    }
}

if (Start-DockerRedis) {
    Write-Host "Redis is ready at localhost:6379 (Docker tln-redis)"
    exit 0
}

Write-Error @"
Could not start Redis.

Option A — native Windows (recommended, no Docker):
  winget install -e --id taizod1024.redis-windows-fork --source winget
  Then re-run: pwsh ./BE/scripts/ensure-redis.ps1

Option B — Docker:
  Start Docker Desktop, then: docker compose up -d
"@
exit 1
