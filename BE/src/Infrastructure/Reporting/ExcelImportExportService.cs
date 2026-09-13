using System.Globalization;
using System.Reflection;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Exceptions;
using Backend.Shared.Models;
using ClosedXML.Excel;

namespace Backend.Infrastructure.Reporting;

/// <summary>
/// ClosedXML-backed <see cref="IImportExportService"/>. Parsing is deliberately
/// fault-tolerant: a cell that cannot be converted produces an
/// <see cref="ImportError"/> and disqualifies its own row only, so a 5,000-row upload
/// with three typos comes back as 4,997 usable rows plus three actionable messages
/// instead of a single exception the user has to guess their way through.
/// </summary>
public class ExcelImportExportService : IImportExportService
{
    /// <summary>Beyond this row count the column auto-fit pass costs more than the readability it buys.</summary>
    private const int AutoFitRowLimit = 1_000;

    /// <remarks>
    /// Parsing only - nothing here touches the database. The caller decides whether to
    /// persist <see cref="ImportResult{T}.ValidRows"/>, and does so through
    /// <c>IUnitOfWork</c> so the import shares the transaction/audit rules of every
    /// other write in the system.
    /// </remarks>
    public Task<ImportResult<T>> ImportExcelAsync<T>(Stream excelStream, CancellationToken cancellationToken = default) where T : new() =>
        ParseAsync<T>(excelStream, cancellationToken);

    public Task<ImportResult<T>> ValidateImportAsync<T>(Stream excelStream, CancellationToken cancellationToken = default) where T : new() =>
        ParseAsync<T>(excelStream, cancellationToken);

    /// <remarks>
    /// Header lấy đúng tên property để file xuất ra import lại được không cần sửa.
    /// Nội bộ vẫn đi qua cùng một routine ghi với bản export theo cột tự khai báo,
    /// nên hai đường không bao giờ lệch nhau về định dạng.
    /// </remarks>
    public Task<byte[]> ExportExcelAsync<T>(IEnumerable<T> rows, string sheetName = "Data", CancellationToken cancellationToken = default) =>
        ExportExcelAsync(rows, BuildColumnsFromProperties<T>(), sheetName, cancellationToken);

    public Task<byte[]> ExportExcelAsync<T>(
        IEnumerable<T> rows,
        IReadOnlyList<ExcelColumn<T>> columns,
        string sheetName = "Data",
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        Write(rows, columns, stream, sheetName, cancellationToken);

        return Task.FromResult(stream.ToArray());
    }

    public Task ExportExcelAsync<T>(
        IEnumerable<T> rows,
        IReadOnlyList<ExcelColumn<T>> columns,
        Stream destination,
        string sheetName = "Data",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        Write(rows, columns, destination, sheetName, cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Điểm ghi workbook duy nhất: mọi overload export đều đi qua đây.
    /// </summary>
    private static void Write<T>(
        IEnumerable<T> rows,
        IReadOnlyList<ExcelColumn<T>> columns,
        Stream destination,
        string sheetName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);

        if (columns.Count == 0)
            throw new ArgumentException("Cần khai báo ít nhất một cột để export.", nameof(columns));

        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet(SanitiseSheetName(sheetName));

        for (var index = 0; index < columns.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = columns[index].Header;

            if (columns[index].Width is { } width)
                worksheet.Column(index + 1).Width = width;
        }

        worksheet.Row(1).Style.Font.Bold = true;

        // Giữ dòng tiêu đề luôn hiển thị khi cuộn - file export thường dài.
        worksheet.SheetView.FreezeRows(1);

        var rowNumber = 1;
        foreach (var item in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            for (var index = 0; index < columns.Count; index++)
            {
                var column = columns[index];
                var cell = worksheet.Cell(rowNumber, index + 1);

                SetCellValue(cell, column.ValueSelector(item));

                // Áp định dạng lên từng ô dữ liệu, không áp cho cả cột, để ô tiêu
                // đề (là text) không bị dính định dạng số/ngày.
                if (!string.IsNullOrWhiteSpace(column.NumberFormat))
                    cell.Style.NumberFormat.Format = column.NumberFormat;
            }
        }

        worksheet.Range(1, 1, rowNumber, columns.Count).SetAutoFilter();

        // Chỉ tự canh những cột không được chỉ định độ rộng, và chỉ với file nhỏ.
        if (rowNumber <= AutoFitRowLimit)
        {
            for (var index = 0; index < columns.Count; index++)
            {
                if (columns[index].Width is null)
                    worksheet.Column(index + 1).AdjustToContents();
            }
        }

        workbook.SaveAs(destination);
    }

    /// <summary>Sinh danh sách cột từ property của <typeparamref name="T"/> cho bản export mặc định.</summary>
    private static IReadOnlyList<ExcelColumn<T>> BuildColumnsFromProperties<T>() =>
        [.. GetReadableProperties<T>().Select(property =>
            new ExcelColumn<T>(property.Name, item => property.GetValue(item)))];

    /// <summary>
    /// The one parse routine behind both import and validate, so a "preview" can never
    /// disagree with what the subsequent import produces.
    /// </summary>
    private static async Task<ImportResult<T>> ParseAsync<T>(Stream excelStream, CancellationToken cancellationToken) where T : new()
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        // ClosedXML needs random access; request/network streams often provide none.
        if (excelStream.CanSeek)
        {
            excelStream.Position = 0;
            return Parse<T>(excelStream, cancellationToken);
        }

        using var buffer = new MemoryStream();
        await excelStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return Parse<T>(buffer, cancellationToken);
    }

    private static ImportResult<T> Parse<T>(Stream excelStream, CancellationToken cancellationToken) where T : new()
    {
        using var workbook = OpenWorkbook(excelStream);

        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new BusinessException("The workbook contains no worksheets.", "Import.EmptyWorkbook");

        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null) return new ImportResult<T>();

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);

        var columnMap = new Dictionary<int, PropertyInfo>();
        var columnNames = new Dictionary<int, string>();

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (header.Length == 0) continue;

            columnNames[cell.Address.ColumnNumber] = header;

            // Unrecognised columns are ignored on purpose: spreadsheets in the wild carry
            // notes/id columns that are none of our business.
            if (properties.TryGetValue(header, out var property))
                columnMap[cell.Address.ColumnNumber] = property;
        }

        if (columnMap.Count == 0)
        {
            return new ImportResult<T>
            {
                Errors =
                [
                    new ImportError(
                        headerRow.RowNumber(),
                        string.Empty,
                        $"No column header matches a writable property of {typeof(T).Name}. Expected one of: {string.Join(", ", properties.Keys)}.")
                ]
            };
        }

        var validRows = new List<T>();
        var errors = new List<ImportError>();
        var totalRows = 0;

        foreach (var row in worksheet.RowsUsed())
        {
            if (row.RowNumber() <= headerRow.RowNumber()) continue;

            cancellationToken.ThrowIfCancellationRequested();
            totalRows++;

            var item = new T();
            var rowIsValid = true;

            foreach (var (columnNumber, property) in columnMap)
            {
                if (TryConvert(row.Cell(columnNumber), property.PropertyType, out var value, out var error))
                {
                    property.SetValue(item, value);
                    continue;
                }

                rowIsValid = false;
                errors.Add(new ImportError(row.RowNumber(), columnNames[columnNumber], error!));
            }

            if (rowIsValid) validRows.Add(item);
        }

        return new ImportResult<T>
        {
            ValidRows = validRows,
            Errors = errors,
            TotalRows = totalRows
        };
    }

    private static XLWorkbook OpenWorkbook(Stream excelStream)
    {
        try
        {
            return new XLWorkbook(excelStream);
        }
        catch (Exception exception)
        {
            // A corrupt/renamed upload is the user's mistake, not a server fault - surface it as 400.
            throw new BusinessException(
                "The uploaded file could not be read as an .xlsx workbook.",
                "Import.InvalidFile",
                new Dictionary<string, object?> { ["reason"] = exception.Message });
        }
    }

    /// <summary>
    /// Native cell types win over the displayed text: Excel keeps dates and numbers as
    /// doubles and renders them with the workbook's locale, so round-tripping through
    /// <see cref="string"/> would break as soon as a file is authored in another region.
    /// </summary>
    private static bool TryConvert(IXLCell cell, Type targetType, out object? converted, out string? error)
    {
        converted = null;
        error = null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var acceptsNull = underlyingType != targetType || !targetType.IsValueType;

        var cellValue = cell.Value;

        if (cellValue.IsBlank || (cellValue.IsText && string.IsNullOrWhiteSpace(cellValue.GetText())))
        {
            if (!acceptsNull)
            {
                error = "A value is required.";
                return false;
            }

            return true;
        }

        if (cellValue.IsError)
        {
            error = $"The cell contains the Excel error '{cellValue.GetError()}'.";
            return false;
        }

        if (underlyingType == typeof(string))
        {
            converted = cell.GetString().Trim();
            return true;
        }

        if (underlyingType == typeof(DateTime) && cellValue.IsDateTime)
        {
            converted = cellValue.GetDateTime();
            return true;
        }

        if (underlyingType == typeof(bool) && cellValue.IsBoolean)
        {
            converted = cellValue.GetBoolean();
            return true;
        }

        // A date column that was never date-formatted arrives as an OLE Automation serial.
        if (underlyingType == typeof(DateTime) && cellValue.IsNumber)
        {
            try
            {
                converted = DateTime.FromOADate(cellValue.GetNumber());
                return true;
            }
            catch (ArgumentException)
            {
                error = $"'{cell.GetString()}' is not a valid date.";
                return false;
            }
        }

        if (cellValue.IsNumber && IsNumeric(underlyingType))
        {
            try
            {
                converted = Convert.ChangeType(cellValue.GetNumber(), underlyingType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException)
            {
                error = $"'{cell.GetString()}' is out of range for {underlyingType.Name}.";
                return false;
            }
        }

        return TryConvertText(cell.GetString().Trim(), underlyingType, out converted, out error);
    }

    private static bool TryConvertText(string text, Type underlyingType, out object? converted, out string? error)
    {
        converted = null;
        error = null;

        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse(underlyingType, text, ignoreCase: true, out var enumValue) &&
                enumValue is not null &&
                Enum.IsDefined(underlyingType, enumValue))
            {
                converted = enumValue;
                return true;
            }

            error = $"'{text}' is not one of: {string.Join(", ", Enum.GetNames(underlyingType))}.";
            return false;
        }

        object? parsed = underlyingType switch
        {
            _ when underlyingType == typeof(int) =>
                int.TryParse(text, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var intValue)
                    ? (object?)intValue : null,
            _ when underlyingType == typeof(long) =>
                long.TryParse(text, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var longValue)
                    ? (object?)longValue : null,
            _ when underlyingType == typeof(decimal) =>
                decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)
                    ? (object?)decimalValue : null,
            _ when underlyingType == typeof(double) =>
                double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue)
                    ? (object?)doubleValue : null,
            _ when underlyingType == typeof(bool) => ParseBoolean(text),
            _ when underlyingType == typeof(DateTime) =>
                DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue)
                    ? (object?)dateValue : null,
            _ when underlyingType == typeof(Guid) =>
                Guid.TryParse(text, out var guidValue) ? (object?)guidValue : null,
            _ => ChangeType(text, underlyingType)
        };

        if (parsed is null)
        {
            error = $"'{text}' is not a valid {underlyingType.Name} value.";
            return false;
        }

        converted = parsed;
        return true;
    }

    /// <summary>
    /// Last resort for the less common primitives (short, float, char, ...) so an exotic
    /// property type degrades into a per-cell error instead of aborting the whole import.
    /// </summary>
    private static object? ChangeType(string text, Type underlyingType)
    {
        try
        {
            return Convert.ChangeType(text, underlyingType, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    /// <summary>Spreadsheets express booleans as TRUE/FALSE, 1/0 and yes/no interchangeably.</summary>
    private static object? ParseBoolean(string text) => text.ToLowerInvariant() switch
    {
        "true" or "1" or "yes" or "y" => true,
        "false" or "0" or "no" or "n" => false,
        _ => null
    };

    private static bool IsNumeric(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(double) ||
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong) || type == typeof(float);

    private static PropertyInfo[] GetReadableProperties<T>() =>
        [.. typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)];

    /// <summary>
    /// Maps CLR values onto the handful of native Excel types; anything else (enum, Guid,
    /// complex type) is written as invariant text so the cell is still readable.
    /// </summary>
    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                return;
            case string text:
                cell.Value = text;
                break;
            case bool flag:
                cell.Value = flag;
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.UtcDateTime;
                break;
            case TimeSpan timeSpan:
                cell.Value = timeSpan;
                break;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            default:
                cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                break;
        }
    }

    /// <summary>Excel rejects workbooks whose sheet name is empty, over 31 chars or contains <c>[]:*?/\</c>.</summary>
    private static string SanitiseSheetName(string sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName)) return "Data";

        var cleaned = string.Concat(sheetName.Where(character => !"[]:*?/\\".Contains(character))).Trim();

        if (cleaned.Length == 0) return "Data";

        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
