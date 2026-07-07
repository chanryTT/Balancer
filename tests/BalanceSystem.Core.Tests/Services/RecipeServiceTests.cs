using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using BalanceSystem.Infrastructure.Database;
using FluentAssertions;
using FreeSql;
using Microsoft.Extensions.Logging.Abstractions;

namespace BalanceSystem.Core.Tests.Services;

public class RecipeServiceTests : IDisposable
{
    private readonly IFreeSql _db;
    private readonly RecipeService _service;

    public RecipeServiceTests()
    {
        _db = new FreeSqlBuilder()
            .UseConnectionString(DataType.Sqlite, "Data Source=:memory:")
            .UseAutoSyncStructure(true)
            .Build();
        _db.CodeFirst.SyncStructure<Recipe>();

        var dbContext = new AppDbContext("Data Source=:memory:");
        // Replace Orm with our in-memory SQLite instance
        typeof(AppDbContext).GetProperty("Orm")!.SetValue(dbContext, _db);

        _service = new RecipeService(dbContext, NullLogger<RecipeService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_ValidRecipe_StoresAndReturnsWithId()
    {
        var recipe = new Recipe
        {
            Name = "测试转子A",
            RatedSpeed = 1500,
            AllowUnbalanceLeft = 0.5,
            AllowUnbalanceRight = 0.5,
            TrialMass1 = 30,
            TrialAngle1 = 45,
            TrialMass2 = 30,
            TrialAngle2 = 225
        };

        var result = await _service.CreateAsync(recipe);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("测试转子A");
        result.RatedSpeed.Should().Be(1500);
        result.CreateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleRecipes_ReturnsAll()
    {
        await _service.CreateAsync(new Recipe { Name = "R1", RatedSpeed = 1000 });
        await _service.CreateAsync(new Recipe { Name = "R2", RatedSpeed = 2000 });

        var all = await _service.GetAllAsync();

        all.Should().HaveCount(2);
        all.Select(r => r.Name).Should().Contain(["R1", "R2"]);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsRecipe()
    {
        var created = await _service.CreateAsync(new Recipe { Name = "Target", RatedSpeed = 1500 });

        var found = await _service.GetByIdAsync(created.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Target");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var found = await _service.GetByIdAsync(99999);
        found.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingRecipe_ModifiesAndReturnsTrue()
    {
        var created = await _service.CreateAsync(new Recipe { Name = "Old", RatedSpeed = 1000 });
        created.Name = "New";
        created.RatedSpeed = 2000;

        var result = await _service.UpdateAsync(created);

        result.Should().BeTrue();
        var updated = await _service.GetByIdAsync(created.Id);
        updated!.Name.Should().Be("New");
        updated.RatedSpeed.Should().Be(2000);
        updated.UpdatedTime.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_NonExistingRecipe_ReturnsFalse()
    {
        var result = await _service.UpdateAsync(new Recipe { Id = 99999, Name = "Nope" });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesAndReturnsTrue()
    {
        var created = await _service.CreateAsync(new Recipe { Name = "ToDelete" });

        var result = await _service.DeleteAsync(created.Id);

        result.Should().BeTrue();
        var found = await _service.GetByIdAsync(created.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(99999);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_ByPartialName_FindsMatches()
    {
        await _service.CreateAsync(new Recipe { Name = "发动机转子", RatedSpeed = 1500 });
        await _service.CreateAsync(new Recipe { Name = "电机转子", RatedSpeed = 3000 });
        await _service.CreateAsync(new Recipe { Name = "风扇", RatedSpeed = 1000 });

        var results = await _service.SearchAsync("转子");

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_BySpeed_FindsMatches()
    {
        await _service.CreateAsync(new Recipe { Name = "A", RatedSpeed = 1500 });
        await _service.CreateAsync(new Recipe { Name = "B", RatedSpeed = 3000 });

        var results = await _service.SearchAsync("1500");

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("A");
    }

    [Fact]
    public async Task ExportToJsonAsync_ReturnsValidJson()
    {
        var created = await _service.CreateAsync(new Recipe
        {
            Name = "ExportTest",
            RatedSpeed = 2000,
            TrialMass1 = 50,
            TrialAngle1 = 90
        });

        var json = await _service.ExportToJsonAsync(created.Id);

        json.Should().Contain("ExportTest");
        json.Should().Contain("2000");
        json.Should().Contain("\"trialMass1\"");
    }

    [Fact]
    public async Task ImportFromJsonAsync_ValidJson_CreatesRecipe()
    {
        var json = """{"name":"Imported","ratedSpeed":2500,"trialMass1":40,"trialAngle1":180,"trialMass2":40,"trialAngle2":0,"allowUnbalanceLeft":0.3,"allowUnbalanceRight":0.3,"calibrationFactorLeft":1.0,"calibrationFactorRight":1.0}""";

        var recipe = await _service.ImportFromJsonAsync(json);

        recipe.Id.Should().BeGreaterThan(0);
        recipe.Name.Should().Be("Imported");
        recipe.RatedSpeed.Should().Be(2500);
        recipe.TrialMass1.Should().Be(40);
    }

    [Fact]
    public async Task ExportToXmlAsync_ReturnsValidXml()
    {
        var created = await _service.CreateAsync(new Recipe { Name = "XmlTest", RatedSpeed = 1000 });

        var xml = await _service.ExportToXmlAsync(created.Id);

        xml.Should().Contain("<Name>XmlTest</Name>");
        xml.Should().Contain("<RatedSpeed>1000</RatedSpeed>");
    }

    [Fact]
    public async Task ImportFromXmlAsync_ValidXml_CreatesRecipe()
    {
        var xml = """<?xml version="1.0" encoding="utf-8"?><Recipe><Name>XmlImport</Name><RatedSpeed>3000</RatedSpeed><AllowUnbalanceLeft>0.5</AllowUnbalanceLeft><AllowUnbalanceRight>0.5</AllowUnbalanceRight><TrialMass1>60</TrialMass1><TrialAngle1>270</TrialAngle1><TrialMass2>60</TrialMass2><TrialAngle2>90</TrialAngle2><CalibrationFactorLeft>1.0</CalibrationFactorLeft><CalibrationFactorRight>1.0</CalibrationFactorRight></Recipe>""";

        var recipe = await _service.ImportFromXmlAsync(xml);

        recipe.Name.Should().Be("XmlImport");
        recipe.RatedSpeed.Should().Be(3000);
        recipe.TrialMass1.Should().Be(60);
    }

    [Fact]
    public async Task ExportAllToJsonAsync_ReturnsJsonArray()
    {
        await _service.CreateAsync(new Recipe { Name = "R1" });
        await _service.CreateAsync(new Recipe { Name = "R2" });

        var json = await _service.ExportAllToJsonAsync();

        json.Should().Contain("R1");
        json.Should().Contain("R2");
        json.TrimStart().Should().StartWith("[");
    }

    [Fact]
    public async Task ImportAllFromJsonAsync_ArrayJson_ImportsAll()
    {
        var json = """[{"name":"Bulk1","ratedSpeed":1000,"trialMass1":10,"trialAngle1":0,"trialMass2":10,"trialAngle2":180,"allowUnbalanceLeft":0.5,"allowUnbalanceRight":0.5,"calibrationFactorLeft":1.0,"calibrationFactorRight":1.0},{"name":"Bulk2","ratedSpeed":2000,"trialMass1":20,"trialAngle1":90,"trialMass2":20,"trialAngle2":270,"allowUnbalanceLeft":0.8,"allowUnbalanceRight":0.8,"calibrationFactorLeft":1.0,"calibrationFactorRight":1.0}]""";

        var recipes = await _service.ImportAllFromJsonAsync(json);

        recipes.Should().HaveCount(2);
        recipes[0].Name.Should().Be("Bulk1");
        recipes[1].Name.Should().Be("Bulk2");
    }
}
