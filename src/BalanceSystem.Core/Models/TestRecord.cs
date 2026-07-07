using FreeSql.DataAnnotations;

namespace BalanceSystem.Core.Models;

[Table(Name = "TestRecords")]
public class TestRecord
{
    [Column(IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public DateTime TestTime { get; set; } = DateTime.Now;
    public double Speed { get; set; }

    // ── Initial run ──
    public double InitialLeftAmplitude { get; set; }
    public double InitialLeftPhase { get; set; }
    public double InitialRightAmplitude { get; set; }
    public double InitialRightPhase { get; set; }

    // ── Left trial ──
    public double LeftTrialLeftAmplitude { get; set; }
    public double LeftTrialLeftPhase { get; set; }
    public double LeftTrialRightAmplitude { get; set; }
    public double LeftTrialRightPhase { get; set; }
    public double LeftTrialMass { get; set; }
    public double LeftTrialAngle { get; set; }

    // ── Right trial ──
    public double RightTrialLeftAmplitude { get; set; }
    public double RightTrialLeftPhase { get; set; }
    public double RightTrialRightAmplitude { get; set; }
    public double RightTrialRightPhase { get; set; }
    public double RightTrialMass { get; set; }
    public double RightTrialAngle { get; set; }

    // ── Retest (nullable — only populated if retest performed) ──
    public double? RetestLeftAmplitude { get; set; }
    public double? RetestLeftPhase { get; set; }
    public double? RetestRightAmplitude { get; set; }
    public double? RetestRightPhase { get; set; }

    // ── Correction result ──
    public double LeftCorrectionMass { get; set; }
    public double LeftCorrectionAngle { get; set; }
    public double RightCorrectionMass { get; set; }
    public double RightCorrectionAngle { get; set; }
    public double ResidualLeft { get; set; }
    public double ResidualRight { get; set; }
    public bool IsPassed { get; set; }

    [Column(StringLength = 500)]
    public string? Notes { get; set; }
}
