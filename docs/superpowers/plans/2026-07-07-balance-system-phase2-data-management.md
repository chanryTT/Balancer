# Phase 2: Data Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build recipe management CRUD with JSON/XML import/export, history records with query and comparison, QuestPDF test report generation, and integration of recipe parameters into the balancing test flow.

**Architecture:** Three new service interfaces in `Core/Services/` with FreeSql-backed implementations in `Infrastructure/Database/`, following the same "interface in Core, implementation in Infrastructure" pattern as `IDataAcquisitionService`. WPF Views and ViewModels use CommunityToolkit.Mvvm source generators. SQLite in-memory database for service-level tests. QuestPDF added to Infrastructure for PDF report generation.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm 8.4.2, FreeSql 3.5.310 + FreeSql.Provider.Sqlite (tests), QuestPDF, xUnit 2.9.3, FluentAssertions 8.10.0

## Global Constraints

- .NET 10, WPF, CommunityToolkit.Mvvm 8.4.2 with source-generated `[ObservableProperty]` and `[RelayCommand]`
- FreeSql 3.5.310 + SQL Server for production, FreeSql.Provider.Sqlite for tests
- MVVM pattern: ViewModels in `App/ViewModels/`, Views in `App/Views/`
- 4-layer onion: Shared → Core → Infrastructure → App
- All user-facing text in Chinese (中文界面)
- Service interfaces in `Core/Services/`, implementations in `Infrastructure/Database/`
- Use `ILogger<T>` for all service logging via Serilog
- DI: all services and ViewModels registered as Singleton in `ServiceCollectionExtensions.cs`
- Database tables synced via `FreeSql.CodeFirst.SyncStructure<T>()` in `DatabaseInitializer.Initialize()`
- TDD: red → green → commit for all service-level code
- JSON export/import via `System.Text.Json`, XML via `System.Xml.Linq`
- No new external dependencies beyond QuestPDF and FreeSql.Provider.Sqlite (test only)
- Follow existing naming: PascalCase properties, Chinese display strings, `Async` suffix on async methods
- Allow unsafe blocks already enabled in App.csproj — no additional unsafe code needed in Phase 2

---

### Task 1: Recipe + TestRecord models and database migration

**Files:**
- Create: `src/BalanceSystem.Core/Models/Recipe.cs`
- Create: `src/BalanceSystem.Core/Models/TestRecord.cs`
- Modify: `src/BalanceSystem.Infrastructure/Database/DatabaseInitializer.cs`

**Interfaces:**
- Consumes: `FreeSql.DataAnnotations.[Table]`, `[Column]` attributes (already in Core via FreeSql package)
- Produces: `Recipe` entity (Id, Name, RatedSpeed, AllowUnbalanceLeft, AllowUnbalanceRight, TrialMass1, TrialAngle1, TrialMass2, TrialAngle2, CalibrationFactorLeft, CalibrationFactorRight, CreateTime, UpdatedTime), `TestRecord` entity (Id, RecipeId, UserId, TestTime, Speed, Initial* amplitudes/phases, LeftTrial* amplitudes/phases, RightTrial* amplitudes/phases, Retest* amplitudes/phases, LeftCorrectionMass, LeftCorrectionAngle, RightCorrectionMass, RightCorrectionAngle, ResidualLeft, ResidualRight, IsPassed, Notes)

- [ ] **Step 1: Create Recipe entity**

```csharp
// src/BalanceSystem.Core/Models/Recipe.cs
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
```

- [ ] **Step 2: Create TestRecord entity**

```csharp
// src/BalanceSystem.Core/Models/TestRecord.cs
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
```

- [ ] **Step 3: Update DatabaseInitializer to sync new tables**

```csharp
// src/BalanceSystem.Infrastructure/Database/DatabaseInitializer.cs
// Replace the Initialize() method body — add SyncStructure calls for Recipe and TestRecord:

public void Initialize()
{
    _logger.LogInformation("Initializing database...");

    _db.Orm.CodeFirst.SyncStructure<User>();
    _db.Orm.CodeFirst.SyncStructure<Recipe>();
    _db.Orm.CodeFirst.SyncStructure<TestRecord>();

    if (!_db.Orm.Select<User>().Any())
    {
        _db.Orm.Insert(new User
        {
            Username = "admin",
            PasswordHash = HashPassword("admin123"),
            Role = "Admin",
            IsActive = true
        }).ExecuteAffrows();
        _logger.LogInformation("Seeded default admin user");
    }

    // Seed a sample recipe for testing
    if (!_db.Orm.Select<Recipe>().Any())
    {
        _db.Orm.Insert(new Recipe
        {
            Name = "演示转子",
            RatedSpeed = 1500,
            AllowUnbalanceLeft = 1.0,
            AllowUnbalanceRight = 1.0,
            TrialMass1 = 50,
            TrialAngle1 = 0,
            TrialMass2 = 50,
            TrialAngle2 = 0
        }).ExecuteAffrows();
        _logger.LogInformation("Seeded default demo recipe");
    }

    _logger.LogInformation("Database initialization complete");
}
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build src/BalanceSystem.Core/BalanceSystem.Core.csproj && dotnet build src/BalanceSystem.Infrastructure/BalanceSystem.Infrastructure.csproj`
Expected: 0 errors, 0 warnings

- [ ] **Step 5: Commit**

```bash
git add src/BalanceSystem.Core/Models/Recipe.cs src/BalanceSystem.Core/Models/TestRecord.cs src/BalanceSystem.Infrastructure/Database/DatabaseInitializer.cs
git commit -m "feat: add Recipe and TestRecord entities with database migration"
```

---

### Task 2: IRecipeService + RecipeService with CRUD and import/export + tests

**Files:**
- Create: `src/BalanceSystem.Core/Services/IRecipeService.cs`
- Create: `src/BalanceSystem.Infrastructure/Database/RecipeService.cs`
- Create: `tests/BalanceSystem.Core.Tests/Services/RecipeServiceTests.cs`
- Modify: `tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj`

**Interfaces:**
- Consumes: `Recipe` model (Task 1), `AppDbContext` (existing), `ILogger<T>` pattern from `CsvSimulationService`
- Produces: `IRecipeService` interface with `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `SearchAsync`, `ExportToJsonAsync`, `ExportToXmlAsync`, `ImportFromJsonAsync`, `ImportFromXmlAsync`, `ExportAllToJsonAsync`, `ExportAllToXmlAsync`, `ImportAllFromJsonAsync`, `ImportAllFromXmlAsync`

- [ ] **Step 1: Add FreeSql.Provider.Sqlite to test project**

Edit `tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj` — add inside `<ItemGroup>`:

```xml
<PackageReference Include="FreeSql.Provider.Sqlite" Version="3.5.310" />
```

- [ ] **Step 2: Write the IRecipeService interface**

```csharp
// src/BalanceSystem.Core/Services/IRecipeService.cs
using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Services;

public interface IRecipeService
{
    Task<List<Recipe>> GetAllAsync();
    Task<Recipe?> GetByIdAsync(int id);
    Task<Recipe> CreateAsync(Recipe recipe);
    Task<bool> UpdateAsync(Recipe recipe);
    Task<bool> DeleteAsync(int id);

    /// <summary>根据关键字搜索配方（匹配名称或转速）</summary>
    Task<List<Recipe>> SearchAsync(string keyword);

    /// <summary>导出单个配方为JSON字符串</summary>
    Task<string> ExportToJsonAsync(int id);

    /// <summary>导出单个配方为XML字符串</summary>
    Task<string> ExportToXmlAsync(int id);

    /// <summary>从JSON字符串导入单个配方</summary>
    Task<Recipe> ImportFromJsonAsync(string json);

    /// <summary>从XML字符串导入单个配方</summary>
    Task<Recipe> ImportFromXmlAsync(string xml);

    /// <summary>导出所有配方为JSON数组字符串</summary>
    Task<string> ExportAllToJsonAsync();

    /// <summary>导出所有配方为XML字符串</summary>
    Task<string> ExportAllToXmlAsync();

    /// <summary>从JSON数组字符串批量导入配方</summary>
    Task<List<Recipe>> ImportAllFromJsonAsync(string json);

    /// <summary>从XML字符串批量导入配方</summary>
    Task<List<Recipe>> ImportAllFromXmlAsync(string xml);
}
```

- [ ] **Step 3: Write failing test — RecipeServiceTests**

```csharp
// tests/BalanceSystem.Core.Tests/Services/RecipeServiceTests.cs
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
```

- [ ] **Step 4: Run tests — verify they fail**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj --filter "FullyQualifiedName~RecipeServiceTests"`
Expected: All 16 tests FAIL (RecipeService not implemented)

- [ ] **Step 5: Implement RecipeService**

```csharp
// src/BalanceSystem.Infrastructure/Database/RecipeService.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.Database;

public class RecipeService : IRecipeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RecipeService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public RecipeService(AppDbContext db, ILogger<RecipeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<List<Recipe>> GetAllAsync()
    {
        return _db.Orm.Select<Recipe>()
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public Task<Recipe?> GetByIdAsync(int id)
    {
        return _db.Orm.Select<Recipe>()
            .Where(r => r.Id == id)
            .FirstAsync();
    }

    public async Task<Recipe> CreateAsync(Recipe recipe)
    {
        recipe.CreateTime = DateTime.Now;
        recipe.UpdatedTime = null;
        var inserted = await _db.Orm.Insert(recipe).ExecuteInsertedAsync();
        var result = inserted.First();
        _logger.LogInformation("Recipe created: {Name} (Id={Id})", result.Name, result.Id);
        return result;
    }

    public async Task<bool> UpdateAsync(Recipe recipe)
    {
        var existing = await _db.Orm.Select<Recipe>().Where(r => r.Id == recipe.Id).FirstAsync();
        if (existing is null)
        {
            _logger.LogWarning("Update failed: Recipe Id={Id} not found", recipe.Id);
            return false;
        }

        recipe.UpdatedTime = DateTime.Now;
        var affected = await _db.Orm.Update<Recipe>()
            .SetSource(recipe)
            .ExecuteAffrowsAsync();
        _logger.LogInformation("Recipe updated: {Name} (Id={Id})", recipe.Name, recipe.Id);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affected = await _db.Orm.Delete<Recipe>().Where(r => r.Id == id).ExecuteAffrowsAsync();
        if (affected > 0)
            _logger.LogInformation("Recipe deleted: Id={Id}", id);
        return affected > 0;
    }

    public Task<List<Recipe>> SearchAsync(string keyword)
    {
        return _db.Orm.Select<Recipe>()
            .Where(r => r.Name.Contains(keyword) || r.RatedSpeed.ToString().Contains(keyword))
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<string> ExportToJsonAsync(int id)
    {
        var recipe = await GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Recipe Id={id} not found");
        return JsonSerializer.Serialize(recipe, JsonOptions);
    }

    public async Task<string> ExportToXmlAsync(int id)
    {
        var recipe = await GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Recipe Id={id} not found");

        return new XDocument(
            new XElement("Recipe",
                new XElement("Name", recipe.Name),
                new XElement("RatedSpeed", recipe.RatedSpeed),
                new XElement("AllowUnbalanceLeft", recipe.AllowUnbalanceLeft),
                new XElement("AllowUnbalanceRight", recipe.AllowUnbalanceRight),
                new XElement("TrialMass1", recipe.TrialMass1),
                new XElement("TrialAngle1", recipe.TrialAngle1),
                new XElement("TrialMass2", recipe.TrialMass2),
                new XElement("TrialAngle2", recipe.TrialAngle2),
                new XElement("CalibrationFactorLeft", recipe.CalibrationFactorLeft),
                new XElement("CalibrationFactorRight", recipe.CalibrationFactorRight)
            )
        ).Declaration!.ToString() + Environment.NewLine + new XDocument(
            new XElement("Recipe",
                new XElement("Name", recipe.Name),
                new XElement("RatedSpeed", recipe.RatedSpeed),
                new XElement("AllowUnbalanceLeft", recipe.AllowUnbalanceLeft),
                new XElement("AllowUnbalanceRight", recipe.AllowUnbalanceRight),
                new XElement("TrialMass1", recipe.TrialMass1),
                new XElement("TrialAngle1", recipe.TrialAngle1),
                new XElement("TrialMass2", recipe.TrialMass2),
                new XElement("TrialAngle2", recipe.TrialAngle2),
                new XElement("CalibrationFactorLeft", recipe.CalibrationFactorLeft),
                new XElement("CalibrationFactorRight", recipe.CalibrationFactorRight)
            )
        ).ToString();
    }

    public async Task<Recipe> ImportFromJsonAsync(string json)
    {
        var imported = JsonSerializer.Deserialize<Recipe>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse JSON");
        imported.Id = 0; // Force new record
        return await CreateAsync(imported);
    }

    public async Task<Recipe> ImportFromXmlAsync(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid XML: missing root element");

        var recipe = new Recipe
        {
            Name = root.Element("Name")?.Value ?? "",
            RatedSpeed = int.Parse(root.Element("RatedSpeed")?.Value ?? "1500"),
            AllowUnbalanceLeft = double.Parse(root.Element("AllowUnbalanceLeft")?.Value ?? "0"),
            AllowUnbalanceRight = double.Parse(root.Element("AllowUnbalanceRight")?.Value ?? "0"),
            TrialMass1 = double.Parse(root.Element("TrialMass1")?.Value ?? "0"),
            TrialAngle1 = double.Parse(root.Element("TrialAngle1")?.Value ?? "0"),
            TrialMass2 = double.Parse(root.Element("TrialMass2")?.Value ?? "0"),
            TrialAngle2 = double.Parse(root.Element("TrialAngle2")?.Value ?? "0"),
            CalibrationFactorLeft = double.Parse(root.Element("CalibrationFactorLeft")?.Value ?? "1.0"),
            CalibrationFactorRight = double.Parse(root.Element("CalibrationFactorRight")?.Value ?? "1.0")
        };

        return await CreateAsync(recipe);
    }

    public async Task<string> ExportAllToJsonAsync()
    {
        var all = await GetAllAsync();
        return JsonSerializer.Serialize(all, JsonOptions);
    }

    public async Task<string> ExportAllToXmlAsync()
    {
        var all = await GetAllAsync();
        var doc = new XDocument(
            new XElement("Recipes",
                all.Select(r =>
                    new XElement("Recipe",
                        new XElement("Name", r.Name),
                        new XElement("RatedSpeed", r.RatedSpeed),
                        new XElement("AllowUnbalanceLeft", r.AllowUnbalanceLeft),
                        new XElement("AllowUnbalanceRight", r.AllowUnbalanceRight),
                        new XElement("TrialMass1", r.TrialMass1),
                        new XElement("TrialAngle1", r.TrialAngle1),
                        new XElement("TrialMass2", r.TrialMass2),
                        new XElement("TrialAngle2", r.TrialAngle2),
                        new XElement("CalibrationFactorLeft", r.CalibrationFactorLeft),
                        new XElement("CalibrationFactorRight", r.CalibrationFactorRight)
                    )
                )
            )
        );
        return doc.Declaration!.ToString() + Environment.NewLine + doc;
    }

    public async Task<List<Recipe>> ImportAllFromJsonAsync(string json)
    {
        var list = JsonSerializer.Deserialize<List<Recipe>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse JSON array");
        var results = new List<Recipe>();
        foreach (var r in list)
        {
            r.Id = 0;
            results.Add(await CreateAsync(r));
        }
        return results;
    }

    public async Task<List<Recipe>> ImportAllFromXmlAsync(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid XML: missing root element");

        var results = new List<Recipe>();
        foreach (var el in root.Elements("Recipe"))
        {
            var recipe = new Recipe
            {
                Name = el.Element("Name")?.Value ?? "",
                RatedSpeed = int.Parse(el.Element("RatedSpeed")?.Value ?? "1500"),
                AllowUnbalanceLeft = double.Parse(el.Element("AllowUnbalanceLeft")?.Value ?? "0"),
                AllowUnbalanceRight = double.Parse(el.Element("AllowUnbalanceRight")?.Value ?? "0"),
                TrialMass1 = double.Parse(el.Element("TrialMass1")?.Value ?? "0"),
                TrialAngle1 = double.Parse(el.Element("TrialAngle1")?.Value ?? "0"),
                TrialMass2 = double.Parse(el.Element("TrialMass2")?.Value ?? "0"),
                TrialAngle2 = double.Parse(el.Element("TrialAngle2")?.Value ?? "0"),
                CalibrationFactorLeft = double.Parse(el.Element("CalibrationFactorLeft")?.Value ?? "1.0"),
                CalibrationFactorRight = double.Parse(el.Element("CalibrationFactorRight")?.Value ?? "1.0")
            };
            results.Add(await CreateAsync(recipe));
        }
        return results;
    }
}
```

**IMPORTANT NOTE for implementer:** The `ExportToXmlAsync` method has a known issue — the XML declaration concatenation with Environment.NewLine + new XDocument produces an invalid double-declaration. The correct approach is to return `doc.ToString()` for export and then re-parse with `XDocument.Parse`. The `ExportAllToXmlAsync` is correct. The implementer should fix `ExportToXmlAsync` during implementation to use the same pattern.

- [ ] **Step 6: Run tests — verify they pass**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj --filter "FullyQualifiedName~RecipeServiceTests"`
Expected: All 16 tests PASS

- [ ] **Step 7: Run all existing tests to check for regressions**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj`
Expected: All 23 tests PASS (7 existing + 16 new)

- [ ] **Step 8: Commit**

```bash
git add src/BalanceSystem.Core/Services/IRecipeService.cs src/BalanceSystem.Infrastructure/Database/RecipeService.cs tests/BalanceSystem.Core.Tests/Services/RecipeServiceTests.cs tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj
git commit -m "feat: add IRecipeService and RecipeService with CRUD and JSON/XML import-export"
```

---

### Task 3: ITestRecordService + TestRecordService + tests

**Files:**
- Create: `src/BalanceSystem.Core/Services/ITestRecordService.cs`
- Create: `src/BalanceSystem.Infrastructure/Database/TestRecordService.cs`
- Create: `tests/BalanceSystem.Core.Tests/Services/TestRecordServiceTests.cs`

**Interfaces:**
- Consumes: `TestRecord` model (Task 1), `AppDbContext`, `ILogger<T>`
- Produces: `ITestRecordService` interface with `CreateAsync`, `GetByIdAsync`, `QueryAsync`, `CountAsync`, `GetByRecipeIdAsync`

- [ ] **Step 1: Write the ITestRecordService interface**

```csharp
// src/BalanceSystem.Core/Services/ITestRecordService.cs
using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Services;

public interface ITestRecordService
{
    /// <summary>保存一条测试记录</summary>
    Task<TestRecord> CreateAsync(TestRecord record);

    /// <summary>根据ID获取测试记录详情</summary>
    Task<TestRecord?> GetByIdAsync(int id);

    /// <summary>
    /// 分页查询测试记录。
    /// 支持按时间范围、配方ID、是否合格筛选。
    /// </summary>
    Task<List<TestRecord>> QueryAsync(
        DateTime? from = null,
        DateTime? to = null,
        int? recipeId = null,
        bool? isPassed = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>查询符合条件的记录总数（用于分页控件）</summary>
    Task<long> CountAsync(
        DateTime? from = null,
        DateTime? to = null,
        int? recipeId = null,
        bool? isPassed = null);

    /// <summary>获取同一配方最近的N条记录（用于对比）</summary>
    Task<List<TestRecord>> GetByRecipeIdAsync(int recipeId, int limit = 10);
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// tests/BalanceSystem.Core.Tests/Services/TestRecordServiceTests.cs
using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using BalanceSystem.Infrastructure.Database;
using FluentAssertions;
using FreeSql;
using Microsoft.Extensions.Logging.Abstractions;

namespace BalanceSystem.Core.Tests.Services;

public class TestRecordServiceTests : IDisposable
{
    private readonly IFreeSql _db;
    private readonly TestRecordService _service;

    public TestRecordServiceTests()
    {
        _db = new FreeSqlBuilder()
            .UseConnectionString(DataType.Sqlite, "Data Source=:memory:")
            .UseAutoSyncStructure(true)
            .Build();
        _db.CodeFirst.SyncStructure<TestRecord>();

        var dbContext = new AppDbContext("Data Source=:memory:");
        typeof(AppDbContext).GetProperty("Orm")!.SetValue(dbContext, _db);

        _service = new TestRecordService(dbContext, NullLogger<TestRecordService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_ValidRecord_StoresAndReturnsWithId()
    {
        var record = new TestRecord
        {
            RecipeId = 1,
            UserId = 1,
            Speed = 1500,
            InitialLeftAmplitude = 2.5,
            InitialLeftPhase = 30,
            InitialRightAmplitude = 1.8,
            InitialRightPhase = 150,
            LeftCorrectionMass = 12.5,
            LeftCorrectionAngle = 45,
            RightCorrectionMass = 8.3,
            RightCorrectionAngle = 225,
            ResidualLeft = 0.15,
            ResidualRight = 0.12,
            IsPassed = true
        };

        var result = await _service.CreateAsync(record);

        result.Id.Should().BeGreaterThan(0);
        result.TestTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRecord_ReturnsCompleteRecord()
    {
        var created = await _service.CreateAsync(new TestRecord
        {
            RecipeId = 2,
            Speed = 2000,
            LeftCorrectionMass = 5.0,
            LeftCorrectionAngle = 90,
            RightCorrectionMass = 5.0,
            RightCorrectionAngle = 270,
            IsPassed = false
        });

        var found = await _service.GetByIdAsync(created.Id);

        found.Should().NotBeNull();
        found!.RecipeId.Should().Be(2);
        found.IsPassed.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var found = await _service.GetByIdAsync(99999);
        found.Should().BeNull();
    }

    [Fact]
    public async Task QueryAsync_ByTimeRange_FiltersCorrectly()
    {
        var old = await _service.CreateAsync(new TestRecord
        {
            RecipeId = 1, Speed = 1000, TestTime = DateTime.Now.AddDays(-30),
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = true
        });
        var recent = await _service.CreateAsync(new TestRecord
        {
            RecipeId = 1, Speed = 1000, TestTime = DateTime.Now,
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = true
        });

        var results = await _service.QueryAsync(from: DateTime.Now.AddDays(-7), to: DateTime.Now.AddDays(1));

        results.Should().ContainSingle(r => r.Id == recent.Id);
        results.Should().NotContain(r => r.Id == old.Id);
    }

    [Fact]
    public async Task QueryAsync_ByIsPassed_FiltersCorrectly()
    {
        await _service.CreateAsync(new TestRecord
        {
            RecipeId = 1, Speed = 1000,
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = true
        });
        await _service.CreateAsync(new TestRecord
        {
            RecipeId = 1, Speed = 1000,
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = false
        });

        var passed = await _service.QueryAsync(isPassed: true);
        var failed = await _service.QueryAsync(isPassed: false);

        passed.Should().AllSatisfy(r => r.IsPassed.Should().BeTrue());
        failed.Should().AllSatisfy(r => r.IsPassed.Should().BeFalse());
    }

    [Fact]
    public async Task QueryAsync_Pagination_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 5; i++)
            await _service.CreateAsync(new TestRecord
            {
                RecipeId = i, Speed = 1000 * i,
                LeftCorrectionMass = i, LeftCorrectionAngle = 0,
                RightCorrectionMass = i, RightCorrectionAngle = 180, IsPassed = true
            });

        var page1 = await _service.QueryAsync(page: 1, pageSize: 2);
        var page2 = await _service.QueryAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
        page1.Intersect(page2).Should().BeEmpty();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectTotal()
    {
        for (int i = 0; i < 3; i++)
            await _service.CreateAsync(new TestRecord
            {
                RecipeId = 1, Speed = 1000,
                LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
                RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = true
            });

        var count = await _service.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_WithFilter_ReturnsFilteredCount()
    {
        await _service.CreateAsync(new TestRecord
        {
            RecipeId = 1, Speed = 1000,
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = true
        });
        await _service.CreateAsync(new TestRecord
        {
            RecipeId = 2, Speed = 1000,
            LeftCorrectionMass = 10, LeftCorrectionAngle = 0,
            RightCorrectionMass = 10, RightCorrectionAngle = 180, IsPassed = false
        });

        var count = await _service.CountAsync(isPassed: true);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetByRecipeIdAsync_ReturnsLatestNRecords()
    {
        for (int i = 1; i <= 5; i++)
            await _service.CreateAsync(new TestRecord
            {
                RecipeId = 42, Speed = 1000,
                TestTime = DateTime.Now.AddMinutes(-i),
                LeftCorrectionMass = i, LeftCorrectionAngle = 0,
                RightCorrectionMass = i, RightCorrectionAngle = 180, IsPassed = true
            });

        var results = await _service.GetByRecipeIdAsync(42, limit: 3);

        results.Should().HaveCount(3);
        results.Should().BeInDescendingOrder(r => r.TestTime);
    }
}
```

- [ ] **Step 3: Run tests — verify they fail**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj --filter "FullyQualifiedName~TestRecordServiceTests"`
Expected: All 9 tests FAIL

- [ ] **Step 4: Implement TestRecordService**

```csharp
// src/BalanceSystem.Infrastructure/Database/TestRecordService.cs
using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.Database;

public class TestRecordService : ITestRecordService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TestRecordService> _logger;

    public TestRecordService(AppDbContext db, ILogger<TestRecordService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TestRecord> CreateAsync(TestRecord record)
    {
        record.TestTime = record.TestTime == default ? DateTime.Now : record.TestTime;
        var inserted = await _db.Orm.Insert(record).ExecuteInsertedAsync();
        var result = inserted.First();
        _logger.LogInformation("Test record created: Id={Id}, RecipeId={RecipeId}, Passed={IsPassed}",
            result.Id, result.RecipeId, result.IsPassed);
        return result;
    }

    public Task<TestRecord?> GetByIdAsync(int id)
    {
        return _db.Orm.Select<TestRecord>()
            .Where(r => r.Id == id)
            .FirstAsync();
    }

    public async Task<List<TestRecord>> QueryAsync(
        DateTime? from = null,
        DateTime? to = null,
        int? recipeId = null,
        bool? isPassed = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.Orm.Select<TestRecord>();

        if (from.HasValue)
            query = query.Where(r => r.TestTime >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.TestTime <= to.Value);
        if (recipeId.HasValue)
            query = query.Where(r => r.RecipeId == recipeId.Value);
        if (isPassed.HasValue)
            query = query.Where(r => r.IsPassed == isPassed.Value);

        return await query
            .OrderByDescending(r => r.TestTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<long> CountAsync(
        DateTime? from = null,
        DateTime? to = null,
        int? recipeId = null,
        bool? isPassed = null)
    {
        var query = _db.Orm.Select<TestRecord>();

        if (from.HasValue)
            query = query.Where(r => r.TestTime >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.TestTime <= to.Value);
        if (recipeId.HasValue)
            query = query.Where(r => r.RecipeId == recipeId.Value);
        if (isPassed.HasValue)
            query = query.Where(r => r.IsPassed == isPassed.Value);

        return await query.CountAsync();
    }

    public Task<List<TestRecord>> GetByRecipeIdAsync(int recipeId, int limit = 10)
    {
        return _db.Orm.Select<TestRecord>()
            .Where(r => r.RecipeId == recipeId)
            .OrderByDescending(r => r.TestTime)
            .Take(limit)
            .ToListAsync();
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj --filter "FullyQualifiedName~TestRecordServiceTests"`
Expected: All 9 tests PASS

- [ ] **Step 6: Run full test suite**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj`
Expected: All 32 tests PASS (7 FFT/solver + 16 recipe + 9 test record)

- [ ] **Step 7: Commit**

```bash
git add src/BalanceSystem.Core/Services/ITestRecordService.cs src/BalanceSystem.Infrastructure/Database/TestRecordService.cs tests/BalanceSystem.Core.Tests/Services/TestRecordServiceTests.cs
git commit -m "feat: add ITestRecordService and TestRecordService with query, pagination, and filtering"
```

---

### Task 4: QuestPDF test report generation + tests

**Files:**
- Create: `src/BalanceSystem.Infrastructure/Reporting/TestReportService.cs`
- Create: `tests/BalanceSystem.Core.Tests/Services/TestReportServiceTests.cs`
- Modify: `src/BalanceSystem.Infrastructure/BalanceSystem.Infrastructure.csproj`

**Interfaces:**
- Consumes: `TestRecord` model (Task 1), `Recipe` model (Task 1), `IRecipeService` (Task 2)
- Produces: `TestReportService` with `GenerateReportAsync(TestRecord, Recipe?, string outputPath)` → returns file path

- [ ] **Step 1: Add QuestPDF NuGet package to Infrastructure**

Edit `src/BalanceSystem.Infrastructure/BalanceSystem.Infrastructure.csproj` — add inside `<ItemGroup>`:

```xml
<PackageReference Include="QuestPDF" Version="2025.8.4" />
```

- [ ] **Step 2: Write failing tests**

```csharp
// tests/BalanceSystem.Core.Tests/Services/TestReportServiceTests.cs
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
```

- [ ] **Step 3: Run tests — verify they fail**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj --filter "FullyQualifiedName~TestReportServiceTests"`
Expected: All 3 tests FAIL (TestReportService not implemented)

- [ ] **Step 4: Implement TestReportService**

```csharp
// src/BalanceSystem.Infrastructure/Reporting/TestReportService.cs
using BalanceSystem.Core.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BalanceSystem.Infrastructure.Reporting;

public class TestReportService
{
    private readonly ILogger<TestReportService> _logger;

    static TestReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public TestReportService(ILogger<TestReportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 生成PDF测试报告并保存到指定路径。
    /// </summary>
    /// <param name="record">测试记录</param>
    /// <param name="recipe">关联配方（可选）</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>生成的PDF文件路径</returns>
    public Task<string> GenerateReportAsync(TestRecord record, Recipe? recipe, string outputPath)
    {
        return Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("SimHei"));

                    // ── Header ──
                    page.Header().Element(c =>
                    {
                        c.Column(col =>
                        {
                            col.Item().AlignCenter().Text("动平衡测试报告")
                                .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().AlignCenter().Text($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        });
                    });

                    // ── Content ──
                    page.Content().Element(c =>
                    {
                        c.Column(col =>
                        {
                            col.Spacing(8);

                            // Section 1: Basic info
                            col.Item().Text("一、基本信息").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                    });
                                    void Row(string label, string value, string label2, string value2)
                                    {
                                        table.Cell().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1);
                                        table.Cell().Text(value).FontSize(10).Bold();
                                        table.Cell().Text(label2).FontSize(10).FontColor(Colors.Grey.Darken1);
                                        table.Cell().Text(value2).FontSize(10).Bold();
                                    }
                                    Row("配方名称:", recipe?.Name ?? "未知", "测试时间:", record.TestTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                    Row("额定转速:", $"{record.Speed} RPM", "判定结果:", record.IsPassed ? "合格 ✓" : "不合格 ✗");
                                });
                            });

                            col.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Section 2: Initial vibration
                            col.Item().Text("二、初始振动数据").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                    });
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("").FontSize(10);
                                        header.Cell().Text("幅值 (μm)").FontSize(10).Bold();
                                        header.Cell().Text("相位 (°)").FontSize(10).Bold();
                                        header.Cell().Text("").FontSize(10);
                                    });
                                    void PlaneRow(string plane, double amp, double phase)
                                    {
                                        table.Cell().Text(plane).FontSize(11).Bold();
                                        table.Cell().Text($"{amp:F2}").FontSize(11);
                                        table.Cell().Text($"{phase:F1}°").FontSize(11);
                                        table.Cell().Text("").FontSize(11);
                                    }
                                    PlaneRow("左面:", record.InitialLeftAmplitude, record.InitialLeftPhase);
                                    PlaneRow("右面:", record.InitialRightAmplitude, record.InitialRightPhase);
                                });
                            });

                            col.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Section 3: Correction result
                            col.Item().Text("三、配重结果").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                    });
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("平面").FontSize(10).Bold();
                                        header.Cell().Text("配重质量 (g)").FontSize(10).Bold();
                                        header.Cell().Text("配重角度 (°)").FontSize(10).Bold();
                                        header.Cell().Text("剩余不平衡量 (μm)").FontSize(10).Bold();
                                    });
                                    table.Cell().Text("左面").FontSize(11).Bold();
                                    table.Cell().Text($"{record.LeftCorrectionMass:F2}").FontSize(11);
                                    table.Cell().Text($"{record.LeftCorrectionAngle:F1}°").FontSize(11);
                                    table.Cell().Text($"{record.ResidualLeft:F2}").FontSize(11);

                                    table.Cell().Text("右面").FontSize(11).Bold();
                                    table.Cell().Text($"{record.RightCorrectionMass:F2}").FontSize(11);
                                    table.Cell().Text($"{record.RightCorrectionAngle:F1}°").FontSize(11);
                                    table.Cell().Text($"{record.ResidualRight:F2}").FontSize(11);
                                });
                            });

                            // Section 4: Verdict
                            col.Item().PaddingTop(12);
                            col.Item().Element(container =>
                            {
                                var (verdictText, verdictColor) = record.IsPassed
                                    ? ("判定: 合格", Colors.Green.Darken2)
                                    : ("判定: 不合格", Colors.Red.Darken2);
                                container.Background(record.IsPassed ? Colors.Green.Lighten4 : Colors.Red.Lighten4)
                                    .Padding(12)
                                    .AlignCenter()
                                    .Text(verdictText)
                                    .FontSize(18).Bold().FontColor(verdictColor);
                            });

                            if (record.Notes is not null)
                            {
                                col.Item().PaddingTop(8).Text($"备注: {record.Notes}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });

                    // ── Footer ──
                    page.Footer().Element(c =>
                    {
                        c.AlignCenter().Text(text =>
                        {
                            text.Span("BalanceSystem — 动平衡测试系统").FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span("    ");
                            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span(" / ");
                            text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            }).GeneratePdf(outputPath);

            _logger.LogInformation("Test report generated: {Path}", outputPath);
            return outputPath;
        });
    }
}
```

**IMPORTANT NOTE for implementer:** QuestPDF Community license requires registration. The `Settings.License = LicenseType.Community` call handles this. The font `SimHei` (黑体) is referenced for Chinese text rendering — on Windows this is available by default. If it's not found, QuestPDF will fall back to a default font and Chinese characters may render as boxes. The implementer may need to register a fallback font. Also, the `Task.Run` wrapping is acceptable here since PDF generation is a CPU-bound operation that should not block the UI thread.

- [ ] **Step 5: Run tests — verify they pass**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj --filter "FullyQualifiedName~TestReportServiceTests"`
Expected: All 3 tests PASS

- [ ] **Step 6: Run full test suite**

Run: `dotnet test tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj`
Expected: All 35 tests PASS

- [ ] **Step 7: Commit**

```bash
git add src/BalanceSystem.Infrastructure/Reporting/TestReportService.cs tests/BalanceSystem.Core.Tests/Services/TestReportServiceTests.cs src/BalanceSystem.Infrastructure/BalanceSystem.Infrastructure.csproj
git commit -m "feat: add QuestPDF test report generation service"
```

---

### Task 5: RecipeManagementViewModel

**Files:**
- Create: `src/BalanceSystem.App/ViewModels/RecipeManagementViewModel.cs`

**Interfaces:**
- Consumes: `IRecipeService` (Task 2), `Recipe` model (Task 1)
- Produces: `RecipeManagementViewModel` with observable collections for recipe list, selected recipe, edit form fields, and CRUD + import/export relay commands

- [ ] **Step 1: Implement RecipeManagementViewModel**

```csharp
// src/BalanceSystem.App/ViewModels/RecipeManagementViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BalanceSystem.App.ViewModels;

public partial class RecipeManagementViewModel : ObservableObject
{
    private readonly IRecipeService _recipeService;

    [ObservableProperty] private ObservableCollection<Recipe> _recipes = [];
    [ObservableProperty] private Recipe? _selectedRecipe;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = "就绪";
    [ObservableProperty] private bool _isEditing;

    // ── Edit form fields ──
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private int _editRatedSpeed = 1500;
    [ObservableProperty] private double _editAllowUnbalanceLeft;
    [ObservableProperty] private double _editAllowUnbalanceRight;
    [ObservableProperty] private double _editTrialMass1 = 50;
    [ObservableProperty] private double _editTrialAngle1;
    [ObservableProperty] private double _editTrialMass2 = 50;
    [ObservableProperty] private double _editTrialAngle2;
    [ObservableProperty] private double _editCalibrationFactorLeft = 1.0;
    [ObservableProperty] private double _editCalibrationFactorRight = 1.0;

    public int[] SpeedOptions => Shared.Constants.SpeedOptions;

    public RecipeManagementViewModel(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [RelayCommand]
    private async Task LoadRecipes()
    {
        try
        {
            var list = await _recipeService.GetAllAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Recipes = new ObservableCollection<Recipe>(list);
                StatusText = $"已加载 {list.Count} 条配方";
            });
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadRecipes();
            return;
        }
        try
        {
            var list = await _recipeService.SearchAsync(SearchText);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Recipes = new ObservableCollection<Recipe>(list);
                StatusText = $"找到 {list.Count} 条匹配";
            });
        }
        catch (Exception ex)
        {
            StatusText = $"搜索失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NewRecipe()
    {
        SelectedRecipe = null;
        EditName = string.Empty;
        EditRatedSpeed = 1500;
        EditAllowUnbalanceLeft = 0;
        EditAllowUnbalanceRight = 0;
        EditTrialMass1 = 50;
        EditTrialAngle1 = 0;
        EditTrialMass2 = 50;
        EditTrialAngle2 = 0;
        EditCalibrationFactorLeft = 1.0;
        EditCalibrationFactorRight = 1.0;
        IsEditing = true;
        StatusText = "填写配方信息后点击保存";
    }

    [RelayCommand]
    private void EditRecipe()
    {
        if (SelectedRecipe is null) return;
        EditName = SelectedRecipe.Name;
        EditRatedSpeed = SelectedRecipe.RatedSpeed;
        EditAllowUnbalanceLeft = SelectedRecipe.AllowUnbalanceLeft;
        EditAllowUnbalanceRight = SelectedRecipe.AllowUnbalanceRight;
        EditTrialMass1 = SelectedRecipe.TrialMass1;
        EditTrialAngle1 = SelectedRecipe.TrialAngle1;
        EditTrialMass2 = SelectedRecipe.TrialMass2;
        EditTrialAngle2 = SelectedRecipe.TrialAngle2;
        EditCalibrationFactorLeft = SelectedRecipe.CalibrationFactorLeft;
        EditCalibrationFactorRight = SelectedRecipe.CalibrationFactorRight;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveRecipe()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusText = "请输入配方名称";
            return;
        }

        try
        {
            if (SelectedRecipe is not null)
            {
                SelectedRecipe.Name = EditName;
                SelectedRecipe.RatedSpeed = EditRatedSpeed;
                SelectedRecipe.AllowUnbalanceLeft = EditAllowUnbalanceLeft;
                SelectedRecipe.AllowUnbalanceRight = EditAllowUnbalanceRight;
                SelectedRecipe.TrialMass1 = EditTrialMass1;
                SelectedRecipe.TrialAngle1 = EditTrialAngle1;
                SelectedRecipe.TrialMass2 = EditTrialMass2;
                SelectedRecipe.TrialAngle2 = EditTrialAngle2;
                SelectedRecipe.CalibrationFactorLeft = EditCalibrationFactorLeft;
                SelectedRecipe.CalibrationFactorRight = EditCalibrationFactorRight;
                await _recipeService.UpdateAsync(SelectedRecipe);
                StatusText = $"配方 \"{EditName}\" 已更新";
            }
            else
            {
                var recipe = new Recipe
                {
                    Name = EditName,
                    RatedSpeed = EditRatedSpeed,
                    AllowUnbalanceLeft = EditAllowUnbalanceLeft,
                    AllowUnbalanceRight = EditAllowUnbalanceRight,
                    TrialMass1 = EditTrialMass1,
                    TrialAngle1 = EditTrialAngle1,
                    TrialMass2 = EditTrialMass2,
                    TrialAngle2 = EditTrialAngle2,
                    CalibrationFactorLeft = EditCalibrationFactorLeft,
                    CalibrationFactorRight = EditCalibrationFactorRight
                };
                await _recipeService.CreateAsync(recipe);
                StatusText = $"配方 \"{EditName}\" 已创建";
            }
            IsEditing = false;
            await LoadRecipes();
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        StatusText = "已取消";
    }

    [RelayCommand]
    private async Task DeleteRecipe()
    {
        if (SelectedRecipe is null) return;
        var result = MessageBox.Show(
            $"确定要删除配方 \"{SelectedRecipe.Name}\" 吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _recipeService.DeleteAsync(SelectedRecipe.Id);
            StatusText = $"配方 \"{SelectedRecipe.Name}\" 已删除";
            SelectedRecipe = null;
            await LoadRecipes();
        }
        catch (Exception ex)
        {
            StatusText = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportRecipe()
    {
        if (SelectedRecipe is null)
        {
            StatusText = "请先选择一个配方";
            return;
        }
        var dialog = new SaveFileDialog
        {
            Filter = "JSON 文件|*.json|XML 文件|*.xml",
            FileName = SelectedRecipe.Name
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            string content = dialog.FilterIndex == 1
                ? await _recipeService.ExportToJsonAsync(SelectedRecipe.Id)
                : await _recipeService.ExportToXmlAsync(SelectedRecipe.Id);
            await File.WriteAllTextAsync(dialog.FileName, content);
            StatusText = $"配方已导出到: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportRecipe()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "配方文件|*.json;*.xml",
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            string content = await File.ReadAllTextAsync(dialog.FileName);
            if (dialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                await _recipeService.ImportFromJsonAsync(content);
            else
                await _recipeService.ImportFromXmlAsync(content);
            StatusText = $"配方已从 {dialog.FileName} 导入";
            await LoadRecipes();
        }
        catch (Exception ex)
        {
            StatusText = $"导入失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportAll()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON 文件|*.json|XML 文件|*.xml",
            FileName = "all_recipes"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            string content = dialog.FilterIndex == 1
                ? await _recipeService.ExportAllToJsonAsync()
                : await _recipeService.ExportAllToXmlAsync();
            await File.WriteAllTextAsync(dialog.FileName, content);
            StatusText = $"全部配方已导出到: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败: {ex.Message}";
        }
    }
}
```

- [ ] **Step 2: Build verify**

Run: `dotnet build src/BalanceSystem.App/BalanceSystem.App.csproj`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/ViewModels/RecipeManagementViewModel.cs
git commit -m "feat: add RecipeManagementViewModel with CRUD and import/export commands"
```

---

### Task 6: RecipeManagementView

**Files:**
- Create: `src/BalanceSystem.App/Views/RecipeManagementView.xaml`
- Create: `src/BalanceSystem.App/Views/RecipeManagementView.xaml.cs`

**Interfaces:**
- Consumes: `RecipeManagementViewModel` (Task 5)
- Produces: WPF UserControl with DataGrid for recipe list + edit form panel

- [ ] **Step 1: Create the view code-behind**

```csharp
// src/BalanceSystem.App/Views/RecipeManagementView.xaml.cs
using System.Windows.Controls;

namespace BalanceSystem.App.Views;

public partial class RecipeManagementView : UserControl
{
    public RecipeManagementView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 2: Create the view XAML**

```xml
<!-- src/BalanceSystem.App/Views/RecipeManagementView.xaml -->
<UserControl x:Class="BalanceSystem.App.Views.RecipeManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="clr-namespace:BalanceSystem.App.ViewModels"
             mc:Ignorable="d"
             d:DataContext="{d:DesignInstance Type=vm:RecipeManagementViewModel}">
    <Grid Background="White" Margin="10">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" MinWidth="300"/>
            <ColumnDefinition Width="5"/>
            <ColumnDefinition Width="*" MinWidth="280"/>
        </Grid.ColumnDefinitions>

        <!-- Left: Recipe list -->
        <Grid Grid.Column="0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- Search bar -->
            <DockPanel Grid.Row="0" Margin="0,0,0,8">
                <Button Content="搜索" Width="50" Height="24" DockPanel.Dock="Right"
                        Background="#2C5AA0" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding SearchCommand}"/>
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Height="24"
                         VerticalContentAlignment="Center" FontSize="12"/>
            </DockPanel>

            <!-- Toolbar -->
            <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,6">
                <Button Content="+ 新建" Width="60" Height="26"
                        Background="#28A745" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding NewRecipeCommand}"/>
                <Button Content="✎ 编辑" Width="60" Height="26" Margin="6,0,0,0"
                        Background="#2C5AA0" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding EditRecipeCommand}"/>
                <Button Content="✕ 删除" Width="60" Height="26" Margin="6,0,0,0"
                        Background="#DC3545" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding DeleteRecipeCommand}"/>
                <Button Content="导出" Width="50" Height="26" Margin="6,0,0,0"
                        Background="#6C757D" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding ExportRecipeCommand}"/>
                <Button Content="导入" Width="50" Height="26" Margin="6,0,0,0"
                        Background="#6C757D" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding ImportRecipeCommand}"/>
                <Button Content="全部导出" Width="70" Height="26" Margin="6,0,0,0"
                        Background="#6C757D" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding ExportAllCommand}"/>
            </StackPanel>

            <!-- DataGrid -->
            <DataGrid Grid.Row="2" ItemsSource="{Binding Recipes}"
                      SelectedItem="{Binding SelectedRecipe}"
                      AutoGenerateColumns="False" IsReadOnly="True"
                      SelectionMode="Single" GridLinesVisibility="Horizontal"
                      HeadersVisibility="Column" FontSize="12"
                      Loaded="DataGrid_Loaded">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="40"/>
                    <DataGridTextColumn Header="配方名称" Binding="{Binding Name}" Width="*" MinWidth="100"/>
                    <DataGridTextColumn Header="额定转速" Binding="{Binding RatedSpeed}" Width="70"/>
                    <DataGridTextColumn Header="试重1(g)" Binding="{Binding TrialMass1}" Width="65"/>
                    <DataGridTextColumn Header="试重1(°)" Binding="{Binding TrialAngle1}" Width="65"/>
                    <DataGridTextColumn Header="创建时间" Binding="{Binding CreateTime, StringFormat={}{0:yyyy-MM-dd}}" Width="90"/>
                </DataGrid.Columns>
            </DataGrid>

            <!-- Status -->
            <TextBlock Grid.Row="3" Text="{Binding StatusText}" FontSize="11"
                       Foreground="#666" Margin="0,6,0,0"/>
        </Grid>

        <GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch"
                      Background="#E0E0E0"/>

        <!-- Right: Edit form -->
        <Border Grid.Column="2" BorderBrush="#E0E0E0" BorderThickness="1"
                Visibility="{Binding IsEditing, Converter={StaticResource BoolToVisibility}}">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <StackPanel Margin="12">
                    <TextBlock Text="配方编辑" FontSize="14" FontWeight="Bold"
                               Foreground="#2C5AA0" Margin="0,0,0,12"/>

                    <TextBlock Text="配方名称" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditName, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="额定转速" FontSize="11" Foreground="#666"/>
                    <ComboBox ItemsSource="{Binding SpeedOptions}" Height="26"
                              SelectedItem="{Binding EditRatedSpeed}" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="左面允许不平衡量 (μm)" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditAllowUnbalanceLeft, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="右面允许不平衡量 (μm)" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditAllowUnbalanceRight, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <Separator Margin="0,4"/>
                    <TextBlock Text="试重参数" FontSize="12" FontWeight="Bold"
                               Foreground="#333" Margin="0,0,0,6"/>

                    <TextBlock Text="左面试重质量 (g)" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditTrialMass1, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="左面试重角度 (°)" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditTrialAngle1, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="右面试重质量 (g)" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditTrialMass2, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="右面试重角度 (°)" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditTrialAngle2, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <Separator Margin="0,4"/>
                    <TextBlock Text="校准系数" FontSize="12" FontWeight="Bold"
                               Foreground="#333" Margin="0,0,0,6"/>

                    <TextBlock Text="左面校准系数" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditCalibrationFactorLeft, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <TextBlock Text="右面校准系数" FontSize="11" Foreground="#666"/>
                    <TextBox Text="{Binding EditCalibrationFactorRight, UpdateSourceTrigger=PropertyChanged}"
                             Height="26" FontSize="12" Margin="0,2,0,8"/>

                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,12,0,0">
                        <Button Content="取消" Width="60" Height="28" Margin="0,0,8,0"
                                Background="#6C757D" Foreground="White" BorderThickness="0"
                                FontSize="12" Command="{Binding CancelEditCommand}"/>
                        <Button Content="保存" Width="60" Height="28"
                                Background="#2C5AA0" Foreground="White" BorderThickness="0"
                                FontSize="12" Command="{Binding SaveRecipeCommand}"/>
                    </StackPanel>
                </StackPanel>
            </ScrollViewer>
        </Border>

        <!-- Empty state when not editing -->
        <Border Grid.Column="2" BorderBrush="#E0E0E0" BorderThickness="1"
                Visibility="{Binding IsEditing, Converter={StaticResource BoolToVisibility}, ConverterParameter=invert}">
            <TextBlock Text="选择配方后点击编辑，或点击新建创建配方"
                       FontSize="13" Foreground="#999"
                       HorizontalAlignment="Center" VerticalAlignment="Center"
                       TextWrapping="Wrap" Margin="20"/>
        </Border>
    </Grid>
</UserControl>
```

**NOTE for implementer:** The `ConverterParameter=invert` on the second BoolToVisibilityConverter requires a second converter instance configured with the opposite logic, OR the implementer can add a `BoolToVisibilityInverseConverter` class. The simpler approach is to create a second converter resource `BoolToVisibilityInverse` in App.xaml. Alternatively, replace both uses with the inverse approach — show the form when NOT editing vs show the placeholder when editing.

**Implementer resolution:** Add to [App.xaml](src/BalanceSystem.App/App.xaml) a second converter:

```xml
<converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibility"/>
```

And create the converter as:
```csharp
// src/BalanceSystem.App/Converters/InverseBoolToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BalanceSystem.App.Converters;

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible ? false : true;
}
```

Then update `App.xaml`'s Application.Resources to include:

```xml
<converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibility"/>
```

And update the RecipeManagementView.xaml to use `{StaticResource InverseBoolToVisibility}` instead of `{StaticResource BoolToVisibility}` with `ConverterParameter=invert`.

- [ ] **Step 3: Wire the Loaded event in code-behind**

Add to `RecipeManagementView.xaml.cs`:
```csharp
private async void DataGrid_Loaded(object sender, RoutedEventArgs e)
{
    if (DataContext is RecipeManagementViewModel vm)
        await vm.LoadRecipesCommand.ExecuteAsync(null);
}
```

- [ ] **Step 4: Build verify**

Run: `dotnet build src/BalanceSystem.App/BalanceSystem.App.csproj`
Expected: 0 errors, 0 warnings

- [ ] **Step 5: Commit**

```bash
git add src/BalanceSystem.App/Views/RecipeManagementView.xaml src/BalanceSystem.App/Views/RecipeManagementView.xaml.cs src/BalanceSystem.App/Converters/InverseBoolToVisibilityConverter.cs src/BalanceSystem.App/App.xaml
git commit -m "feat: add RecipeManagementView with DataGrid and edit form"
```

---

### Task 7: HistoryViewModel

**Files:**
- Create: `src/BalanceSystem.App/ViewModels/HistoryViewModel.cs`

**Interfaces:**
- Consumes: `ITestRecordService` (Task 3), `IRecipeService` (Task 2), `TestReportService` (Task 4)
- Produces: `HistoryViewModel` with query filters, paged results, detail view, comparison view, PDF export command

- [ ] **Step 1: Implement HistoryViewModel**

```csharp
// src/BalanceSystem.App/ViewModels/HistoryViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using BalanceSystem.Infrastructure.Reporting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BalanceSystem.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ITestRecordService _recordService;
    private readonly IRecipeService _recipeService;
    private readonly TestReportService _reportService;

    [ObservableProperty] private ObservableCollection<TestRecord> _records = [];
    [ObservableProperty] private ObservableCollection<Recipe> _recipes = [];
    [ObservableProperty] private TestRecord? _selectedRecord;
    [ObservableProperty] private string _statusText = "就绪";

    // ── Query filters ──
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private int? _filterRecipeId;
    [ObservableProperty] private bool? _filterIsPassed;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private long _totalRecords;
    [ObservableProperty] private int _pageSize = 20;

    // ── Comparison ──
    [ObservableProperty] private ObservableCollection<TestRecord> _compareRecords = [];
    [ObservableProperty] private bool _isComparing;
    [ObservableProperty] private string _compareHeader = string.Empty;

    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPrevPage => CurrentPage > 1;

    // Filters for combo boxes
    public static Dictionary<bool?, string> PassFilterOptions => new()
    {
        { null, "全部" },
        { true, "合格" },
        { false, "不合格" }
    };

    public HistoryViewModel(ITestRecordService recordService, IRecipeService recipeService,
                            TestReportService reportService)
    {
        _recordService = recordService;
        _recipeService = recipeService;
        _reportService = reportService;
    }

    [RelayCommand]
    private async Task Load()
    {
        try
        {
            var recipeList = await _recipeService.GetAllAsync();
            Application.Current.Dispatcher.Invoke(() =>
                Recipes = new ObservableCollection<Recipe>(recipeList));
        }
        catch { /* non-critical */ }
        await Query();
    }

    [RelayCommand]
    private async Task Query()
    {
        try
        {
            var records = await _recordService.QueryAsync(
                DateFrom, DateTo, FilterRecipeId, FilterIsPassed, CurrentPage, PageSize);
            var total = await _recordService.CountAsync(
                DateFrom, DateTo, FilterRecipeId, FilterIsPassed);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Records = new ObservableCollection<TestRecord>(records);
                TotalRecords = total;
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(HasPrevPage));
                StatusText = $"共 {total} 条记录，当前第 {CurrentPage}/{TotalPages} 页";
            });
        }
        catch (Exception ex)
        {
            StatusText = $"查询失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (HasNextPage) { CurrentPage++; await Query(); }
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (HasPrevPage) { CurrentPage--; await Query(); }
    }

    [RelayCommand]
    private void ViewDetail()
    {
        if (SelectedRecord is not null)
            StatusText = $"查看记录 #{SelectedRecord.Id} — "
                + $"左面配重 {SelectedRecord.LeftCorrectionMass:F1}g@{SelectedRecord.LeftCorrectionAngle:F0}° "
                + $"右面配重 {SelectedRecord.RightCorrectionMass:F1}g@{SelectedRecord.RightCorrectionAngle:F0}°";
    }

    [RelayCommand]
    private async Task CompareByRecipe()
    {
        if (SelectedRecord is null) return;
        try
        {
            var records = await _recordService.GetByRecipeIdAsync(SelectedRecord.RecipeId, limit: 5);
            var recipe = await _recipeService.GetByIdAsync(SelectedRecord.RecipeId);
            Application.Current.Dispatcher.Invoke(() =>
            {
                CompareRecords = new ObservableCollection<TestRecord>(records);
                IsComparing = true;
                CompareHeader = $"配方 \"{recipe?.Name ?? "未知"}\" 最近 {records.Count} 次测试对比";
            });
        }
        catch (Exception ex)
        {
            StatusText = $"对比失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseCompare()
    {
        IsComparing = false;
        CompareRecords.Clear();
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (SelectedRecord is null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "PDF 文件|*.pdf",
            FileName = $"测试报告_{SelectedRecord.Id}_{SelectedRecord.TestTime:yyyyMMdd}"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Recipe? recipe = null;
            if (SelectedRecord.RecipeId > 0)
                recipe = await _recipeService.GetByIdAsync(SelectedRecord.RecipeId);

            await _reportService.GenerateReportAsync(SelectedRecord, recipe, dialog.FileName);
            StatusText = $"PDF报告已导出到: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetFilters()
    {
        DateFrom = null;
        DateTo = null;
        FilterRecipeId = null;
        FilterIsPassed = null;
        CurrentPage = 1;
    }
}
```

- [ ] **Step 2: Build verify**

Run: `dotnet build src/BalanceSystem.App/BalanceSystem.App.csproj`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/ViewModels/HistoryViewModel.cs
git commit -m "feat: add HistoryViewModel with query, pagination, comparison, and PDF export"
```

---

### Task 8: HistoryView

**Files:**
- Create: `src/BalanceSystem.App/Views/HistoryView.xaml`
- Create: `src/BalanceSystem.App/Views/HistoryView.xaml.cs`

**Interfaces:**
- Consumes: `HistoryViewModel` (Task 7)
- Produces: WPF UserControl with filter panel, DataGrid, detail view, comparison panel

- [ ] **Step 1: Create the view code-behind**

```csharp
// src/BalanceSystem.App/Views/HistoryView.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace BalanceSystem.App.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.HistoryViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
```

- [ ] **Step 2: Create the view XAML**

```xml
<!-- src/BalanceSystem.App/Views/HistoryView.xaml -->
<UserControl x:Class="BalanceSystem.App.Views.HistoryView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="clr-namespace:BalanceSystem.App.ViewModels"
             mc:Ignorable="d"
             d:DataContext="{d:DesignInstance Type=vm:HistoryViewModel}"
             Loaded="UserControl_Loaded">
    <Grid Background="White" Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Filter panel -->
        <Border Grid.Row="0" Background="#F8F9FA" Padding="10" Margin="0,0,0,8">
            <WrapPanel>
                <StackPanel Orientation="Horizontal" Margin="0,0,15,0">
                    <TextBlock Text="起始:" FontSize="11" Foreground="#666" VerticalAlignment="Center"/>
                    <DatePicker SelectedDate="{Binding DateFrom}" Width="120" Height="24"
                                FontSize="11" Margin="4,0"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="0,0,15,0">
                    <TextBlock Text="截止:" FontSize="11" Foreground="#666" VerticalAlignment="Center"/>
                    <DatePicker SelectedDate="{Binding DateTo}" Width="120" Height="24"
                                FontSize="11" Margin="4,0"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="0,0,15,0">
                    <TextBlock Text="配方:" FontSize="11" Foreground="#666" VerticalAlignment="Center"/>
                    <ComboBox ItemsSource="{Binding Recipes}" Width="130" Height="24"
                              SelectedValue="{Binding FilterRecipeId}"
                              SelectedValuePath="Id" DisplayMemberPath="Name"
                              FontSize="11" Margin="4,0"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="0,0,15,0">
                    <TextBlock Text="结果:" FontSize="11" Foreground="#666" VerticalAlignment="Center"/>
                    <ComboBox ItemsSource="{Binding PassFilterOptions}"
                              SelectedValue="{Binding FilterIsPassed}"
                              SelectedValuePath="Key" DisplayMemberPath="Value"
                              Width="80" Height="24" FontSize="11" Margin="4,0"/>
                </StackPanel>
                <Button Content="查询" Width="50" Height="24"
                        Background="#2C5AA0" Foreground="White" BorderThickness="0"
                        FontSize="11" Command="{Binding QueryCommand}"/>
                <Button Content="重置" Width="50" Height="24" Margin="6,0,0,0"
                        Background="#6C757D" Foreground="White" BorderThickness="0"
                        FontSize="11" Command="{Binding ResetFiltersCommand}"/>
            </WrapPanel>
        </Border>

        <!-- Toolbar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,6">
            <Button Content="查看详情" Width="70" Height="24"
                    Background="#2C5AA0" Foreground="White" BorderThickness="0"
                    FontSize="11" Command="{Binding ViewDetailCommand}"/>
            <Button Content="同配方对比" Width="80" Height="24" Margin="6,0,0,0"
                    Background="#17A2B8" Foreground="White" BorderThickness="0"
                    FontSize="11" Command="{Binding CompareByRecipeCommand}"/>
            <Button Content="导出PDF" Width="70" Height="24" Margin="6,0,0,0"
                    Background="#DC3545" Foreground="White" BorderThickness="0"
                    FontSize="11" Command="{Binding ExportPdfCommand}"/>
        </StackPanel>

        <!-- DataGrid -->
        <DataGrid Grid.Row="2" ItemsSource="{Binding Records}"
                  SelectedItem="{Binding SelectedRecord}"
                  AutoGenerateColumns="False" IsReadOnly="True"
                  SelectionMode="Single" GridLinesVisibility="Horizontal"
                  HeadersVisibility="Column" FontSize="11">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="40"/>
                <DataGridTextColumn Header="配方ID" Binding="{Binding RecipeId}" Width="55"/>
                <DataGridTextColumn Header="测试时间" Binding="{Binding TestTime, StringFormat={}{0:yyyy-MM-dd HH:mm}}" Width="130"/>
                <DataGridTextColumn Header="转速" Binding="{Binding Speed, StringFormat={}{0:F0}}" Width="60"/>
                <DataGridTextColumn Header="左配重(g)" Binding="{Binding LeftCorrectionMass, StringFormat={}{0:F2}}" Width="75"/>
                <DataGridTextColumn Header="左角度(°)" Binding="{Binding LeftCorrectionAngle, StringFormat={}{0:F1}}" Width="70"/>
                <DataGridTextColumn Header="右配重(g)" Binding="{Binding RightCorrectionMass, StringFormat={}{0:F2}}" Width="75"/>
                <DataGridTextColumn Header="右角度(°)" Binding="{Binding RightCorrectionAngle, StringFormat={}{0:F1}}" Width="70"/>
                <DataGridTextColumn Header="剩余左" Binding="{Binding ResidualLeft, StringFormat={}{0:F2}}" Width="60"/>
                <DataGridTextColumn Header="剩余右" Binding="{Binding ResidualRight, StringFormat={}{0:F2}}" Width="60"/>
                <DataGridTemplateColumn Header="判定" Width="50">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding IsPassed, Converter={StaticResource BoolToPassFail}}"
                                       Foreground="{Binding IsPassed, Converter={StaticResource BoolToPassFail}}"
                                       FontWeight="Bold" FontSize="11"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Pagination and status -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Orientation="Horizontal">
                <Button Content="上一页" Width="60" Height="24"
                        Background="#E8F0FE" Foreground="#2C5AA0"
                        BorderBrush="#2C5AA0" BorderThickness="1"
                        FontSize="11" IsEnabled="{Binding HasPrevPage}"
                        Command="{Binding PrevPageCommand}"/>
                <Button Content="下一页" Width="60" Height="24" Margin="6,0,0,0"
                        Background="#E8F0FE" Foreground="#2C5AA0"
                        BorderBrush="#2C5AA0" BorderThickness="1"
                        FontSize="11" IsEnabled="{Binding HasNextPage}"
                        Command="{Binding NextPageCommand}"/>
            </StackPanel>
            <TextBlock Grid.Column="1" Text="{Binding StatusText}" FontSize="11"
                       Foreground="#666" HorizontalAlignment="Center" VerticalAlignment="Center"/>
        </Grid>
    </Grid>
</UserControl>
```

**NOTE for implementer:** The `BoolToPassFail` converter returns `"PASS"` or `"FAIL"` — not a color. The DataGridTemplateColumn's Foreground binding to the same converter won't produce a Brush. The implementer should either:
1. Create a new `BoolToPassFailColorConverter` that returns a `SolidColorBrush` (green for true, red for false)
2. Or use a DataTrigger with the existing TextBlock style

**Implementer resolution:** Add `BoolToPassFailColorConverter`:

```csharp
// src/BalanceSystem.App/Converters/BoolToPassFailColorConverter.cs
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BalanceSystem.App.Converters;

public class BoolToPassFailColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45))
            : new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

Register in App.xaml and update the Foreground binding in the DataGrid column.

- [ ] **Step 3: Build verify**

Run: `dotnet build src/BalanceSystem.App/BalanceSystem.App.csproj`
Expected: 0 errors, 0 warnings

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.App/Views/HistoryView.xaml src/BalanceSystem.App/Views/HistoryView.xaml.cs src/BalanceSystem.App/Converters/BoolToPassFailColorConverter.cs src/BalanceSystem.App/App.xaml
git commit -m "feat: add HistoryView with filter panel, DataGrid, and pagination"
```

---

### Task 9: Integration — DI wiring, MainWindow tabs, converters

**Files:**
- Modify: `src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs`
- Modify: `src/BalanceSystem.App/ViewModels/MainViewModel.cs`
- Modify: `src/BalanceSystem.App/Views/MainWindow.xaml`
- Modify: `src/BalanceSystem.App/App.xaml`
- Modify: `src/BalanceSystem.App/App.xaml.cs`

**Interfaces:**
- Consumes: All Phase 2 services (IRecipeService, ITestRecordService, TestReportService) and ViewModels (RecipeManagementViewModel, HistoryViewModel)
- Produces: Fully integrated Phase 2 features accessible via MainWindow tabs

- [ ] **Step 1: Update ServiceCollectionExtensions to register Phase 2 services**

```csharp
// src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs
// Add the following registrations inside AddBalanceSystemServices():

// Phase 2 — Data management services
services.AddSingleton<IRecipeService, RecipeService>();
services.AddSingleton<ITestRecordService, TestRecordService>();
services.AddSingleton<TestReportService>();

// Phase 2 — ViewModels
services.AddSingleton<ViewModels.RecipeManagementViewModel>();
services.AddSingleton<ViewModels.HistoryViewModel>();
```

Also add the necessary using statements at the top:

```csharp
using BalanceSystem.Core.Services;
using BalanceSystem.Infrastructure.Database;
using BalanceSystem.Infrastructure.Reporting;
```

- [ ] **Step 2: Update MainViewModel to hold Phase 2 child ViewModels**

```csharp
// src/BalanceSystem.App/ViewModels/MainViewModel.cs
// Add property declarations:

public RecipeManagementViewModel RecipeManagement { get; }
public HistoryViewModel History { get; }

// Update constructor:
public MainViewModel(
    MonitoringViewModel monitoring,
    BalancingTestViewModel balancingTest,
    RecipeManagementViewModel recipeManagement,
    HistoryViewModel history)
{
    Monitoring = monitoring;
    BalancingTest = balancingTest;
    RecipeManagement = recipeManagement;
    History = history;
}
```

- [ ] **Step 3: Update App.xaml to register new converters**

```xml
<!-- src/BalanceSystem.App/App.xaml -->
<!-- Add inside Application.Resources: -->
<converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibility"/>
<converters:BoolToPassFailColorConverter x:Key="BoolToPassFailColor"/>
```

- [ ] **Step 4: Update MainWindow.xaml to add Phase 2 tabs**

In MainWindow.xaml, add two new TabItems to the TabControl:

```xml
<TabItem Header="Recipes"/>
<TabItem Header="History"/>
```

And add the corresponding content views in the content Grid, with appropriate Visibility converters:

```xml
<views:RecipeManagementView DataContext="{Binding RecipeManagement}"
    Visibility="{Binding SelectedTabIndex, Converter={StaticResource TabToVisibility}, ConverterParameter=2}"/>
<views:HistoryView DataContext="{Binding History}"
    Visibility="{Binding SelectedTabIndex, Converter={StaticResource TabToVisibility}, ConverterParameter=3}"/>
```

- [ ] **Step 5: Build full solution**

Run: `dotnet build`
Expected: 0 errors, 0 warnings

- [ ] **Step 6: Run full test suite**

Run: `dotnet test`
Expected: All 35 tests PASS

- [ ] **Step 7: Commit**

```bash
git add src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs src/BalanceSystem.App/ViewModels/MainViewModel.cs src/BalanceSystem.App/Views/MainWindow.xaml src/BalanceSystem.App/App.xaml src/BalanceSystem.App/App.xaml.cs
git commit -m "feat: integrate Phase 2 services, ViewModels, and tabs into MainWindow"
```

---

### Task 10: Recipe integration with balancing test + test record persistence

**Files:**
- Modify: `src/BalanceSystem.Core/Services/BalancingTestService.cs`
- Modify: `src/BalanceSystem.Core/Services/IBalancingTestService.cs`
- Modify: `src/BalanceSystem.App/ViewModels/BalancingTestViewModel.cs`
- Modify: `src/BalanceSystem.App/Views/BalancingTestView.xaml`
- Modify: `src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `IRecipeService` (Task 2), `ITestRecordService` (Task 3), `Recipe` model (Task 1), `TestRecord` model (Task 1), existing `IBalancingTestService`, `BalancingTestViewModel`, `BalancingTestView`
- Produces: `BalancingTestService` accepts trial mass/angle per-step, `BalancingTestViewModel` loads recipes and exposes selector, `BalancingTestView` shows recipe selector, test records auto-saved to database on test completion

- [ ] **Step 1: Update IBalancingTestService to accept per-step trial parameters**

```csharp
// src/BalanceSystem.Core/Services/IBalancingTestService.cs
// Add two new method overloads:

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
```

- [ ] **Step 2: Implement the new overloads in BalancingTestService**

```csharp
// src/BalanceSystem.Core/Services/BalancingTestService.cs
// Add these methods:

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

// Update the existing RecordCurrentValues() to delegate:
public void RecordCurrentValues()
{
    // Fallback: use hardcoded 50g@0° for backward compatibility
    RecordCurrentValues(trialMass: 50, trialAngle: 0);
}
```

- [ ] **Step 3: Update BalancingTestViewModel to load recipes and pass trial parameters**

```csharp
// src/BalanceSystem.App/ViewModels/BalancingTestViewModel.cs
// Add new fields and modify constructor:

private readonly IRecipeService _recipeService;
private readonly ITestRecordService _testRecordService;

[ObservableProperty] private ObservableCollection<Recipe> _recipes = [];
[ObservableProperty] private Recipe? _selectedRecipe;

// Store raw measurement data for persisting
private double _initialLeftAmp, _initialLeftPhase, _initialRightAmp, _initialRightPhase;
private double _leftTrialLeftAmp, _leftTrialLeftPhase, _leftTrialRightAmp, _leftTrialRightPhase;
private double _rightTrialLeftAmp, _rightTrialLeftPhase, _rightTrialRightAmp, _rightTrialRightPhase;

public BalancingTestViewModel(
    IBalancingTestService testService,
    IRecipeService recipeService,
    ITestRecordService testRecordService)
{
    _testService = testService;
    _recipeService = recipeService;
    _testRecordService = testRecordService;
    _testService.StepChanged += OnStepChanged;
    _testService.StabilityChanged += (_, stable) =>
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsStable = stable;
            StabilityText = stable ? "稳定 — 可以记录数据" : "等待稳定...";
        });
    };
}

// Update RecordStep to use recipe parameters:
[RelayCommand]
private void RecordStep()
{
    if (SelectedRecipe is not null)
        _testService.RecordCurrentValues(SelectedRecipe);
    else
        _testService.RecordCurrentValues();
}

// Update OnStepChanged to save TestRecord when completed:
private void OnStepChanged(object? sender, TestStep step)
{
    Application.Current.Dispatcher.BeginInvoke(async () =>
    {
        CurrentStep = step;
        CanRecord = step is TestStep.InitialRun or TestStep.LeftTrial
                         or TestStep.RightTrial or TestStep.Retest;
        CanAdvance = step is TestStep.InitialRun or TestStep.LeftTrial
                          or TestStep.RightTrial or TestStep.Calculation;
        CanReset = step != TestStep.Idle;

        StepDescription = step switch
        {
            TestStep.Idle => "准备开始测试",
            TestStep.InitialRun => "步骤 1/4：初始运行 — 请等待转速稳定后记录数据",
            TestStep.LeftTrial => SelectedRecipe is not null
                ? $"步骤 2/4：左面加试重 {SelectedRecipe.TrialMass1}g@{SelectedRecipe.TrialAngle1}°"
                : "步骤 2/4：左面加试重 — 请先选择配方",
            TestStep.RightTrial => SelectedRecipe is not null
                ? $"步骤 3/4：右面加试重 {SelectedRecipe.TrialMass2}g@{SelectedRecipe.TrialAngle2}°"
                : "步骤 3/4：右面加试重 — 请先选择配方",
            TestStep.Calculation => "步骤 4/4：计算配重结果",
            TestStep.Completed => "测试完成！可进行复测验证",
            TestStep.Retest => "复测验证中 — 请等待稳定后记录",
            _ => "未知步骤"
        };

        if (step == TestStep.Calculation && _testService.Result is { } result)
        {
            LeftMass = result.LeftMass;
            LeftAngle = result.LeftAngle;
            RightMass = result.RightMass;
            RightAngle = result.RightAngle;
            ResidualLeft = result.ResidualLeftAmplitude;
            ResidualRight = result.ResidualRightAmplitude;
            IsBalanced = result.IsBalanced;
            HasResult = true;
        }

        // Save test record to database when test completes
        if (step == TestStep.Completed && _testService.Result is { } finalResult)
        {
            try
            {
                var record = new TestRecord
                {
                    RecipeId = SelectedRecipe?.Id ?? 0,
                    UserId = 1, // TODO: Phase 3 — use actual logged-in user
                    Speed = Constants.SpeedOptions[SelectedSpeedIndex],
                    TestTime = DateTime.Now,
                    InitialLeftAmplitude = _initialLeftAmp,
                    InitialLeftPhase = _initialLeftPhase,
                    InitialRightAmplitude = _initialRightAmp,
                    InitialRightPhase = _initialRightPhase,
                    LeftTrialMass = SelectedRecipe?.TrialMass1 ?? 50,
                    LeftTrialAngle = SelectedRecipe?.TrialAngle1 ?? 0,
                    RightTrialMass = SelectedRecipe?.TrialMass2 ?? 50,
                    RightTrialAngle = SelectedRecipe?.TrialAngle2 ?? 0,
                    LeftCorrectionMass = finalResult.LeftMass,
                    LeftCorrectionAngle = finalResult.LeftAngle,
                    RightCorrectionMass = finalResult.RightMass,
                    RightCorrectionAngle = finalResult.RightAngle,
                    ResidualLeft = finalResult.ResidualLeftAmplitude,
                    ResidualRight = finalResult.ResidualRightAmplitude,
                    IsPassed = finalResult.IsBalanced
                };
                await _testRecordService.CreateAsync(record);
            }
            catch (Exception ex)
            {
                // Don't block the UI — log would help in real app
                System.Diagnostics.Debug.WriteLine($"Failed to save test record: {ex.Message}");
            }
        }
    });
}

// Add command to load recipes:
[RelayCommand]
private async Task LoadRecipes()
{
    try
    {
        var list = await _recipeService.GetAllAsync();
        Application.Current.Dispatcher.Invoke(() =>
            Recipes = new ObservableCollection<Recipe>(list));
    }
    catch { /* non-critical */ }
}
```

- [ ] **Step 4: Update BalancingTestView to add recipe selector**

Add to `BalancingTestView.xaml`, inside the bottom speed selector bar (Grid.Row="3"):

```xml
<!-- Add before the speed ComboBox in the bottom bar: -->
<TextBlock Text="Recipe:" FontSize="12" Foreground="#666"
           VerticalAlignment="Center" Margin="0,0,10,0"/>
<ComboBox ItemsSource="{Binding Recipes}"
          SelectedItem="{Binding SelectedRecipe}"
          DisplayMemberPath="Name"
          Width="130" Height="24" FontSize="12" Margin="0,0,10,0"/>
<TextBlock Text="Speed:" FontSize="12" Foreground="#666"
           VerticalAlignment="Center" Margin="0,0,10,0"/>
```

- [ ] **Step 5: Register new dependencies in DI**

In `ServiceCollectionExtensions.cs`, update the BalancingTestViewModel registration to pass the new dependencies (constructor DI handles this automatically since the new parameters are already registered):

```csharp
// The existing registration line should already work because IRecipeService
// and ITestRecordService are now registered. But we must ensure they're registered
// BEFORE the ViewModel registrations. Reorder AddBalanceSystemServices() so that
// service registrations come before ViewModel registrations.
```

The reordering:
```csharp
// Business services (register before ViewModels that depend on them)
services.AddSingleton<IRecipeService, RecipeService>();
services.AddSingleton<ITestRecordService, TestRecordService>();
services.AddSingleton<TestReportService>();
services.AddSingleton<IBalancingTestService, BalancingTestService>();

// ViewModels (must come after their dependencies)
services.AddSingleton<ViewModels.RecipeManagementViewModel>();
services.AddSingleton<ViewModels.HistoryViewModel>();
services.AddSingleton<ViewModels.MonitoringViewModel>();
services.AddSingleton<ViewModels.BalancingTestViewModel>();
services.AddSingleton<ViewModels.MainViewModel>();
```

- [ ] **Step 6: Update BalancingTestViewModel to load recipes on initialization**

The `LoadRecipesCommand` should be called when the BalancingTest tab is first shown. The simplest approach: add a Loaded handler in the view's code-behind, or call it from the ViewModel constructor (fire-and-forget):

Add to the BalancingTestViewModel constructor:
```csharp
// Fire-and-forget load recipes
_ = LoadRecipes();
```

- [ ] **Step 7: Build full solution**

Run: `dotnet build`
Expected: 0 errors, 0 warnings

- [ ] **Step 8: Run full test suite**

Run: `dotnet test`
Expected: All 35 tests PASS (no test changes in this task; existing tests unaffected)

- [ ] **Step 9: Commit**

```bash
git add src/BalanceSystem.Core/Services/IBalancingTestService.cs src/BalanceSystem.Core/Services/BalancingTestService.cs src/BalanceSystem.App/ViewModels/BalancingTestViewModel.cs src/BalanceSystem.App/Views/BalancingTestView.xaml src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs
git commit -m "feat: integrate recipe selection into balancing test and auto-save test records"
```

---

## Verification Checklist

After all 10 tasks are complete:

1. **Build:** `dotnet build` — 0 errors, 0 warnings
2. **Tests:** `dotnet test` — 35/35 tests pass (7 FFT/solver + 16 recipe + 9 test record + 3 PDF report)
3. **Tab count:** MainWindow has 4 tabs: Monitor, Balance Test, Recipes, History
4. **Recipe CRUD:** Create, edit, delete recipes via UI; search filters by name/speed
5. **Recipe import/export:** Single recipe and bulk export to JSON/XML; import from file
6. **History query:** Filter by date range, recipe, pass/fail; pagination works
7. **History comparison:** Compare up to 5 recent tests for same recipe
8. **PDF export:** Generate PDF report with Chinese text for any test record
9. **Database:** `Recipes` and `TestRecords` tables auto-created on startup; demo recipe seeded
10. **Recipe-balancing integration:** Recipe selector visible in Balance Test tab; trial mass/angle used from selected recipe; test record auto-saved on completion
11. **Backward compatibility:** Test works without recipe selected (uses default 50g@0° trial mass)

---
