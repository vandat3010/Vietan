using Backend.Domain.Enums;
using Backend.Shared.Constants;

namespace Backend.Application.DTOs.Scada;

/// <summary>Map PLC value / raw code ↔ enum ↔ API status string.</summary>
public static class ScadaStatusMapper
{
    public static string ToCode(PumpOperatingStatus status) => status switch
    {
        PumpOperatingStatus.Running => ScadaStatusCodes.Running,
        PumpOperatingStatus.Error => ScadaStatusCodes.Error,
        PumpOperatingStatus.Stopped => ScadaStatusCodes.Stopped,
        PumpOperatingStatus.Maintenance => ScadaStatusCodes.Maintenance,
        _ => ScadaStatusCodes.Unknown
    };

    public static string ToCode(LockStatus status) => status switch
    {
        LockStatus.Closed => ScadaStatusCodes.Closed,
        _ => ScadaStatusCodes.Open
    };

    public static PumpOperatingStatus ParsePump(double? value) => value switch
    {
        1 => PumpOperatingStatus.Running,
        2 => PumpOperatingStatus.Error,
        3 => PumpOperatingStatus.Stopped,
        4 => PumpOperatingStatus.Maintenance,
        _ => PumpOperatingStatus.Unknown
    };

    public static PumpOperatingStatus ParsePump(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return PumpOperatingStatus.Unknown;

        return rawCode.Trim().ToLowerInvariant() switch
        {
            ScadaStatusCodes.Running or "run" or "on" => PumpOperatingStatus.Running,
            ScadaStatusCodes.Error or "fault" or "alarm" => PumpOperatingStatus.Error,
            ScadaStatusCodes.Stopped or "stop" or "off" => PumpOperatingStatus.Stopped,
            ScadaStatusCodes.Maintenance or "maint" or "service" => PumpOperatingStatus.Maintenance,
            ScadaStatusCodes.Unknown => PumpOperatingStatus.Unknown,
            _ => PumpOperatingStatus.Unknown
        };
    }

    public static PumpOperatingStatus ParseKdm(double? value) => value switch
    {
        1 => PumpOperatingStatus.Running,
        2 => PumpOperatingStatus.Error,
        _ => PumpOperatingStatus.Stopped
    };

    public static PumpOperatingStatus ParseKdm(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return PumpOperatingStatus.Stopped;

        return rawCode.Trim().ToLowerInvariant() switch
        {
            ScadaStatusCodes.Running or "run" or "on" => PumpOperatingStatus.Running,
            ScadaStatusCodes.Error or "fault" => PumpOperatingStatus.Error,
            ScadaStatusCodes.Stopped or "stop" or "off" => PumpOperatingStatus.Stopped,
            _ => PumpOperatingStatus.Stopped
        };
    }

    public static LockStatus ParseLock(double? value) =>
        value is 1 ? LockStatus.Closed : LockStatus.Open;

    public static LockStatus ParseLock(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return LockStatus.Open;

        return rawCode.Trim().ToLowerInvariant() switch
        {
            ScadaStatusCodes.Closed or "close" or "on" => LockStatus.Closed,
            ScadaStatusCodes.Open or "off" => LockStatus.Open,
            _ => LockStatus.Open
        };
    }

    public static string MapMotorStatus(double? value, string? rawCode = null)
    {
        // Prefer numeric/bool PLC value. Tag codes like FB_Run are not status enums.
        if (value is not null)
            return ToCode(ParsePump(value));

        if (!string.IsNullOrWhiteSpace(rawCode))
        {
            var fromCode = ParsePump(rawCode);
            if (fromCode != PumpOperatingStatus.Unknown)
                return ToCode(fromCode);
        }

        return ToCode(PumpOperatingStatus.Unknown);
    }

    /// <summary>
    /// Resolve status from Excel TLHN bool tags: Fault → Maintenance → Run → Stop.
    /// </summary>
    public static string ResolveFromFeedback(
        bool? fault,
        bool? maintenance,
        bool? run,
        bool? stop)
    {
        if (fault == true) return ScadaStatusCodes.Error;
        if (maintenance == true) return ScadaStatusCodes.Maintenance;
        if (run == true) return ScadaStatusCodes.Running;
        if (stop == true) return ScadaStatusCodes.Stopped;
        if (run == false && stop != true) return ScadaStatusCodes.Stopped;
        return ScadaStatusCodes.Unknown;
    }

    public static string MapKdmStatus(double? value, string? rawCode = null)
    {
        var status = !string.IsNullOrWhiteSpace(rawCode)
            ? ParseKdm(rawCode)
            : ParseKdm(value);
        return ToCode(status);
    }

    public static string MapLockStatus(double? value, string? rawCode = null)
    {
        var status = !string.IsNullOrWhiteSpace(rawCode)
            ? ParseLock(rawCode)
            : ParseLock(value);
        return ToCode(status);
    }

    /// <summary>Khi motor bảo trì mà KĐM đang chạy → ép KĐM dừng.</summary>
    public static string ResolveKdmStatus(string motorStatus, string kdmStatus) =>
        motorStatus == ScadaStatusCodes.Maintenance && kdmStatus == ScadaStatusCodes.Running
            ? ScadaStatusCodes.Stopped
            : kdmStatus;
}
