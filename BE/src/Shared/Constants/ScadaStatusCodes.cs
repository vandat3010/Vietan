namespace Backend.Shared.Constants;

/// <summary>Mã status string trả về FE (JSON).</summary>
public static class ScadaStatusCodes
{
    // PumpOperatingStatus
    public const string Unknown = "unknown";
    public const string Running = "running";
    public const string Error = "error";
    public const string Stopped = "stopped";
    public const string Maintenance = "maintenance";

    // LockStatus
    public const string Open = "open";
    public const string Closed = "closed";
}
