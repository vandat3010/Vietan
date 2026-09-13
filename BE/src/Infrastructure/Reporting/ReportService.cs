using System.Data;
using System.Globalization;
using System.Reflection;
using Backend.Application.DTOs.Reports;
using Backend.Application.Interfaces.Dapper;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Exceptions;
using ClosedXML.Excel;
using Dapper;

namespace Backend.Infrastructure.Reporting;

/// <summary>
/// Default <see cref="IReportService"/>: every report goes through the shared
/// <see cref="IDapperRepository"/> read-side, so reports inherit the same connection
/// handling as the rest of the dashboard queries and can never accidentally write.
/// </summary>
public class ReportService(IDapperRepository dapperRepository) : IReportService
{
    /// <summary>Beyond this row count the column auto-fit pass costs more than the readability it buys.</summary>
    private const int AutoFitRowLimit = 1_000;

    /// <remarks>
    /// <see cref="ReportRequest.CommandTimeoutSeconds"/> is deliberately not forced onto
    /// <see cref="IDapperRepository"/>: that contract is shared with every other read query
    /// and exposes no timeout overload, so the value is reserved for a timeout-aware
    /// repository implementation and the connection default applies until then.
    /// </remarks>
    public async Task<IReadOnlyList<T>> GenerateAsync<T>(ReportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = BuildParameters(request.Parameters);

        if (request.IsStoredProcedure)
        {
            if (string.IsNullOrWhiteSpace(request.StoredProcedureName))
                throw new ValidationException(nameof(ReportRequest.StoredProcedureName), "A stored procedure name is required when IsStoredProcedure is true.");

            var procedureRows = await dapperRepository.ExecuteStoredProcedureAsync<T>(
                request.StoredProcedureName,
                parameters,
                cancellationToken);

            return [.. procedureRows];
        }

        if (string.IsNullOrWhiteSpace(request.Sql))
            throw new ValidationException(nameof(ReportRequest.Sql), "A SQL statement is required when IsStoredProcedure is false.");

        var rows = await dapperRepository.QueryAsync<T>(
            request.Sql,
            parameters,
            CommandType.Text,
            cancellationToken);

        return [.. rows];
    }

    public Task<byte[]> ExportExcelAsync<T>(IEnumerable<T> rows, string sheetName = "Report", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        cancellationToken.ThrowIfCancellationRequested();

        var properties = GetReadableProperties<T>();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet(SanitiseSheetName(sheetName));

        for (var column = 0; column < properties.Length; column++)
            worksheet.Cell(1, column + 1).Value = properties[column].Name;

        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 1;
        foreach (var item in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            for (var column = 0; column < properties.Length; column++)
                SetCellValue(worksheet.Cell(rowNumber, column + 1), properties[column].GetValue(item));
        }

        if (rowNumber <= AutoFitRowLimit)
            worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return Task.FromResult(stream.ToArray());
    }

    /// <inheritdoc />
    /// <remarks>
    /// Intentionally unimplemented: shipping a PDF engine would impose QuestPDF's or
    /// iText's licence on every consumer of this template, so the decision is left to the
    /// team that actually needs PDF output.
    /// </remarks>
    public Task<byte[]> ExportPdfAsync<T>(IEnumerable<T> rows, string title = "Report", CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "PDF export is not part of this template. Register a PDF-capable IReportService implementation (QuestPDF/iText) to enable PDF export.");

    /// <summary>
    /// Values always travel as Dapper parameters - concatenating them into the statement
    /// would reintroduce SQL injection through the one part of the request users control.
    /// </summary>
    private static DynamicParameters? BuildParameters(IDictionary<string, object?>? parameters)
    {
        if (parameters is null || parameters.Count == 0) return null;

        var dynamicParameters = new DynamicParameters();

        foreach (var (name, value) in parameters)
            dynamicParameters.Add(name, value);

        return dynamicParameters;
    }

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
        if (string.IsNullOrWhiteSpace(sheetName)) return "Report";

        var cleaned = string.Concat(sheetName.Where(character => !"[]:*?/\\".Contains(character))).Trim();

        if (cleaned.Length == 0) return "Report";

        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
