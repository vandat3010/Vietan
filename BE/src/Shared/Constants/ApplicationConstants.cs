namespace Backend.Shared.Constants;

/// <summary>
/// General, application-wide constants that don't belong to a more specific
/// bucket (Security/Cache/Validation). Anything used in more than one place
/// belongs here instead of being re-typed as a magic string/number.
/// </summary>
public static class ApplicationConstants
{
    public const string DefaultTimeZone = "UTC";
    public const string DefaultCulture = "en-US";

    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public const string DateFormat = "yyyy-MM-dd";
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>Định dạng ngày/giờ dùng trong file Excel xuất cho người dùng cuối.</summary>
    public const string ExcelDateFormat = "dd/MM/yyyy";
    public const string ExcelDateTimeFormat = "dd/MM/yyyy HH:mm";

    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Trần số dòng cho một lần export. Không phải giới hạn của Excel (hơn 1 triệu
    /// dòng) mà là ngưỡng an toàn bộ nhớ: quá mức này thì đúng cách làm là đẩy
    /// sang background job rồi gửi link tải, không phải giữ request HTTP chờ.
    /// </summary>
    public const int MaxExportRows = 50_000;

    /// <summary>Used by <see cref="Backend.Application.Common.ICurrentUserService"/> fallbacks and audit stamping.</summary>
    public const string SystemUserName = "system";
}
