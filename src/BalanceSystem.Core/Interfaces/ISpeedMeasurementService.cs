namespace BalanceSystem.Core.Interfaces;

public interface ISpeedMeasurementService
{
    double CurrentSpeed { get; }
    event EventHandler<double>? SpeedChanged;
    event EventHandler? ZeroPulseDetected;
    double GetCurrentAngle();
}
