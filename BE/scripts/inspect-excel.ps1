Add-Type -AssemblyName System.Xml.Linq
$tmp = Join-Path $env:TEMP 'tln-xlsx-inspect2'
$ns = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'

function Load-SharedStrings($path) {
  $doc = [xml](Get-Content $path -Encoding UTF8)
  $nsm = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
  $nsm.AddNamespace('m', $ns)
  $list = @()
  foreach ($si in $doc.SelectNodes('//m:si', $nsm)) {
    $texts = $si.SelectNodes('.//m:t', $nsm) | ForEach-Object { $_.'#text' }
    $list += ,(($texts -join ''))
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
  return $rows
}

$ss = Load-SharedStrings (Join-Path $tmp 'xl\sharedStrings.xml')
Write-Output "sharedStrings=$($ss.Count)"
Write-Output '--- first 50 strings ---'
for ($i=0; $i -lt [Math]::Min(50,$ss.Count); $i++) { Write-Output "[$i] $($ss[$i])" }

$sheet1 = Load-Sheet (Join-Path $tmp 'xl\worksheets\sheet1.xml') $ss
$sheet2 = Load-Sheet (Join-Path $tmp 'xl\worksheets\sheet2.xml') $ss
Write-Output "sheet1 rows=$($sheet1.Count) sheet2 rows=$($sheet2.Count)"

function Dump-Header($rows, $name) {
  Write-Output "=== $name header rows 1-6 ==="
  foreach ($r in 1..6) {
    if (-not $rows.ContainsKey($r)) { continue }
    $cols = $rows[$r].Keys | Sort-Object
    $parts = foreach ($c in $cols) { "${c}:$($rows[$r][$c])" }
    Write-Output "R$r $($parts -join ' | ')"
  }
}
Dump-Header $sheet1 'Tram Bom Ap Bac'
Dump-Header $sheet2 'Trung tam'

# Mapping counts on Trung tam: find header row
$headerRow = 1
$maxCol = 20
for ($r=1; $r -le 6; $r++) {
  if ($sheet2.ContainsKey($r) -and ($sheet2[$r].Values -contains 'Tag')) { $headerRow = $r; break }
}
Write-Output "headerRow=$headerRow"
if ($sheet2.ContainsKey($headerRow)) {
  $h = $sheet2[$headerRow]
  foreach ($c in ($h.Keys | Sort-Object)) { Write-Output "COL $c = $($h[$c])" }
}
