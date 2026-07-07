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
        recipe.Id = (int)await _db.Orm.Insert(recipe).ExecuteIdentityAsync();
        _logger.LogInformation("Recipe created: {Name} (Id={Id})", recipe.Name, recipe.Id);
        return recipe;
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
        bool isNumeric = int.TryParse(keyword, out int speedKw);
        var query = _db.Orm.Select<Recipe>();
        if (isNumeric)
            query = query.Where(r => r.Name.Contains(keyword) || r.RatedSpeed == speedKw);
        else
            query = query.Where(r => r.Name.Contains(keyword));
        return query.OrderByDescending(r => r.CreateTime).ToListAsync();
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

        var doc = new XDocument(
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
        );
        return doc.ToString();
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
        return doc.ToString();
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
