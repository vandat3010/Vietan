Add-Type -AssemblyName System.Xml.Linq
$tmp = Join-Path $env:TEMP 'tln-xlsx-inspect2'
$ns = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'

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
function Parse-Ref($ref) {
  if ($ref -notmatch '^([A-Z]+)(\d+)$') { return $null }
  return @{ Col = (Col-Index $Matches[1]); Row = [int]$Matches[2] }
}

$ss = Load-SharedStrings (Join-Path $tmp 'xl\sharedStrings.xml')
$doc = [xml](Get-Content (Join-Path $tmp 'xl\worksheets\sheet2.xml') -Encoding UTF8)
$nsm = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$nsm.AddNamespace('m', $ns)
$rows = @{}
foreach ($c in $doc.SelectNodes('//m:c', $nsm)) {
  $ref = $c.GetAttribute('r')
  $t = $c.GetAttribute('t')
  $v = $c.SelectSingleNode('m:v', $nsm)
  $val = $null
  if ($t -eq 's' -and $v) { $val = $ss[[int]$v.InnerText] }
  elseif ($v) { $val = $v.InnerText }
  if ($null -eq $val -or $val -eq '') { continue }
  $p = Parse-Ref $ref
  if ($null -eq $p) { continue }
  if (-not $rows.ContainsKey($p.Row)) { $rows[$p.Row] = @{} }
  $rows[$p.Row][$p.Col] = $val
}
# expand merges
foreach ($m in $doc.SelectNodes('//m:mergeCell', $nsm)) {
  $ref = $m.GetAttribute('ref')
  if ($ref -notmatch '^([A-Z]+)(\d+):([A-Z]+)(\d+)$') { continue }
  $c1 = Col-Index $Matches[1]; $r1=[int]$Matches[2]; $c2=Col-Index $Matches[3]; $r2=[int]$Matches[4]
  $src = $null
  if ($rows.ContainsKey($r1) -and $rows[$r1].ContainsKey($c1)) { $src = $rows[$r1][$c1] }
  if ($null -eq $src) { continue }
  for ($r=$r1; $r -le $r2; $r++) {
    if (-not $rows.ContainsKey($r)) { $rows[$r] = @{} }
    for ($c=$c1; $c -le $c2; $c++) {
      if (-not $rows[$r].ContainsKey($c)) { $rows[$r][$c] = $src }
    }
  }
}

function IsCo($v) { return ($v -eq 'Có' -or $v -eq '1' -or $v -eq 'TRUE' -or $v -eq 'True') }
function HasText($v) { return -not [string]::IsNullOrWhiteSpace([string]$v) }

$uniq13=@{}; $uniq14=@{}; $uniq15=@{}; $uniq16=@{}
$nly=$cng=$ctb=$loi=$trd=$bcao=0
$byDev = @{}
for ($r=4; $r -le 2000; $r++) {
  if (-not $rows.ContainsKey($r)) { continue }
  $row = $rows[$r]
  $full = $row[2]; if (-not $full) { continue }
  $dev = $row[9]
  if (-not $byDev.ContainsKey($dev)) { $byDev[$dev] = @{n=0; nly=0; cng=0; ctb=0; loi=0; trd=0; bcao=0} }
  $byDev[$dev].n++
  if ($row.ContainsKey(13)) { $uniq13[$row[13]]++ }
  if ($row.ContainsKey(14)) { $uniq14[$row[14]]++ }
  if ($row.ContainsKey(15)) { $uniq15[$row[15]]++ }
  if ($row.ContainsKey(16)) { $uniq16[$row[16]]++ }
  if (IsCo $row[13]) { $nly++; $byDev[$dev].nly++ }
  if (IsCo $row[14]) { $cng++; $byDev[$dev].cng++ }
  if (IsCo $row[15]) { $ctb++; $byDev[$dev].ctb++ }
  if (IsCo $row[16]) { $loi++; $byDev[$dev].loi++ }
  if (HasText $row[17]) { $trd++; $byDev[$dev].trd++ }
  if (HasText $row[18]) { $bcao++; $byDev[$dev].bcao++ }
}
Write-Output "AFTER MERGE FILL: nly=$nly cng=$cng ctb=$ctb loi=$loi trend=$trd baocao=$bcao"
Write-Output 'col13 unique:'; $uniq13.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }
Write-Output 'col14 unique:'; $uniq14.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }
Write-Output 'col15 unique:'; $uniq15.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }
Write-Output 'col16 unique:'; $uniq16.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }
Write-Output '--- by device ---'
$byDev.GetEnumerator() | Sort-Object Name | ForEach-Object {
  $v=$_.Value
  "$($_.Key) n=$($v.n) nly=$($v.nly) cng=$($v.cng) ctb=$($v.ctb) loi=$($v.loi) trd=$($v.trd) bc=$($v.bcao)"
}
