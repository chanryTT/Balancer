using CommunityToolkit.Mvvm.ComponentModel;

namespace BalanceSystem.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _currentUser = "Admin";
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    [ObservableProperty] private bool _hasAlarm;

    public MonitoringViewModel Monitoring { get; }
    public BalancingTestViewModel BalancingTest { get; }
    public RecipeManagementViewModel RecipeManagement { get; }
    public HistoryViewModel History { get; }

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
}
