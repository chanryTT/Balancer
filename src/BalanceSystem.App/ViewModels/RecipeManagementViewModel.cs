using System.Collections.ObjectModel;
using System.IO;
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
