namespace BalanceSystem.Shared;

public static class Constants
{
    public const double DefaultSampleRate = 6400.0;
    public const int DefaultSpeedRpm = 1500;
    public static readonly int[] SpeedOptions = { 500, 1000, 1500, 2000, 2500, 3000 };
    public const double StabilityCheckDurationSeconds = 3.0;
    public const double AmplitudeStabilityThreshold = 0.05;
    public const double PhaseStabilityThreshold = 2.0;
    public const double SpeedStabilityPercent = 2.0;
}
