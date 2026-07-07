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
