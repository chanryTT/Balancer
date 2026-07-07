using BalanceSystem.Core.Algorithms;
using BalanceSystem.Core.Models;
using FluentAssertions;

namespace BalanceSystem.Core.Tests.Algorithms;

public class InfluenceCoefficientSolverTests
{
    [Fact]
    public void Solve_WithSyntheticData_ReturnsFiniteCorrectionWeights()
    {
        var solver = new InfluenceCoefficientSolver();

        solver.AddInitialRun(
            leftAmplitude: 55.0, leftPhase: 35.0,
            rightAmplitude: 42.0, rightPhase: 195.0);

        solver.AddLeftTrialRun(
            leftAmplitude: 60.0, leftPhase: 42.0,
            rightAmplitude: 38.0, rightPhase: 200.0,
            trialMass: 50.0, trialAngle: 90.0);

        solver.AddRightTrialRun(
            leftAmplitude: 48.0, leftPhase: 28.0,
            rightAmplitude: 50.0, rightPhase: 210.0,
            trialMass: 50.0, trialAngle: 270.0);

        var result = solver.Solve();

        result.Should().NotBeNull();
        result.LeftMass.Should().BePositive();
        result.RightMass.Should().BePositive();
        result.LeftAngle.Should().BeInRange(0.0, 360.0);
        result.RightAngle.Should().BeInRange(0.0, 360.0);
        result.ResidualLeftAmplitude.Should().BeLessThan(55.0);
        result.ResidualRightAmplitude.Should().BeLessThan(42.0);
    }

    [Fact]
    public void Solve_IncompleteSteps_ThrowsInvalidOperationException()
    {
        var solver = new InfluenceCoefficientSolver();
        solver.AddInitialRun(10, 0, 10, 0);

        Action act = () => solver.Solve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Solve_IdenticalTrials_ProducesFiniteResult()
    {
        var solver = new InfluenceCoefficientSolver();
        solver.AddInitialRun(10.0, 45.0, 10.0, 135.0);
        solver.AddLeftTrialRun(10.001, 45.001, 10.001, 135.001, 0.001, 0.0);
        solver.AddRightTrialRun(10.0, 45.0, 10.001, 135.001, 0.001, 0.0);

        var result = solver.Solve();
        result.LeftMass.Should().NotBe(double.NaN);
        result.RightMass.Should().NotBe(double.NaN);
    }
}
