namespace BalanceSystem.Core.Models;

public record BalancingResult
{
    public double LeftMass { get; init; }
    public double LeftAngle { get; init; }
    public double RightMass { get; init; }
    public double RightAngle { get; init; }
    public double ResidualLeftAmplitude { get; init; }
    public double ResidualRightAmplitude { get; init; }
    public bool IsBalanced { get; init; }
    public DateTime CalculatedAt { get; init; } = DateTime.Now;
}
