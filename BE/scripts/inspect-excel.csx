#r "nuget: ClosedXML, 0.105.1"
using ClosedXML.Excel;

var path = @"c:\Users\PC\Downloads\Định Nghĩa Tag Thủy Lợi Hà Nội.xlsx";
using var wb = new XLWorkbook(path);
foreach (var ws in wb.Worksheets)
{
    Console.WriteLine($"=== SHEET: {ws.Name} used={ws.RangeUsed()?.RangeAddress} ===");
    var used = ws.RangeUsed();
    if (used is null) continue;
    var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 20);
    var lastRow = Math.Min(used.LastRow().RowNumber(), 8);
    for (var r = 1; r <= lastRow; r++)
    {
        var cells = new List<string>();
        for (var c = 1; c <= lastCol; c++)
        {
            var v = ws.Cell(r, c).GetFormattedString();
            if (!string.IsNullOrWhiteSpace(v))
                cells.Add($"{c}:{v.Replace('\n',' ').Trim()}");
        }
        Console.WriteLine($"R{r}: {string.Join(" | ", cells)}");
    }
    Console.WriteLine($"rows={used.LastRow().RowNumber()} cols={used.LastColumn().ColumnNumber()}");
}
