using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Services;

public interface IBalancingTestService
{
    TestStep CurrentStep { get; }
    bool IsStable { get; }
    BalancingResult? Result { get; }
    double LastLeftAmplitude { get; }
    double LastLeftPhase { get; }
    double LastRightAmplitude { get; }
    double LastRightPhase { get; }
    event EventHandler<TestStep>? StepChanged;
    event EventHandler<bool>? StabilityChanged;

    void StartTest(double targetSpeed);

    /// <summary>
    /// Records current vibration values for the current step,
    /// using the specified trial mass and angle (for LeftTrial / RightTrial steps).
    /// </summary>
    void RecordCurrentValues(double trialMass, double trialAngle);

    /// <summary>
    /// Records current values with recipe-specified trial parameters.
    /// For LeftTrial: trialMass = recipe.TrialMass1, trialAngle = recipe.TrialAngle1.
    /// For RightTrial: trialMass = recipe.TrialMass2, trialAngle = recipe.TrialAngle2.
    /// For other steps: trialMass and trialAngle are ignored.
    /// </summary>
    void RecordCurrentValues(Recipe recipe);

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
