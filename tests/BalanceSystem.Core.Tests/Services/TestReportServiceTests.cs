using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using BalanceSystem.Infrastructure.Database;
using BalanceSystem.Infrastructure.Reporting;
using FluentAssertions;
using FreeSql;
using Microsoft.Extensions.Logging.Abstractions;

namespace BalanceSystem.Core.Tests.Services;

public class TestReportServiceTests : IDisposable
{
    private readonly IFreeSql _db;
    private readonly RecipeService _recipeService;
    private readonly TestReportService _reportService;
    private readonly string _outputDir;

    public TestReportServiceTests()
    {
        _db = new FreeSqlBuilder()
            .UseConnectionString(DataType.Sqlite, "Data Source=:memory:")
            .UseAutoSyncStructure(true)
            .Build();
        _db.CodeFirst.SyncStructure<Recipe>();
        _db.CodeFirst.SyncStructure<TestRecord>();

        var dbContext = new AppDbContext("Data Source=:memory:");
        typeof(AppDbContext).GetProperty("Orm")!.SetValue(dbContext, _db);

        _recipeService = new RecipeService(dbContext, NullLogger<RecipeService>.Instance);
        _reportService = new TestReportService(NullLogger<TestReportService>.Instance);
        _outputDir = Path.Combine(Path.GetTempPath(), "BalanceTestReports");
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, true);
    }

    [Fact]
    public async Task GenerateReportAsync_ValidData_CreatesPdfFile()
    {
        var recipe = await _recipeService.CreateAsync(new Recipe
        {
            Name = "测试转子",
            RatedSpeed = 1500,
            AllowUnbalanceLeft = 0.5,
            AllowUnbalanceRight = 0.5
        });

        var record = new TestRecord
        {
            RecipeId = recipe.Id,
            UserId = 1,
            Speed = 1500,
            InitialLeftAmplitude = 2.5, InitialLeftPhase = 30,
            InitialRightAmplitude = 1.8, InitialRightPhase = 150,
            LeftTrialMass = 50, LeftTrialAngle = 0,
            RightTrialMass = 50, RightTrialAngle = 0,
            LeftCorrectionMass = 12.5, LeftCorrectionAngle = 45,
            RightCorrectionMass = 8.3, RightCorrectionAngle = 225,
            ResidualLeft = 0.15, ResidualRight = 0.12,
            IsPassed = true
        };

        var filePath = Path.Combine(_outputDir, "test_report.pdf");
        var result = await _reportService.GenerateReportAsync(record, recipe, filePath);

        result.Should().Be(filePath);
        File.Exists(result).Should().BeTrue();
        new FileInfo(result).Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task GenerateReportAsync_NullRecipe_StillGeneratesPdf()
    {
        var record = new TestRecord
        {
            RecipeId = 99,
            Speed = 2000,
            InitialLeftAmplitude = 1.0, InitialLeftPhase = 0,
            InitialRightAmplitude = 1.0, InitialRightPhase = 180,
            LeftCorrectionMass = 5.0, LeftCorrectionAngle = 0,
            RightCorrectionMass = 5.0, RightCorrectionAngle = 180,
            IsPassed = false
        };

        var filePath = Path.Combine(_outputDir, "no_recipe_report.pdf");
        var result = await _reportService.GenerateReportAsync(record, null, filePath);

        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateReportAsync_OverwritesExistingFile()
    {
        var record = new TestRecord
        {
            Speed = 1000,
            InitialLeftAmplitude = 1.0, InitialLeftPhase = 0,
            InitialRightAmplitude = 1.0, InitialRightPhase = 0,
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 0,
            IsPassed = true
        };
        var filePath = Path.Combine(_outputDir, "overwrite.pdf");
        await File.WriteAllTextAsync(filePath, "dummy");

        var result = await _reportService.GenerateReportAsync(record, null, filePath);

        File.Exists(result).Should().BeTrue();
        var content = await File.ReadAllBytesAsync(result);
        content[0].Should().Be(0x25); // PDF magic '%'
    }
}
