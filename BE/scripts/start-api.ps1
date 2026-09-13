<#
.SYNOPSIS
  Starts Redis (if needed) then runs Backend.Api in Development.
#>
[CmdletBinding()]
param(
    [string]$LaunchProfile = "http"
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$apiProject = Join-Path $scriptDir "..\src\Api\Backend.Api.csproj"

& (Join-Path $scriptDir "ensure-redis.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Starting Backend.Api (profile: $LaunchProfile)"
dotnet run --project $apiProject --launch-profile $LaunchProfile
