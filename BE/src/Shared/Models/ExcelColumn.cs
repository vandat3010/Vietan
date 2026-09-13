namespace Backend.Shared.Models;

/// <summary>
/// Định nghĩa một cột khi export Excel: tiêu đề hiển thị và cách lấy giá trị từ
/// một dòng dữ liệu.
/// <para>
/// Tồn tại vì cách export mặc định (phản chiếu qua property của
/// <typeparamref name="T"/>) không đáp ứng được ba nhu cầu rất thường gặp:
/// tiêu đề tiếng Việt có dấu, giá trị ghép/tính toán từ nhiều property, và thứ
/// tự cột do người dùng quyết định chứ không theo thứ tự khai báo trong class.
/// </para>
/// </summary>
/// <typeparam name="T">Kiểu của một dòng dữ liệu.</typeparam>
public class ExcelColumn<T>
{
    public ExcelColumn(string header, Func<T, object?> valueSelector, string? numberFormat = null, double? width = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        ArgumentNullException.ThrowIfNull(valueSelector);

        Header = header;
        ValueSelector = valueSelector;
        NumberFormat = numberFormat;
        Width = width;
    }

    /// <summary>Tiêu đề in ở dòng đầu tiên.</summary>
    public string Header { get; }

    /// <summary>Hàm lấy giá trị của cột này từ một dòng dữ liệu.</summary>
    public Func<T, object?> ValueSelector { get; }

    /// <summary>
    /// Định dạng hiển thị của Excel, ví dụ <c>dd/MM/yyyy</c> hoặc <c>#,##0.00</c>.
    /// Bỏ trống thì dùng định dạng mặc định của Excel.
    /// </summary>
    public string? NumberFormat { get; }

    /// <summary>Độ rộng cột cố định. Bỏ trống thì cột được tự canh theo nội dung.</summary>
    public double? Width { get; }
}

/// <summary>
/// Danh sách cột hỗ trợ cú pháp collection initializer, để khai báo bảng export
/// đọc gần giống như chính cái bảng sẽ xuất ra:
/// <code>
/// var columns = new ExcelColumns&lt;UserDto&gt;
/// {
///     { "Email",     u =&gt; u.Email },
///     { "Họ tên",    u =&gt; $"{u.FirstName} {u.LastName}" },
///     { "Trạng thái", u =&gt; u.Status },
///     { "Ngày tạo",  u =&gt; u.CreatedDate, "dd/MM/yyyy HH:mm" }
/// };
/// </code>
/// </summary>
public class ExcelColumns<T> : List<ExcelColumn<T>>
{
    public void Add(string header, Func<T, object?> valueSelector, string? numberFormat = null, double? width = null) =>
        Add(new ExcelColumn<T>(header, valueSelector, numberFormat, width));
}
