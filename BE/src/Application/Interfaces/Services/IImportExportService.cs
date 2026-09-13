using Backend.Shared.Models;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// Spreadsheet round-trip for bulk data maintenance: parse an uploaded .xlsx into typed
/// rows, or write typed rows back out. The abstraction exists mainly so a single bad cell
/// never aborts a whole upload - failures are collected per row/column instead of thrown -
/// and so the Application layer stays free of any Excel library reference.
/// </summary>
public interface IImportExportService
{
    /// <summary>
    /// Parses and validates <paramref name="excelStream"/> into <typeparamref name="T"/> rows.
    /// Nothing is written to the database: persisting <see cref="ImportResult{T}.ValidRows"/>
    /// (and deciding whether to reject the batch when <see cref="ImportResult{T}.IsValid"/> is
    /// <c>false</c>) belongs to the caller, inside its own <c>IUnitOfWork</c> transaction.
    /// </summary>
    Task<ImportResult<T>> ImportExcelAsync<T>(Stream excelStream, CancellationToken cancellationToken = default) where T : new();

    /// <summary>
    /// Dry run of <see cref="ImportExcelAsync{T}"/> for "preview before import" screens:
    /// identical parsing and validation, with an explicit contract that it never persists.
    /// </summary>
    Task<ImportResult<T>> ValidateImportAsync<T>(Stream excelStream, CancellationToken cancellationToken = default) where T : new();

    /// <summary>
    /// Writes <paramref name="rows"/> to an .xlsx workbook whose header row matches the
    /// property names <see cref="ImportExcelAsync{T}"/> expects, so an export can be edited
    /// and re-imported unchanged.
    /// </summary>
    Task<byte[]> ExportExcelAsync<T>(IEnumerable<T> rows, string sheetName = "Data", CancellationToken cancellationToken = default);

    /// <summary>
    /// Export với cột do người gọi tự khai báo: mỗi cột gồm tiêu đề và hàm lấy
    /// giá trị. Dùng cho file gửi cho người dùng cuối, nơi tiêu đề phải là tiếng
    /// Việt và một cột có thể ghép từ nhiều trường (ví dụ "Họ tên" = FirstName +
    /// LastName). Overload không có <paramref name="columns"/> ở trên vẫn dành
    /// cho file kỹ thuật cần import lại được.
    /// </summary>
    /// <param name="rows">Dữ liệu cần xuất.</param>
    /// <param name="columns">Danh sách cột theo đúng thứ tự sẽ xuất ra.</param>
    /// <param name="sheetName">Tên sheet; ký tự Excel cấm sẽ được loại bỏ.</param>
    Task<byte[]> ExportExcelAsync<T>(
        IEnumerable<T> rows,
        IReadOnlyList<ExcelColumn<T>> columns,
        string sheetName = "Data",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Như trên nhưng ghi thẳng vào <paramref name="destination"/> thay vì trả về
    /// mảng byte, để file lớn không phải nằm trọn hai lần trong bộ nhớ. Dùng khi
    /// ghi ra đĩa, đẩy lên <c>IFileStorageService</c>, hoặc stream trực tiếp vào
    /// response.
    /// </summary>
    Task ExportExcelAsync<T>(
        IEnumerable<T> rows,
        IReadOnlyList<ExcelColumn<T>> columns,
        Stream destination,
        string sheetName = "Data",
        CancellationToken cancellationToken = default);
}
