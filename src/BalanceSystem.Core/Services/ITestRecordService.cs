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
