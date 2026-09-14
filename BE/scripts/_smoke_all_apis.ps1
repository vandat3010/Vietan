$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5140'
$results = New-Object System.Collections.Generic.List[object]

function Add-Result($method, $path, $status, $note) {
  $results.Add([pscustomobject]@{ Method = $method; Path = $path; Status = $status; Note = $note })
}

function Invoke-Api {
  param(
    [string]$Method,
    [string]$Path,
    [hashtable]$Headers = @{},
    $Body = $null,
    [string]$ContentType = 'application/json',
    [switch]$RawBytes
  )
  $uri = "$base$Path"
  try {
    $params = @{
      Uri = $uri
      Method = $Method
      Headers = $Headers
      TimeoutSec = 60
      UseBasicParsing = $true
    }
    if ($null -ne $Body) {
      if ($Body -is [string]) { $params.Body = $Body } else { $params.Body = ($Body | ConvertTo-Json -Depth 8) }
      $params.ContentType = $ContentType
    }
    $resp = Invoke-WebRequest @params
    return @{ Ok = $true; Status = [int]$resp.StatusCode; Content = $resp.Content; Headers = $resp.Headers; Bytes = $resp.Content }
  } catch {
    $status = 0
    $content = $_.ErrorDetails.Message
    if ($_.Exception.Response) {
      $status = [int]$_.Exception.Response.StatusCode
      try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $content = $reader.ReadToEnd()
      } catch {}
    }
    return @{ Ok = $false; Status = $status; Content = $content; Error = $_.Exception.Message }
  }
}

# --- Auth ---
$anon = Invoke-Api GET '/api/v1/stations'
Add-Result 'GET' '/api/v1/stations (anon)' $anon.Status $(if ($anon.Status -eq 401) { 'OK expect 401' } else { 'UNEXPECTED' })

$health = Invoke-Api GET '/health'
Add-Result 'GET' '/health' $health.Status $(if ($health.Ok) { 'OK' } else { 'FAIL' })

$login = Invoke-Api POST '/api/v1/auth/login' -Body @{ username = 'tlhn62514'; password = 'Password@123' }
$loginJson = $null
$token = $null
$role = $null
if ($login.Ok) {
  $loginJson = $login.Content | ConvertFrom-Json
  $token = $loginJson.data.accessToken
  if (-not $token) { $token = $loginJson.accessToken }
  $role = $loginJson.data.role
  if (-not $role) { $role = $loginJson.role }
}
Add-Result 'POST' '/api/v1/auth/login' $login.Status $(if ($token) { "OK role=$role" } else { "FAIL $($login.Content)" })

if (-not $token) {
  $results | Format-Table -AutoSize | Out-String | Write-Output
  Write-Output 'ABORT: no token'
  exit 1
}

$H = @{ Authorization = "Bearer $token" }

# --- Core SCADA reads ---
$pathsGet = @(
  '/api/v1/auth/me',
  '/api/v1/stations?pageNumber=1&pageSize=5',
  '/api/v1/session-policy',
  '/api/v1/scada-users?pageNumber=1&pageSize=5',
  '/api/v1/app-settings',
  '/api/v1/app-settings/catalog',
  '/api/v1/plcs?pageNumber=1&pageSize=5',
  '/api/v1/devices?pageNumber=1&pageSize=5',
  '/api/v1/tags?pageNumber=1&pageSize=5',
  '/api/v1/mqtt-configs',
  '/api/v1/communication-configs',
  '/api/v1/history-profiles',
  '/api/v1/tag-history-configs?pageNumber=1&pageSize=5',
  '/api/v1/alarm-histories?pageNumber=1&pageSize=5',
  '/api/v1/alarm-histories/active?pageNumber=1&pageSize=5',
  '/api/v1/event-logs?pageNumber=1&pageSize=5',
  '/api/v1/user-activity-logs?pageNumber=1&pageSize=5',
  '/api/v1/system-audit-logs?pageNumber=1&pageSize=5',
  '/api/v1/licenses',
  '/api/v1/licenses/concurrent-users',
  '/api/v1/map-layers',
  '/api/v1/audit-logs?pageNumber=1&pageSize=5'
)

$stationId = $null
$deviceId = $null

foreach ($p in $pathsGet) {
  $r = Invoke-Api GET $p -Headers $H
  $note = if ($r.Ok) { 'OK' } else { "FAIL $($r.Content)".Substring(0, [Math]::Min(120, ("FAIL $($r.Content)").Length)) }
  Add-Result 'GET' $p $r.Status $note

  if ($p -like '/api/v1/stations?*' -and $r.Ok) {
    try {
      $j = $r.Content | ConvertFrom-Json
      $items = $j.data.items
      if (-not $items) { $items = $j.data }
      if ($items -is [array] -and $items.Count -gt 0) { $stationId = $items[0].id }
      elseif ($items.id) { $stationId = $items.id }
    } catch {}
  }
}

if (-not $stationId) { $stationId = 1 }

# Station nested
$stationGets = @(
  "/api/v1/stations/$stationId",
  "/api/v1/stations/$stationId/electrical",
  "/api/v1/stations/$stationId/schematic",
  "/api/v1/stations/$stationId/device-cards",
  "/api/v1/stations/$stationId/device-monitor",
  "/api/v1/stations/$stationId/reports/devices",
  "/api/v1/stations/$stationId/events/devices",
  "/api/v1/stations/$stationId/charts/devices",
  "/api/v1/stations/$stationId/alarms/active?pageNumber=1&pageSize=5"
)

foreach ($p in $stationGets) {
  $r = Invoke-Api GET $p -Headers $H
  $note = if ($r.Ok) { 'OK' } else { ("FAIL " + $r.Content).Substring(0, [Math]::Min(140, ("FAIL " + $r.Content).Length)) }
  Add-Result 'GET' $p $r.Status $note

  if ($p -like '*/reports/devices' -and $r.Ok) {
    try {
      $j = $r.Content | ConvertFrom-Json
      $devs = $j.data
      if ($devs -is [array]) {
        foreach ($d in $devs) {
          if ([int]$d.id -gt 0) { $deviceId = [int]$d.id; break }
        }
      }
    } catch {}
  }
}

if (-not $deviceId) { $deviceId = 1 }
$today = (Get-Date).ToString('yyyy-MM-dd')
$from = (Get-Date).AddDays(-7).ToString('yyyy-MM-dd')

# Reports / events / charts / exports
$moreGets = @(
  "/api/v1/stations/$stationId/reports/table?deviceId=$deviceId&reportDate=$today&startTime=00:00:00&endTime=23:59:59&pageNumber=1&pageSize=5",
  "/api/v1/stations/$stationId/events/history?category=status&fromDate=$from&toDate=$today&pageNumber=1&pageSize=5",
  "/api/v1/stations/$stationId/reports/water-levels?reportDate=$today&pageNumber=1&pageSize=5",
  "/api/v1/stations/$stationId/reports/pump-temperatures?reportDate=$today&pageNumber=1&pageSize=5"
)
foreach ($p in $moreGets) {
  $r = Invoke-Api GET $p -Headers $H
  $note = if ($r.Ok) { 'OK' } else { ("FAIL " + $r.Content).Substring(0, [Math]::Min(140, ("FAIL " + $r.Content).Length)) }
  Add-Result 'GET' $p.Split('?')[0] $r.Status $note
}

# Excel exports (binary)
$exports = @(
  "/api/v1/stations/$stationId/reports/table/export?deviceId=$deviceId&reportDate=$today&startTime=00:00:00&endTime=23:59:59",
  "/api/v1/stations/$stationId/events/history/export?category=status&fromDate=$from&toDate=$today",
  "/api/v1/stations/$stationId/alarms/active/export"
)
foreach ($p in $exports) {
  try {
    $resp = Invoke-WebRequest -Uri "$base$p" -Headers $H -Method GET -TimeoutSec 90 -UseBasicParsing
    $ct = $resp.Headers['Content-Type']
    $len = $resp.RawContentLength
    $okXlsx = ($ct -like '*spreadsheetml*' -or $ct -like '*octet-stream*') -and $len -gt 0
    Add-Result 'GET' ($p.Split('?')[0]) ([int]$resp.StatusCode) $(if ($okXlsx) { "OK excel bytes=$len" } else { "WARN ct=$ct len=$len" })
  } catch {
    $st = 0
    if ($_.Exception.Response) { $st = [int]$_.Exception.Response.StatusCode }
    Add-Result 'GET' ($p.Split('?')[0]) $st ("FAIL " + $_.Exception.Message)
  }
}

# Mutations
$sp = Invoke-Api GET '/api/v1/session-policy' -Headers $H
$idle = 10
if ($sp.Ok) {
  try { $idle = ($sp.Content | ConvertFrom-Json).data.idleTimeoutMinutes } catch {}
}
$putSp = Invoke-Api PUT '/api/v1/session-policy' -Headers $H -Body @{ idleTimeoutMinutes = [int]$idle }
Add-Result 'PUT' '/api/v1/session-policy' $putSp.Status $(if ($putSp.Ok) { 'OK' } else { 'FAIL' })

$putApp = Invoke-Api PUT "/api/v1/app-settings/by-key/session.idleTimeoutMinutes" -Headers $H -Body @{ settingValue = "$idle" }
Add-Result 'PUT' '/api/v1/app-settings/by-key/{key}' $putApp.Status $(if ($putApp.Ok) { 'OK' } else { ("FAIL " + $putApp.Content).Substring(0,[Math]::Min(100,("FAIL "+$putApp.Content).Length)) })

$stDetail = Invoke-Api GET "/api/v1/stations/$stationId" -Headers $H
$stName = 'Station'
if ($stDetail.Ok) {
  try { $stName = ($stDetail.Content | ConvertFrom-Json).data.name } catch {}
}
$putSt = Invoke-Api PUT "/api/v1/stations/$stationId" -Headers $H -Body @{ name = $stName; description = 'smoke-test' }
Add-Result 'PUT' "/api/v1/stations/{id}" $putSt.Status $(if ($putSt.Ok) { 'OK' } else { 'FAIL' })

# Client audit POST (allowed)
$clientOk = Invoke-Api POST '/api/v1/audit-logs' -Headers $H -Body @{
  category = 'system'
  eventType = 'ui-navigation'
  title = 'Smoke test'
  detail = 'API smoke'
}
Add-Result 'POST' '/api/v1/audit-logs (client ok)' $clientOk.Status $(if ($clientOk.Ok) { 'OK' } else { 'FAIL' })

# Client audit POST (backend-owned -> 403)
$clientDeny = Invoke-Api POST '/api/v1/audit-logs' -Headers $H -Body @{
  category = 'login'
  eventType = 'Login'
  title = 'spoof'
  detail = 'should fail'
}
Add-Result 'POST' '/api/v1/audit-logs (Login spoof)' $clientDeny.Status $(if ($clientDeny.Status -eq 403) { 'OK expect 403' } else { "got $($clientDeny.Status)" })

# Scada user update smoke (self-safe: update demo profile fields if exists)
$users = Invoke-Api GET '/api/v1/scada-users?pageNumber=1&pageSize=20' -Headers $H
$targetUserId = $null
if ($users.Ok) {
  try {
    $uj = $users.Content | ConvertFrom-Json
    foreach ($u in $uj.data.items) {
      if ($u.username -eq 'demo') { $targetUserId = $u.id; break }
    }
    if (-not $targetUserId -and $uj.data.items.Count -gt 0) {
      foreach ($u in $uj.data.items) {
        if ($u.username -ne 'tlhn62514') { $targetUserId = $u.id; break }
      }
    }
  } catch {}
}
if ($targetUserId) {
  $putUser = Invoke-Api PUT "/api/v1/scada-users/$targetUserId" -Headers $H -Body @{ description = 'smoke' }
  Add-Result 'PUT' '/api/v1/scada-users/{id}' $putUser.Status $(if ($putUser.Ok) { "OK id=$targetUserId" } else { 'FAIL' })
} else {
  Add-Result 'PUT' '/api/v1/scada-users/{id}' 0 'SKIP no target user'
}

# Alarm ack/clear if any active
$alarms = Invoke-Api GET "/api/v1/stations/$stationId/alarms/active?pageNumber=1&pageSize=1" -Headers $H
$alarmId = $null
if ($alarms.Ok) {
  try {
    $aj = $alarms.Content | ConvertFrom-Json
    if ($aj.data.items -and $aj.data.items.Count -gt 0) { $alarmId = $aj.data.items[0].id }
  } catch {}
}
if ($alarmId) {
  $ack = Invoke-Api POST "/api/v1/alarm-histories/$alarmId/acknowledge" -Headers $H -Body @{ note = 'smoke' }
  Add-Result 'POST' '/alarm-histories/{id}/acknowledge' $ack.Status $(if ($ack.Ok) { "OK id=$alarmId" } else { 'FAIL' })
  # do not clear in smoke to preserve data state unless already open - skip clear or clear carefully
  Add-Result 'POST' '/alarm-histories/{id}/clear' 0 'SKIP preserve open alarm'
} else {
  Add-Result 'POST' '/alarm-histories/{id}/acknowledge' 0 'SKIP no active alarm'
  Add-Result 'POST' '/alarm-histories/{id}/clear' 0 'SKIP no active alarm'
}

# Screen snapshot (may 400/404 depending on mapping)
$snap = Invoke-Api GET "/api/v1/screens/nguyen-ly/snapshot?stationId=$stationId" -Headers $H
Add-Result 'GET' '/api/v1/screens/{screen}/snapshot' $snap.Status $(if ($snap.Ok -or $snap.Status -in 400,404) { "OK/soft $($snap.Status)" } else { 'FAIL' })

# Summary
$okish = ($results | Where-Object { $_.Note -like 'OK*' -or $_.Note -like 'SKIP*' -or $_.Note -like 'WARN*' }).Count
$fail = ($results | Where-Object { $_.Note -like 'FAIL*' -or ($_.Note -like 'got*' -and $_.Note -notlike 'OK*') }).Count
$unexpected = ($results | Where-Object { $_.Note -like 'UNEXPECTED*' }).Count

Write-Output "=== SMOKE RESULTS stationId=$stationId deviceId=$deviceId ==="
$results | Format-Table -AutoSize | Out-String -Width 220 | Write-Output
Write-Output "TOTAL=$($results.Count) OKISH=$okish FAIL=$fail UNEXPECTED=$unexpected"

$results | ConvertTo-Csv -NoTypeInformation | Set-Content -Encoding utf8 "d:\WEB_TLN\BE\scripts\_smoke_api_results.csv"
