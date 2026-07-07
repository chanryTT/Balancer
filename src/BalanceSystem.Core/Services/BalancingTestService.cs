using BalanceSystem.Core.Algorithms;
using BalanceSystem.Core.Interfaces;
using BalanceSystem.Core.Models;
using BalanceSystem.Shared;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Core.Services;

public class BalancingTestService : IBalancingTestService
{
    private readonly IDataAcquisitionService _dataAcquisition;
    private readonly ILogger<BalancingTestService> _logger;
    private readonly InfluenceCoefficientSolver _solver = new();
    private readonly Queue<StabilitySample> _stabilityWindow = new();
    private readonly List<double> _phaseBuffer = new();
    private double _targetSpeed;

    public TestStep CurrentStep { get; private set; } = TestStep.Idle;
    public bool IsStable { get; private set; }
    public BalancingResult? Result { get; private set; }
    public event EventHandler<TestStep>? StepChanged;
    public event EventHandler<bool>? StabilityChanged;

    public BalancingTestService(IDataAcquisitionService dataAcquisition,
                                 ILogger<BalancingTestService> logger)
    {
        _dataAcquisition = dataAcquisition;
        _logger = logger;
        _dataAcquisition.DataReceived += OnDataReceived;
    }

    public void StartTest(double targetSpeed)
    {
        _targetSpeed = targetSpeed;
        CurrentStep = TestStep.InitialRun;
        _stabilityWindow.Clear();
        IsStable = false;
        Result = null;
        _logger.LogInformation("Balancing test started at {Speed} RPM", targetSpeed);
        StepChanged?.Invoke(this, CurrentStep);
    }

    public void RecordCurrentValues()
    {
        // Fallback: use hardcoded 50g@0° for backward compatibility
        RecordCurrentValues(trialMass: 50, trialAngle: 0);
    }

    public void RecordCurrentValues(double trialMass, double trialAngle)
    {
        var waveform = _dataAcquisition.GetWaveformData(1);
        if (waveform.Length < 100) return;

        double speed = waveform.Average(d => d.Speed);
        var leftSignal = waveform.Select(d => d.LeftChannel).ToArray();
        var rightSignal = waveform.Select(d => d.RightChannel).ToArray();

        var (leftAmp, leftPhase) = FftCalculator.ExtractFundamental(leftSignal, Constants.DefaultSampleRate, speed);
        var (rightAmp, rightPhase) = FftCalculator.ExtractFundamental(rightSignal, Constants.DefaultSampleRate, speed);

        switch (CurrentStep)
        {
            case TestStep.InitialRun:
                _solver.AddInitialRun(leftAmp, leftPhase, rightAmp, rightPhase);
                _logger.LogInformation("Initial run recorded: L={LA:F2} angle {LP:F1}, R={RA:F2} angle {RP:F1}",
                    leftAmp, leftPhase, rightAmp, rightPhase);
                break;
            case TestStep.LeftTrial:
                _solver.AddLeftTrialRun(leftAmp, leftPhase, rightAmp, rightPhase, trialMass, trialAngle);
                _logger.LogInformation("Left trial recorded: mass={M}g, angle={A}°", trialMass, trialAngle);
                break;
            case TestStep.RightTrial:
                _solver.AddRightTrialRun(leftAmp, leftPhase, rightAmp, rightPhase, trialMass, trialAngle);
                _logger.LogInformation("Right trial recorded: mass={M}g, angle={A}°", trialMass, trialAngle);
                break;
            case TestStep.Retest:
                _logger.LogInformation("Retest recorded: L={LA:F2} angle {LP:F1}, R={RA:F2} angle {RP:F1}",
                    leftAmp, leftPhase, rightAmp, rightPhase);
                break;
        }
    }

    public void RecordCurrentValues(Recipe recipe)
    {
        double trialMass = CurrentStep switch
        {
            TestStep.LeftTrial => recipe.TrialMass1,
            TestStep.RightTrial => recipe.TrialMass2,
            _ => 50
        };
        double trialAngle = CurrentStep switch
        {
            TestStep.LeftTrial => recipe.TrialAngle1,
            TestStep.RightTrial => recipe.TrialAngle2,
            _ => 0
        };
        RecordCurrentValues(trialMass, trialAngle);
    }

    public void AdvanceStep()
    {
        CurrentStep = CurrentStep switch
        {
            TestStep.Idle => TestStep.InitialRun,
            TestStep.InitialRun => TestStep.LeftTrial,
            TestStep.LeftTrial => TestStep.RightTrial,
            TestStep.RightTrial => TestStep.Calculation,
            TestStep.Calculation => TestStep.Completed,
            TestStep.Completed => TestStep.Retest,
            TestStep.Retest => TestStep.Completed,
            _ => TestStep.Idle
        };

        if (CurrentStep == TestStep.Calculation)
        {
            Result = _solver.Solve();
            _logger.LogInformation(
                "Calculation complete: Left={LM:F1}g angle {LA:F1}, Right={RM:F1}g angle {RA:F1}, Balanced={B}",
                Result.LeftMass, Result.LeftAngle, Result.RightMass, Result.RightAngle, Result.IsBalanced);
        }

        StepChanged?.Invoke(this, CurrentStep);
    }

    public void Reset()
    {
        CurrentStep = TestStep.Idle;
        IsStable = false;
        Result = null;
        _stabilityWindow.Clear();
        _phaseBuffer.Clear();
        StepChanged?.Invoke(this, CurrentStep);
    }

    public StabilityInfo GetStabilityInfo()
    {
        if (_stabilityWindow.Count < 10)
            return new StabilityInfo(false, 999, 999, 999);

        var samples = _stabilityWindow.ToArray();
        double ampVar = StdDev(samples.Select(s => s.Amplitude).ToArray())
            / (samples.Average(s => s.Amplitude) + 0.001);
        double phaseVar = StdDev(samples.Select(s => s.Phase).ToArray());
        double speedDev = Math.Abs(samples.Average(s => s.Speed) - _targetSpeed)
            / (_targetSpeed + 0.001) * 100;

        return new StabilityInfo(IsStable, ampVar, phaseVar, speedDev);
    }

    private void OnDataReceived(object? sender, VibrationData data)
    {
        // Maintain a rolling buffer for phase estimation
        _phaseBuffer.Add(data.LeftChannel);
        while (_phaseBuffer.Count > 1024)
            _phaseBuffer.RemoveAt(0);

        double phase = EstimatePhaseFromBuffer(data.Speed);

        _stabilityWindow.Enqueue(new StabilitySample(data.Speed, data.LeftChannel, phase));
        while (_stabilityWindow.Count > 100)
            _stabilityWindow.Dequeue();

        if (_stabilityWindow.Count >= 50)
        {
            var samples = _stabilityWindow.ToArray();
            double ampVar = StdDev(samples.Select(s => s.Amplitude).ToArray())
                / (samples.Average(s => s.Amplitude) + 0.001);
            double phaseVar = StdDev(samples.Select(s => s.Phase).ToArray());
            double speedDev = Math.Abs(samples.Average(s => s.Speed) - _targetSpeed)
                / (_targetSpeed + 0.001) * 100;

            bool wasStable = IsStable;
            IsStable = ampVar < Constants.AmplitudeStabilityThreshold
                    && phaseVar < Constants.PhaseStabilityThreshold
                    && speedDev < Constants.SpeedStabilityPercent;

            if (wasStable != IsStable)
                StabilityChanged?.Invoke(this, IsStable);
        }
    }

    /// <summary>
    /// Estimates the vibration phase at the fundamental frequency using a
    /// lightweight single-bin DFT correlation on the rolling phase buffer.
    /// </summary>
    private double EstimatePhaseFromBuffer(double speed)
    {
        if (_phaseBuffer.Count < 64 || speed < 1) return 0;

        double freq = speed / 60.0;
        double sampleRate = Constants.DefaultSampleRate;
        double sumSin = 0, sumCos = 0;
        int n = Math.Min(_phaseBuffer.Count, 1024);
        int offset = _phaseBuffer.Count - n;
        for (int i = 0; i < n; i++)
        {
            double angle = 2 * Math.PI * freq * i / sampleRate;
            double sample = _phaseBuffer[offset + i];
            sumSin += sample * Math.Sin(angle);
            sumCos += sample * Math.Cos(angle);
        }
        double phase = Math.Atan2(sumSin, sumCos) * 180.0 / Math.PI;
        if (phase < 0) phase += 360.0;
        return phase;
    }

    private static double StdDev(double[] values)
    {
        if (values.Length == 0) return 0;
        double avg = values.Average();
        return Math.Sqrt(values.Average(v => (v - avg) * (v - avg)));
    }

    private record struct StabilitySample(double Speed, double Amplitude, double Phase);
}
