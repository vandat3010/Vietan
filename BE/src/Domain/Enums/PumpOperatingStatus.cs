namespace Backend.Domain.Enums;

/// <summary>
/// Trạng thái vận hành bơm / motor / KĐM (giá trị PLC số).
/// API JSON dùng mã string trong <c>Backend.Shared.Constants.ScadaStatusCodes</c>.
/// </summary>
public enum PumpOperatingStatus
{
    Unknown = 0,
    Running = 1,
    Error = 2,
    Stopped = 3,
    Maintenance = 4
}
