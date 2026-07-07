namespace BalanceSystem.Core.Models;

public readonly record struct VibrationData(
    DateTime Timestamp,
    double LeftChannel,
    double RightChannel,
    bool TachPulse,
    double Speed
);
