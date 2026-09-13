using Backend.Application.Realtime;

namespace Backend.Infrastructure.Realtime;

/// <summary>
/// Time-based oscillating fake PLC values so UI metrics visibly move every poll.
/// </summary>
public static class RealtimeValueGenerator
{
    public static object Generate(string? dataType, long tagId)
    {
        var kind = (dataType ?? string.Empty).Trim().ToLowerInvariant();
        var wave = SmoothWave(tagId);

        if (kind is "bool" or "boolean" or "bit")
            // Flip roughly every ~4s, staggered by tag.
            return Math.Sin(DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond / 2.0 + tagId * 0.7) > 0;

        if (kind is "int" or "integer" or "word" or "dword" or "byte" or "sint" or "dint")
            return (int)Math.Round(40 + wave * 35); // 5..75, moves each tick

        if (kind is "real" or "float" or "double" or "lreal" or "number")
            return Math.Round(200 + wave * 180, 2); // 20..380

        return (int)Math.Round(50 + wave * 40);
    }

    /// <summary>Electrical / process analogs with realistic bands that drift continuously.</summary>
    public static double GenerateAnalog(long tagId, double center, double amplitude, double periodSec = 2.5)
    {
        var wave = SmoothWave(tagId, periodSec);
        return Math.Round(center + wave * amplitude, 2);
    }

    private static double SmoothWave(long tagId, double periodSec = 2.5)
    {
        var t = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
        var phase = tagId * 0.41;
        // Two frequencies → less "flat" looking motion.
        return Math.Sin(2 * Math.PI * t / periodSec + phase) * 0.65
               + Math.Sin(2 * Math.PI * t / (periodSec * 0.37) + phase * 1.7) * 0.35;
    }
}
