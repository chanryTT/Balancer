using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BalanceSystem.App.ViewModels;

namespace BalanceSystem.App.Views;

public partial class MonitoringView : UserControl
{
    public MonitoringView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MonitoringViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is MonitoringViewModel newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitoringViewModel.LatestData))
        {
            var vm = (MonitoringViewModel)sender!;
            if (vm.LatestData is { } data)
            {
                Waveform.PushData(data);
                Waveform.InvalidateVisual();
            }
        }
    }
}
