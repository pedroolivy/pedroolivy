namespace ProfileGenerator.Scenes.Terrain;

internal static class TerrainTimeline
{
    public const double HeaderAt = 0.2;
    public const double HeaderSeconds = 0.8;

    public const double NameAt = 0.3;
    public const double NameStrokeStep = 0.045;
    public const double NameStrokeSeconds = 1.1;

    public const double RoleAt = 1.6;
    public const double RoleSeconds = 0.9;

    public const double FlightAt = 0.15;
    public const double FlightSeconds = 5.0;

    public const double DrawAt = 0.1;
    public const double DrawStep = 0.025;
    public const double DrawSeconds = 1.0;

    public const double LiftAt = 0.35;
    public const double LiftStep = 0.045;
    public const double LiftSeconds = 1.7;

    public const double Survey = 4.5;
    public const double SurveyGridStep = 0.025;

    public const double RouteAt = 5.7;
    public const double RouteSeconds = 3.4;
    public const double Arrival = RouteAt + RouteSeconds;

    public const double MonthLightSeconds = 1.6;

    public const double FlowAt = Arrival + 0.5;
    public const double FlowPeriod = 1.1;
    public const int FlowCycles = 34;

    public const double ScanAt = Survey + 0.2;
    public const double ScanStep = 0.05;
    public const double ScanCycle = 7.5;
    public const int ScanCycles = 5;

    public const double BeaconAt = Arrival + 4;
    public const double BeaconCycle = 4;
    public const int BeaconCycles = 8;

    public const double FlagWaveAt = Arrival + 1.1;
    public const double FlagWaveSeconds = 1.3;
    public const int FlagWaves = 24;
}
