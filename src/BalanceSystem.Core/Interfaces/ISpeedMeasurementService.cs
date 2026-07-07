namespace BalanceSystem.Core.Interfaces;

// TODO: Phase 2 — implement real hardware speed measurement (tachometer/encoder)
public interface ISpeedMeasurementService
{
    double CurrentSpeed { get; }
    event EventHandler<double>? SpeedChanged;
    event EventHandler? ZeroPulseDetected;
    double GetCurrentAngle();
}
