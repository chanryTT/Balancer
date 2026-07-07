using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Services;

public interface IBalancingTestService
{
    TestStep CurrentStep { get; }
    bool IsStable { get; }
    BalancingResult? Result { get; }
    event EventHandler<TestStep>? StepChanged;
    event EventHandler<bool>? StabilityChanged;

    void StartTest(double targetSpeed);
    void RecordCurrentValues();
    void AdvanceStep();
    void Reset();
    StabilityInfo GetStabilityInfo();
}

public record StabilityInfo(
    bool IsStable,
    double AmplitudeVariation,
    double PhaseVariation,
    double SpeedDeviation
);
