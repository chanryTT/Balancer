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
        record.Id = (int)await _db.Orm.Insert(record).ExecuteIdentityAsync();
        _logger.LogInformation("Test record created: Id={Id}, RecipeId={RecipeId}, Passed={IsPassed}",
            record.Id, record.RecipeId, record.IsPassed);
        return record;
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
