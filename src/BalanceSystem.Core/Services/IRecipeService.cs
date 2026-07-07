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
