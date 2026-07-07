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
    public static IReadOnlyList<KeyValuePair<bool?, string>> PassFilterOptions => new[]
    {
        new KeyValuePair<bool?, string>(null, "全部"),
        new KeyValuePair<bool?, string>(true, "合格"),
        new KeyValuePair<bool?, string>(false, "不合格")
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
