namespace Backend.Application.Realtime;

public static class ScadaRealtimeGroups
{
    public static string StationScreen(long stationId, string screenSlug) =>
        $"station:{stationId}:screen:{screenSlug}";

    public static string StationScreenDevice(long stationId, string screenSlug, long deviceId) =>
        $"station:{stationId}:screen:{screenSlug}:device:{deviceId}";
}
