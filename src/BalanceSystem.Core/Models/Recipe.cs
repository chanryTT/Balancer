using FreeSql.DataAnnotations;

namespace BalanceSystem.Core.Models;

[Table(Name = "Recipes")]
public class Recipe
{
    [Column(IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    [Column(StringLength = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    public int RatedSpeed { get; set; } = 1500;

    /// <summary>允许不平衡量 — 左面 (μm)</summary>
    public double AllowUnbalanceLeft { get; set; }

    /// <summary>允许不平衡量 — 右面 (μm)</summary>
    public double AllowUnbalanceRight { get; set; }

    /// <summary>左面试重质量 (g)</summary>
    public double TrialMass1 { get; set; }

    /// <summary>左面试重角度 (°)</summary>
    public double TrialAngle1 { get; set; }

    /// <summary>右面试重质量 (g)</summary>
    public double TrialMass2 { get; set; }

    /// <summary>右面试重角度 (°)</summary>
    public double TrialAngle2 { get; set; }

    /// <summary>左面校准系数</summary>
    public double CalibrationFactorLeft { get; set; } = 1.0;

    /// <summary>右面校准系数</summary>
    public double CalibrationFactorRight { get; set; } = 1.0;

    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime? UpdatedTime { get; set; }
}
