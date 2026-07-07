using System.Globalization;
using BalanceSystem.Core.Interfaces;
using BalanceSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.DataAcquisition;

public class CsvSimulationService : IDataAcquisitionService, IDisposable
{
    private readonly ILogger<CsvSimulationService> _logger;
    private readonly List<VibrationData> _buffer = new();
    private readonly object _bufferLock = new();
    private CancellationTokenSource? _cts;
    private Task? _playbackTask;
    private double _currentSpeed;

    public bool IsConnected { get; private set; }
    public event EventHandler<VibrationData>? DataReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    public CsvSimulationService(ILogger<CsvSimulationService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var csvDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Simulation");
        if (!Directory.Exists(csvDir))
        {
            _logger.LogWarning("Simulation data directory not found: {Dir}", csvDir);
            SetConnected(false);
            return;
        }

        var csvFiles = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (csvFiles.Length == 0)
        {
            _logger.LogWarning("No CSV files found in {Dir}", csvDir);
            SetConnected(false);
            return;
        }

        _logger.LogInformation("Starting CSV simulation with {Count} file(s)", csvFiles.Length);
        SetConnected(true);

        _playbackTask = Task.Run(async () =>
        {
            try
            {
                foreach (var file in csvFiles)
                {
                    _logger.LogInformation("Playing back: {File}", Path.GetFileName(file));
                    await PlaybackCsvFile(file, _cts.Token);
                    if (_cts.Token.IsCancellationRequested) break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CSV playback");
            }
        }, _cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }
        if (_playbackTask != null)
        {
            try { await _playbackTask; } catch { }
            _playbackTask = null;
        }
        SetConnected(false);
    }

    public (double LeftAmplitude, double RightAmplitude, double LeftPhase, double RightPhase, double Speed)
        GetCurrentValues()
    {
        VibrationData[] recent;
        lock (_bufferLock)
        {
            recent = _buffer.TakeLast(4096).ToArray();
        }

        if (recent.Length == 0)
            return (0, 0, 0, 0, 0);

        double leftRms = Math.Sqrt(recent.Average(d => d.LeftChannel * d.LeftChannel));
        double rightRms = Math.Sqrt(recent.Average(d => d.RightChannel * d.RightChannel));
        double avgSpeed = recent.Average(d => d.Speed);

        double leftPhase = EstimatePhase(recent.Select(d => d.LeftChannel).ToArray(), avgSpeed);
        double rightPhase = EstimatePhase(recent.Select(d => d.RightChannel).ToArray(), avgSpeed);

        return (leftRms, rightRms, leftPhase, rightPhase, avgSpeed);
    }

    public VibrationData[] GetWaveformData(int windowSeconds = 5)
    {
        int sampleCount = (int)(6400 * windowSeconds);
        lock (_bufferLock)
        {
            return _buffer.TakeLast(sampleCount).ToArray();
        }
    }

    private async Task PlaybackCsvFile(string filePath, CancellationToken ct)
    {
        var lines = await File.ReadAllLinesAsync(filePath, ct);
        int startLine = lines.Length > 0
            && (lines[0].StartsWith("Timestamp") || lines[0].StartsWith("时间戳")) ? 1 : 0;

        int lineIndex = 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            var parts = lines[i].Split(',');
            if (parts.Length < 4) continue;

            if (!double.TryParse(parts[0], CultureInfo.InvariantCulture, out double timestamp)) continue;
            if (!double.TryParse(parts[1], CultureInfo.InvariantCulture, out double left)) continue;
            if (!double.TryParse(parts[2], CultureInfo.InvariantCulture, out double right)) continue;
            if (!double.TryParse(parts[3], CultureInfo.InvariantCulture, out double tachRaw)) continue;

            bool tachPulse = tachRaw > 2.5;
            double speed = CalculateSpeed(tachPulse);

            var data = new VibrationData(DateTime.Now, left, right, tachPulse, speed);

            lock (_bufferLock)
            {
                _buffer.Add(data);
                while (_buffer.Count > 6400 * 10)
                    _buffer.RemoveAt(0);
            }

            DataReceived?.Invoke(this, data);

            lineIndex++;
            if (lineIndex % 64 == 0)
            {
                await Task.Delay(10, ct);
            }
        }
    }

    private double _lastPulseTime;
    private double CalculateSpeed(bool tachPulse)
    {
        if (tachPulse)
        {
            double now = DateTime.Now.Ticks / 10_000_000.0;
            if (_lastPulseTime > 0)
            {
                double period = now - _lastPulseTime;
                if (period > 0.001)
                    _currentSpeed = 60.0 / period;
            }
            _lastPulseTime = now;
        }
        return _currentSpeed;
    }

    private static double EstimatePhase(double[] samples, double speed)
    {
        if (samples.Length < 100 || speed < 1) return 0;
        double freq = speed / 60.0;
        double sampleRate = 6400.0;
        double sumSin = 0, sumCos = 0;
        int n = Math.Min(samples.Length, 4096);
        for (int i = 0; i < n; i++)
        {
            double angle = 2 * Math.PI * freq * i / sampleRate;
            sumSin += samples[i] * Math.Sin(angle);
            sumCos += samples[i] * Math.Cos(angle);
        }
        double phase = Math.Atan2(sumSin, sumCos) * 180.0 / Math.PI;
        if (phase < 0) phase += 360.0;
        return phase;
    }

    private void SetConnected(bool connected)
    {
        if (IsConnected != connected)
        {
            IsConnected = connected;
            ConnectionStateChanged?.Invoke(this, connected);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
