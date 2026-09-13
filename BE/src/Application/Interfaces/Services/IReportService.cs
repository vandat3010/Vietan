using Backend.Application.DTOs.Reports;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// Runs parameterised, read-only reports (raw SQL or stored procedure) through the
/// Dapper read-side and turns the result set into a downloadable file. Kept as an
/// Application-layer abstraction so controllers/handlers never see ClosedXML, Dapper
/// or any other file/IO dependency.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Executes the report described by <paramref name="request"/> and materialises
    /// the rows as <typeparamref name="T"/>. See <see cref="ReportRequest"/> for the
    /// rule that statements must come from a server-side report catalogue.
    /// </summary>
    Task<IReadOnlyList<T>> GenerateAsync<T>(ReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders <paramref name="rows"/> as an .xlsx workbook, one column per public
    /// readable property of <typeparamref name="T"/>.
    /// </summary>
    Task<byte[]> ExportExcelAsync<T>(IEnumerable<T> rows, string sheetName = "Report", CancellationToken cancellationToken = default);

    /// <summary>
    /// PDF export extension point. The default implementation throws
    /// <see cref="NotSupportedException"/>: this template deliberately ships without a
    /// PDF engine so that teams are not forced into the licensing model of QuestPDF or
    /// iText. The method stays on the contract - rather than being omitted - so adding
    /// PDF support later is a DI swap instead of an interface change that ripples
    /// through every caller.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown by any implementation without a PDF engine.</exception>
    Task<byte[]> ExportPdfAsync<T>(IEnumerable<T> rows, string title = "Report", CancellationToken cancellationToken = default);
}
