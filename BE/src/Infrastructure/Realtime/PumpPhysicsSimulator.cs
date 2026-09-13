using System.Collections.Concurrent;
using Backend.Application.Realtime;

namespace Backend.Infrastructure.Realtime;

/// <summary>
/// Classifies SCADA tag codes and advances coherent pump physics
/// (Run / Speed / Current / Temperature / Fault) for fake PLC simulation.
/// </summary>
public static class PumpTagClassifier
{
    public enum Kind
    {
        Unknown,
        Run,
        Stop,
        Fault,
        Temperature,
        TempSetpoint,
        Current,
        CurrentSetpoint,
        Speed,
        Runtime,
        Voltage,
        PowerFactor,
        Frequency,
        Power,
        Energy,
        WaterLevel
    }

    public static Kind Classify(string? code, string? tagName)
    {
        var raw = $"{code} {tagName}".ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(raw))
            return Kind.Unknown;

        if (raw.Contains("FAULT", StringComparison.Ordinal))
            return Kind.Fault;
        if (raw.Contains("SET_TEMP", StringComparison.Ordinal) || raw.Contains("CHO PHEP", StringComparison.OrdinalIgnoreCase))
            return Kind.TempSetpoint;
        // Excel typo: Set_CurentR/S/T
        if (raw.Contains("SET_CURENT", StringComparison.Ordinal)
            || raw.Contains("SET_CURRENT", StringComparison.Ordinal)
            || raw.Contains("SET_I1", StringComparison.Ordinal)
            || raw.Contains("SET_I2", StringComparison.Ordinal)
            || raw.Contains("SET_I3", StringComparison.Ordinal)
            || ((raw.Contains("CURRENT_MAX", StringComparison.Ordinal) || raw.Contains("I1_MAX", StringComparison.Ordinal)
                 || raw.Contains("I2_MAX", StringComparison.Ordinal) || raw.Contains("I3_MAX", StringComparison.Ordinal))
                && (raw.Contains("MAX", StringComparison.Ordinal) || raw.Contains("SET_", StringComparison.Ordinal))))
            return Kind.CurrentSetpoint;
        if (raw.Contains("TEMP", StringComparison.Ordinal) || raw.Contains("PHASEA", StringComparison.Ordinal)
            || raw.Contains("PHASEB", StringComparison.Ordinal) || raw.Contains("PHASEC", StringComparison.Ordinal)
            || raw.Contains("BEARING", StringComparison.Ordinal))
            return Kind.Temperature;
        if (raw.Contains("RIVER", StringComparison.Ordinal) || raw.Contains("DISCHARGE", StringComparison.Ordinal)
            || raw.Contains("LEVEL", StringComparison.Ordinal) || raw.Contains("MUC", StringComparison.Ordinal))
            return Kind.WaterLevel;
        if (raw.Contains("U12", StringComparison.Ordinal) || raw.Contains("U23", StringComparison.Ordinal)
            || raw.Contains("U31", StringComparison.Ordinal) || raw.Contains("VOLTAGE", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(raw, @"(^|[^A-Z0-9])U[123]([^A-Z0-9]|$)"))
            return Kind.Voltage;
        if (raw.Contains("CURRENT", StringComparison.Ordinal) || raw.Contains("CURENT", StringComparison.Ordinal)
            || raw.Contains("I_PH", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(raw, @"(^|[^A-Z0-9])I[123]([^A-Z0-9]|$)"))
            return Kind.Current;
        if (raw.Contains("SPEED", StringComparison.Ordinal) || raw.Contains("RPM", StringComparison.Ordinal))
            return Kind.Speed;
        if (raw.Contains("FREQ", StringComparison.Ordinal) || raw.Contains("TAN_SO", StringComparison.Ordinal))
            return Kind.Frequency;
        if (raw.Contains("TOTAL_KW", StringComparison.Ordinal) || (raw.Contains("POWER", StringComparison.Ordinal)
            && !raw.Contains("FACTOR", StringComparison.Ordinal) && !raw.Contains("PF", StringComparison.Ordinal)))
            return Kind.Power;
        if (raw.Contains("POWER_FACTOR", StringComparison.Ordinal) || raw.Contains("_PF", StringComparison.Ordinal)
            || raw.EndsWith("PF", StringComparison.Ordinal) || raw.Contains("HS_CONG", StringComparison.Ordinal))
            return Kind.PowerFactor;
        if (raw.Contains("KWH", StringComparison.Ordinal) || raw.Contains("ENERGY", StringComparison.Ordinal))
            return Kind.Energy;
        if (raw.Contains("TIME_RUN", StringComparison.Ordinal) || raw.Contains("TOTAL_TIME", StringComparison.Ordinal))
            return Kind.Runtime;
        if (raw.Contains("FB_STOP", StringComparison.Ordinal) || raw.Contains("_STOP", StringComparison.Ordinal))
            return Kind.Stop;
        if (raw.Contains("FB_RUN", StringComparison.Ordinal)
            || raw.Contains("CTRL_RUN", StringComparison.Ordinal)
            || (raw.Contains("RUN", StringComparison.Ordinal) && !raw.Contains("RUNTIME", StringComparison.Ordinal)))
            return Kind.Run;

        return Kind.Unknown;
    }
}

public sealed class PumpDeviceSimState
{
    public bool Running { get; set; }
    public double SpeedRpm { get; set; }
    public double CurrentA { get; set; }
    public double TemperatureC { get; set; } = 35;
    public bool Fault { get; set; }
    public int RuntimeMinutes { get; set; }
    public double VoltageRs { get; set; } = 380;
    public double WaterRiverM { get; set; } = 3.2;
}

/// <summary>Produces next tag values for one device based on prior pump state.</summary>
public static class PumpPhysicsSimulator
{
    private const double TargetRpm = 1450;
    private const double NominalCurrent = 95;
    private const double TempSetpointC = 37;

    public static void Tick(PumpDeviceSimState state, Random rng)
    {
        // Visible start/stop/fault so status banners change often.
        if (state.Fault)
        {
            if (rng.NextDouble() < 0.25)
                state.Fault = false;
        }
        else if (rng.NextDouble() < 0.03)
        {
            state.Fault = true;
            state.Running = false;
        }

        if (!state.Fault)
        {
            if (state.Running)
            {
                if (rng.NextDouble() < 0.12)
                    state.Running = false;
            }
            else if (rng.NextDouble() < 0.18)
            {
                state.Running = true;
            }
        }

        var wobble = (rng.NextDouble() - 0.5) * 2; // -1..1

        if (state.Running && !state.Fault)
        {
            state.SpeedRpm = TargetRpm + rng.Next(-80, 81);
            var load = 0.55 + rng.NextDouble() * 0.55;
            state.CurrentA = Math.Round(NominalCurrent * load * (state.SpeedRpm / TargetRpm) + wobble * 8, 2);
            state.TemperatureC = Math.Clamp(
                state.TemperatureC + 0.8 + rng.NextDouble() * 1.2 + wobble * 0.5,
                35,
                98);
            state.VoltageRs = Math.Round(380 + wobble * 12 + rng.NextDouble() * 6, 2);
            state.RuntimeMinutes++;
        }
        else
        {
            state.SpeedRpm = Math.Max(0, state.SpeedRpm - 220);
            if (state.SpeedRpm < 20)
                state.SpeedRpm = 0;
            state.CurrentA = state.SpeedRpm <= 0
                ? Math.Round(Math.Abs(wobble) * 1.5, 2)
                : Math.Round(state.CurrentA * 0.45 + wobble * 2, 2);
            state.TemperatureC = Math.Clamp(
                state.TemperatureC - 0.6 - rng.NextDouble() * 0.5 + wobble * 0.3,
                28,
                98);
            state.VoltageRs = Math.Round(380 + wobble * 8, 2);
        }

        // River level always drifts a bit (shared feel on cards).
        state.WaterRiverM = Math.Clamp(
            state.WaterRiverM + (rng.NextDouble() - 0.5) * 0.15,
            2.5,
            6.5);
    }

    public static (object? Value, string Quality) ResolveTag(
        PumpTagClassifier.Kind kind,
        string dataType,
        PumpDeviceSimState state,
        Random rng,
        long tagId = 0)
    {
        var quality = state.Fault ? TagQualityNames.Bad : TagQualityNames.Good;
        if (state.Fault && kind is PumpTagClassifier.Kind.Current or PumpTagClassifier.Kind.Speed)
            quality = TagQualityNames.Uncertain;

        // Per-tag micro-jitter so every field moves on screen even when physics is steady.
        var jitter = RealtimeValueGenerator.GenerateAnalog(tagId == 0 ? rng.Next() : tagId, 0, 1.0, 1.8);

        return kind switch
        {
            PumpTagClassifier.Kind.Run => (state.Running && !state.Fault, quality),
            PumpTagClassifier.Kind.Stop => (!state.Running || state.Fault, TagQualityNames.Good),
            PumpTagClassifier.Kind.Fault => (state.Fault, TagQualityNames.Good),
            PumpTagClassifier.Kind.Speed => (Coerce(dataType, state.SpeedRpm + jitter * 15), quality),
            PumpTagClassifier.Kind.Current => (Coerce(dataType, Math.Max(0, state.CurrentA + jitter * 6)), quality),
            PumpTagClassifier.Kind.Temperature => (Coerce(dataType, Math.Round(state.TemperatureC + jitter * 2.5, 1)), quality),
            PumpTagClassifier.Kind.TempSetpoint => (Coerce(dataType, TempSetpointC + (tagId % 5) * 0.4), TagQualityNames.Good),
            PumpTagClassifier.Kind.CurrentSetpoint => (Coerce(dataType, 36 + (tagId % 4) * 0.5), TagQualityNames.Good),
            PumpTagClassifier.Kind.Voltage => (Coerce(dataType, state.VoltageRs + jitter * 5), quality),
            PumpTagClassifier.Kind.Frequency => (Coerce(dataType, 50 + jitter * 0.8), quality),
            PumpTagClassifier.Kind.PowerFactor => (Coerce(dataType, Math.Clamp(0.82 + jitter * 0.08, 0.5, 1.0)), quality),
            PumpTagClassifier.Kind.Power => (Coerce(dataType, Math.Max(0, state.CurrentA * 0.38 * Math.Sqrt(3) * 0.4 + jitter * 5)), quality),
            PumpTagClassifier.Kind.Energy => (Coerce(dataType, 1200 + state.RuntimeMinutes * 0.8 + Math.Abs(jitter) * 3), quality),
            PumpTagClassifier.Kind.WaterLevel => (Coerce(dataType, Math.Round(state.WaterRiverM + jitter * 0.05, 2)), TagQualityNames.Good),
            PumpTagClassifier.Kind.Runtime => (Coerce(dataType, state.RuntimeMinutes), TagQualityNames.Good),
            _ => (RealtimeValueGenerator.Generate(dataType, tagId == 0 ? rng.Next() : tagId), TagQualityNames.Good)
        };
    }

    private static object Coerce(string? dataType, double value)
    {
        var kind = (dataType ?? string.Empty).Trim().ToLowerInvariant();
        if (kind is "bool" or "boolean" or "bit")
            return value > 0.5;
        // Prefer decimal display for temps/electrical even if PLC type is int.
        if (kind is "int" or "integer" or "word" or "dword" or "byte" or "sint" or "dint")
            return Math.Round(value, 1);
        return Math.Round(value, 2);
    }
}

/// <summary>In-memory per-device pump states for the hosted simulator.</summary>
public sealed class PumpSimulationStateStore
{
    private readonly ConcurrentDictionary<long, PumpDeviceSimState> _byDevice = new();

    public PumpDeviceSimState GetOrAdd(long deviceId) =>
        _byDevice.GetOrAdd(deviceId, id => new PumpDeviceSimState
        {
            Running = id % 3 != 0,
            SpeedRpm = id % 3 != 0 ? 1450 : 0,
            CurrentA = id % 3 != 0 ? 90 : 0,
            TemperatureC = 38 + (id % 10),
            Fault = false,
            WaterRiverM = 3.0 + (id % 5) * 0.2
        });
}
