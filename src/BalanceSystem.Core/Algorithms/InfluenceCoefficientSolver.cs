using System.Numerics;
using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Algorithms;

public class InfluenceCoefficientSolver
{
    private Complex _vInitialLeft, _vInitialRight;
    private Complex _vLeftTrialLeft, _vLeftTrialRight;
    private Complex _vRightTrialLeft, _vRightTrialRight;
    private Complex _trialLeft, _trialRight;
    private bool _hasInitial, _hasLeftTrial, _hasRightTrial;

    public void AddInitialRun(double leftAmplitude, double leftPhase,
                               double rightAmplitude, double rightPhase)
    {
        _vInitialLeft = FromPolar(leftAmplitude, leftPhase);
        _vInitialRight = FromPolar(rightAmplitude, rightPhase);
        _hasInitial = true;
    }

    public void AddLeftTrialRun(double leftAmplitude, double leftPhase,
                                 double rightAmplitude, double rightPhase,
                                 double trialMass, double trialAngle)
    {
        _vLeftTrialLeft = FromPolar(leftAmplitude, leftPhase);
        _vLeftTrialRight = FromPolar(rightAmplitude, rightPhase);
        _trialLeft = FromPolar(trialMass, trialAngle);
        _hasLeftTrial = true;
    }

    public void AddRightTrialRun(double leftAmplitude, double leftPhase,
                                  double rightAmplitude, double rightPhase,
                                  double trialMass, double trialAngle)
    {
        _vRightTrialLeft = FromPolar(leftAmplitude, leftPhase);
        _vRightTrialRight = FromPolar(rightAmplitude, rightPhase);
        _trialRight = FromPolar(trialMass, trialAngle);
        _hasRightTrial = true;
    }

    public BalancingResult Solve()
    {
        if (!_hasInitial || !_hasLeftTrial || !_hasRightTrial)
            throw new InvalidOperationException(
                "All 4 steps are required: initial run, left trial, right trial, and then solve.");

        Complex a11 = (_vLeftTrialLeft - _vInitialLeft) / _trialLeft;
        Complex a12 = (_vRightTrialLeft - _vInitialLeft) / _trialRight;
        Complex a21 = (_vLeftTrialRight - _vInitialRight) / _trialLeft;
        Complex a22 = (_vRightTrialRight - _vInitialRight) / _trialRight;

        Complex det = a11 * a22 - a12 * a21;

        if (Complex.Abs(det) < 1e-15)
        {
            return new BalancingResult
            {
                LeftMass = 0, LeftAngle = 0,
                RightMass = 0, RightAngle = 0,
                ResidualLeftAmplitude = _vInitialLeft.Magnitude,
                ResidualRightAmplitude = _vInitialRight.Magnitude,
                IsBalanced = false
            };
        }

        Complex negVLeft = -_vInitialLeft;
        Complex negVRight = -_vInitialRight;

        Complex correctionLeft = (negVLeft * a22 - negVRight * a12) / det;
        Complex correctionRight = (negVRight * a11 - negVLeft * a21) / det;

        Complex residualLeft = _vInitialLeft + a11 * correctionLeft + a12 * correctionRight;
        Complex residualRight = _vInitialRight + a21 * correctionLeft + a22 * correctionRight;

        return new BalancingResult
        {
            LeftMass = Math.Round(correctionLeft.Magnitude, 2),
            LeftAngle = Math.Round(ToDegrees(correctionLeft.Phase), 1),
            RightMass = Math.Round(correctionRight.Magnitude, 2),
            RightAngle = Math.Round(ToDegrees(correctionRight.Phase), 1),
            ResidualLeftAmplitude = Math.Round(residualLeft.Magnitude, 4),
            ResidualRightAmplitude = Math.Round(residualRight.Magnitude, 4),
            IsBalanced = residualLeft.Magnitude < 1.0 && residualRight.Magnitude < 1.0,
            CalculatedAt = DateTime.Now
        };
    }

    private static Complex FromPolar(double magnitude, double phaseDegrees)
    {
        double radians = phaseDegrees * Math.PI / 180.0;
        return new Complex(magnitude * Math.Cos(radians), magnitude * Math.Sin(radians));
    }

    private static double ToDegrees(double radians)
    {
        double deg = radians * 180.0 / Math.PI;
        if (deg < 0) deg += 360.0;
        return deg;
    }
}
