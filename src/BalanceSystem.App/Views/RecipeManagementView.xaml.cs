using System.Windows;
using System.Windows.Controls;
using BalanceSystem.App.ViewModels;

namespace BalanceSystem.App.Views;

public partial class RecipeManagementView : UserControl
{
    public RecipeManagementView()
    {
        InitializeComponent();
    }

    private async void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecipeManagementViewModel vm)
            await vm.LoadRecipesCommand.ExecuteAsync(null);
    }
}
