namespace Backend.Domain.Enums;

/// <summary>
/// Trạng thái khoá / MCCB (giá trị PLC số).
/// API JSON dùng mã string trong <c>Backend.Shared.Constants.ScadaStatusCodes</c>.
/// </summary>
public enum LockStatus
{
    Open = 0,
    Closed = 1
}
