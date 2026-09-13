Add-Type -AssemblyName System.Xml.Linq
$tmp = Join-Path $env:TEMP 'tln-xlsx-inspect2'
$ns = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
$nsmgrPath = Join-Path $tmp 'xl\worksheets\sheet2.xml'

function Load-SharedStrings($path) {
  $doc = [xml](Get-Content $path -Encoding UTF8)
  $nsm = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
  $nsm.AddNamespace('m', $ns)
  $list = New-Object System.Collections.Generic.List[string]
  foreach ($si in $doc.SelectNodes('//m:si', $nsm)) {
    $texts = $si.SelectNodes('.//m:t', $nsm) | ForEach-Object { $_.'#text' }
    [void]$list.Add(($texts -join ''))
  }
  return $list
}
function Col-Index($ref) {
  $letters = ($ref -replace '[0-9]','')
  $n = 0
  foreach ($ch in $letters.ToCharArray()) { $n = $n * 26 + ([int]$ch - 64) }
  return $n
}
function Load-Sheet($path, $ss) {
  $doc = [xml](Get-Content $path -Encoding UTF8)
  $nsm = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
  $nsm.AddNamespace('m', $ns)
  $rows = @{}
  $merges = @()
  foreach ($m in $doc.SelectNodes('//m:mergeCell', $nsm)) { $merges += $m.GetAttribute('ref') }
  foreach ($c in $doc.SelectNodes('//m:c', $nsm)) {
    $ref = $c.GetAttribute('r')
    $t = $c.GetAttribute('t')
    $v = $c.SelectSingleNode('m:v', $nsm)
    $is = $c.SelectSingleNode('m:is', $nsm)
    $val = $null
    if ($t -eq 's' -and $v) { $val = $ss[[int]$v.InnerText] }
    elseif ($t -eq 'inlineStr' -and $is) { $val = ($is.SelectNodes('.//m:t', $nsm) | ForEach-Object { $_.'#text' }) -join '' }
    elseif ($v) { $val = $v.InnerText }
    if ($null -eq $val -or $val -eq '') { continue }
    if ($ref -notmatch '^([A-Z]+)(\d+)$') { continue }
    $col = Col-Index $Matches[1]
    $row = [int]$Matches[2]
    if (-not $rows.ContainsKey($row)) { $rows[$row] = @{} }
    $rows[$row][$col] = $val
  }
  return @{ Rows = $rows; Merges = $merges }
}

$ss = Load-SharedStrings (Join-Path $tmp 'xl\sharedStrings.xml')
$s2 = Load-Sheet $nsmgrPath $ss
$sheet2 = $s2.Rows
Write-Output '--- merges sheet2 ---'
$s2.Merges | Select-Object -First 40
Write-Output "mergeCount=$($s2.Merges.Count)"

# sample data rows
Write-Output '--- sample rows 4,5,6,20,50,100,200,400,687,688 ---'
foreach ($r in 4,5,6,20,50,100,200,400,687,688) {
  if (-not $sheet2.ContainsKey($r)) { Write-Output "R$r MISSING"; continue }
  $cols = $sheet2[$r].Keys | Sort-Object
  $parts = foreach ($c in $cols) { "${c}:$($sheet2[$r][$c])" }
  Write-Output "R$r $($parts -join ' | ')"
}

# unique devices / tags / mapping counts
$devices = @{}
$stations = @{}
$plcs = @{}
$nly=$cng=$ctb=$loi=$trd=$bcao=$rt=0
for ($r=4; $r -le 2000; $r++) {
  if (-not $sheet2.ContainsKey($r)) { continue }
  $row = $sheet2[$r]
  if ($row.ContainsKey(9)) { $devices[$row[9]] = 1 }
  if ($row.ContainsKey(7)) { $stations[$row[7]] = 1 }
  if ($row.ContainsKey(8)) { $plcs[$row[8]] = 1 }
  $v13 = $row[13]; $v14=$row[14]; $v15=$row[15]; $v16=$row[16]; $v17=$row[17]; $v18=$row[18]; $v13rt=$row[13]
  if ($row.ContainsKey(13) -and $row[13] -eq 'Có') { $nly++ }
  # wait col 13 is both Realtime header and Nguyen ly? Row2 col13=Realtime, Row3 col13=Nguyên lý
  # So data: col13 = Nguyên lý (Có), but header row2 says Realtime
  if ($row.ContainsKey(13) -and $row[13] -eq 'Có') { } 
  if ($row.ContainsKey(14) -and $row[14] -eq 'Có') { $cng++ }
  if ($row.ContainsKey(15) -and $row[15] -eq 'Có') { $ctb++ }
  if ($row.ContainsKey(16) -and $row[16] -eq 'Có') { $loi++ }
  if ($row.ContainsKey(17) -and $row[17].ToString().Trim() -ne '') { $trd++ }
  if ($row.ContainsKey(18) -and $row[18].ToString().Trim() -ne '') { $bcao++ }
  if ($row.ContainsKey(13) -and $row[13] -eq 'Có') { $nly++ } # counted twice oops
}
Write-Output "stations: $($stations.Keys -join ',')"
Write-Output "plcs: $($plcs.Keys -join ',')"
Write-Output "devices: $($devices.Keys -join ',')"
Write-Output "counts nly=$nly cng=$cng ctb=$ctb loi=$loi trend=$trd baocao=$bcao"

# unique trend/report labels
$trendLabels = @{}
$reportLabels = @{}
$nly=$cng=$ctb=$loi=$trd=$bcao=0
$dataRows=0
for ($r=4; $r -le 2000; $r++) {
  if (-not $sheet2.ContainsKey($r)) { continue }
  $row = $sheet2[$r]
  if (-not $row.ContainsKey(2) -and -not $row.ContainsKey(10)) { continue }
  $dataRows++
  if ($row.ContainsKey(13) -and $row[13] -eq 'Có') { $nly++ }
  if ($row.ContainsKey(14) -and $row[14] -eq 'Có') { $cng++ }
  if ($row.ContainsKey(15) -and $row[15] -eq 'Có') { $ctb++ }
  if ($row.ContainsKey(16) -and $row[16] -eq 'Có') { $loi++ }
  if ($row.ContainsKey(17) -and -not [string]::IsNullOrWhiteSpace($row[17]) -and $row[17] -ne 'Có') { $trd++; $trendLabels[$row[17]] = 1 }
  elseif ($row.ContainsKey(17) -and $row[17] -eq 'Có') { $trd++; $trendLabels['Có'] = 1 }
  if ($row.ContainsKey(18) -and -not [string]::IsNullOrWhiteSpace($row[18])) { $bcao++; $reportLabels[$row[18]] = 1 }
}
Write-Output "dataRows=$dataRows nly=$nly cng=$cng ctb=$ctb loi=$loi trend=$trd baocao=$bcao"
Write-Output '--- trend labels ---'
$trendLabels.Keys | Sort-Object | ForEach-Object { $_ }
Write-Output '--- report labels ---'
$reportLabels.Keys | Sort-Object | ForEach-Object { $_ }

# col12 Value samples
Write-Output '--- col12 Value samples ---'
$vals=@{}
for ($r=4; $r -le 2000; $r++) {
  if (-not $sheet2.ContainsKey($r)) { continue }
  if ($sheet2[$r].ContainsKey(12)) { $vals[$sheet2[$r][12]] = 1 }
}
$vals.Keys | Select-Object -First 30
