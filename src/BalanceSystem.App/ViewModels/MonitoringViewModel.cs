using System.Windows;
using BalanceSystem.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalanceSystem.Shared;

namespace BalanceSystem.App.ViewModels;

public partial class MonitoringViewModel : ObservableObject
{
    private readonly IDataAcquisitionService _dataAcquisition;

    [ObservableProperty] private double _speed;
    [ObservableProperty] private double _leftAmplitude;
    [ObservableProperty] private double _rightAmplitude;
    [ObservableProperty] private double _leftPhase;
    [ObservableProperty] private double _rightPhase;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isStable;
    [ObservableProperty] private string _connectionStatusText = "未连接";
    [ObservableProperty] private string _stabilityStatusText = "等待数据...";
    [ObservableProperty] private int _selectedSpeedIndex = 2;

    // Polar plot vector properties (Fix 5)
    [ObservableProperty] private double _leftVectorMagnitude;
    [ObservableProperty] private double _leftVectorAngle;
    [ObservableProperty] private double _rightVectorMagnitude;
    [ObservableProperty] private double _rightVectorAngle;

    // Latest vibration sample for waveform rendering (Fix 2)
    [ObservableProperty] private Core.Models.VibrationData? _latestData;

    public int[] SpeedOptions => Constants.SpeedOptions;

    public MonitoringViewModel(IDataAcquisitionService dataAcquisition)
    {
        _dataAcquisition = dataAcquisition;
        _dataAcquisition.DataReceived += OnDataReceived;
        _dataAcquisition.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnDataReceived(object? sender, Core.Models.VibrationData data)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Speed = data.Speed;
            LatestData = data;
        });
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsConnected = connected;
            ConnectionStatusText = connected ? "已连接 (仿真)" : "未连接";
        });
    }

    public void UpdateDisplayValues(double speed, double leftAmp, double rightAmp,
                                     double leftPhase, double rightPhase, bool isStable)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Speed = speed;
            LeftAmplitude = leftAmp;
            RightAmplitude = rightAmp;
            LeftPhase = leftPhase;
            RightPhase = rightPhase;
            IsStable = isStable;
            StabilityStatusText = isStable ? "稳定" : "不稳定";

            // Polar plot vector binding (Fix 5)
            LeftVectorMagnitude = leftAmp;
            LeftVectorAngle = leftPhase;
            RightVectorMagnitude = rightAmp;
            RightVectorAngle = rightPhase;
        });
    }

    [RelayCommand]
    private async Task StartAcquisition()
    {
        await _dataAcquisition.StartAsync();
    }

    [RelayCommand]
    private async Task StopAcquisition()
    {
        await _dataAcquisition.StopAsync();
    }
}
