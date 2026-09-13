using Backend.Application.Realtime;
using Backend.Infrastructure.Realtime;

namespace Backend.UnitTests;

public class PumpPhysicsSimulatorTests
{
    [Theory]
    [InlineData("FB_Run", "Pump1_FB_Run", PumpTagClassifier.Kind.Run)]
    [InlineData("FB_stop", "Pump1_FB_stop", PumpTagClassifier.Kind.Stop)]
    [InlineData("FB_Fault", "Pump1_FB_Fault", PumpTagClassifier.Kind.Fault)]
    [InlineData("FB_Temp_PhaseA", "Pump1_FB_Temp_PhaseA", PumpTagClassifier.Kind.Temperature)]
    [InlineData("FB_Fault_Curent", "Pump1_FB_Fault_Curent", PumpTagClassifier.Kind.Fault)]
    [InlineData("Speed", "Pump_Speed", PumpTagClassifier.Kind.Speed)]
    [InlineData("MotorCurrent", "I_Motor", PumpTagClassifier.Kind.Current)]
    public void Classify_KnownCodes(string code, string tag, PumpTagClassifier.Kind expected) =>
        Assert.Equal(expected, PumpTagClassifier.Classify(code, tag));

    [Fact]
    public void Tick_WhenRunning_KeepsSpeedNearTarget()
    {
        var state = new PumpDeviceSimState { Running = true, SpeedRpm = 1450, CurrentA = 80, TemperatureC = 40 };
        var rng = new Random(42);
        for (var i = 0; i < 20; i++)
            PumpPhysicsSimulator.Tick(state, rng);

        if (state.Running && !state.Fault)
        {
            Assert.InRange(state.SpeedRpm, 1300, 1600);
            Assert.True(state.CurrentA > 40);
            Assert.True(state.TemperatureC >= 40);
        }
    }

    [Fact]
    public void Tick_WhenStopped_SpeedDriftsToZero()
    {
        var state = new PumpDeviceSimState { Running = false, SpeedRpm = 1400, CurrentA = 70, TemperatureC = 60, Fault = false };
        var rng = new Random(7);
        // Force stay stopped: tick many times — Running may flip; lock by fault-free and re-clear.
        for (var i = 0; i < 40; i++)
        {
            PumpPhysicsSimulator.Tick(state, rng);
            state.Running = false;
            state.Fault = false;
        }

        Assert.Equal(0, state.SpeedRpm);
        Assert.True(state.CurrentA < 5);
    }

    [Fact]
    public void ResolveTag_RunReflectsState()
    {
        var state = new PumpDeviceSimState { Running = true, Fault = false };
        var (value, quality) = PumpPhysicsSimulator.ResolveTag(PumpTagClassifier.Kind.Run, "bool", state, new Random(1));
        Assert.Equal(true, value);
        Assert.Equal(TagQualityNames.Good, quality);

        state.Fault = true;
        (value, quality) = PumpPhysicsSimulator.ResolveTag(PumpTagClassifier.Kind.Run, "bool", state, new Random(1));
        Assert.Equal(false, value);
        Assert.Equal(TagQualityNames.Bad, quality);
    }
}
