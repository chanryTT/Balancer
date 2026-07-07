using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Interfaces;

public interface IDataAcquisitionService
{
    bool IsConnected { get; }
    event EventHandler<VibrationData>? DataReceived;
    event EventHandler<bool>? ConnectionStateChanged;

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    (double LeftAmplitude, double RightAmplitude, double LeftPhase, double RightPhase, double Speed) GetCurrentValues();
    VibrationData[] GetWaveformData(int windowSeconds = 5);
}
