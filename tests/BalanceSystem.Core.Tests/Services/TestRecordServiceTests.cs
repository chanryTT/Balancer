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
