using System.Windows;
using BalanceSystem.Core.Models;
using BalanceSystem.Core.Services;
using BalanceSystem.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalanceSystem.App.ViewModels;

public partial class BalancingTestViewModel : ObservableObject
{
    private readonly IBalancingTestService _testService;

    [ObservableProperty] private TestStep _currentStep = TestStep.Idle;
    [ObservableProperty] private int _selectedSpeedIndex = 2;
    [ObservableProperty] private string _stepDescription = "准备开始测试";
    [ObservableProperty] private string _stabilityText = "等待数据...";
    [ObservableProperty] private bool _isStable;

    [ObservableProperty] private double _leftMass;
    [ObservableProperty] private double _leftAngle;
    [ObservableProperty] private double _rightMass;
    [ObservableProperty] private double _rightAngle;
    [ObservableProperty] private double _residualLeft;
    [ObservableProperty] private double _residualRight;
    [ObservableProperty] private bool _isBalanced;
    [ObservableProperty] private bool _hasResult;

    [ObservableProperty] private bool _canRecord;
    [ObservableProperty] private bool _canAdvance;
    [ObservableProperty] private bool _canReset;

    public int[] SpeedOptions => Constants.SpeedOptions;

    public BalancingTestViewModel(IBalancingTestService testService)
    {
        _testService = testService;
        _testService.StepChanged += OnStepChanged;
        _testService.StabilityChanged += (_, stable) =>
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                IsStable = stable;
                StabilityText = stable ? "稳定 — 可以记录数据" : "等待稳定...";
            });
        };
    }

    [RelayCommand]
    private void StartTest()
    {
        double speed = Constants.SpeedOptions[SelectedSpeedIndex];
        _testService.StartTest(speed);
    }

    [RelayCommand]
    private void RecordStep()
    {
        _testService.RecordCurrentValues();
    }

    [RelayCommand]
    private void AdvanceStep()
    {
        _testService.AdvanceStep();
    }

    [RelayCommand]
    private void Reset()
    {
        _testService.Reset();
        HasResult = false;
    }

    private void OnStepChanged(object? sender, TestStep step)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            CurrentStep = step;
            CanRecord = step is TestStep.InitialRun or TestStep.LeftTrial
                             or TestStep.RightTrial or TestStep.Retest;
            CanAdvance = step is TestStep.InitialRun or TestStep.LeftTrial
                              or TestStep.RightTrial or TestStep.Calculation;
            CanReset = step != TestStep.Idle;

            StepDescription = step switch
            {
                TestStep.Idle => "准备开始测试",
                TestStep.InitialRun => "步骤 1/4：初始运行 — 请等待转速稳定后记录数据",
                TestStep.LeftTrial => "步骤 2/4：左面加试重 — 在左平面加试重后运行并记录",
                TestStep.RightTrial => "步骤 3/4：右面加试重 — 取下左面试重，在右平面加试重后运行并记录",
                TestStep.Calculation => "步骤 4/4：计算配重结果",
                TestStep.Completed => "测试完成！可进行复测验证",
                TestStep.Retest => "复测验证中 — 请等待稳定后记录",
                _ => "未知步骤"
            };

            if (step == TestStep.Calculation && _testService.Result is { } result)
            {
                LeftMass = result.LeftMass;
                LeftAngle = result.LeftAngle;
                RightMass = result.RightMass;
                RightAngle = result.RightAngle;
                ResidualLeft = result.ResidualLeftAmplitude;
                ResidualRight = result.ResidualRightAmplitude;
                IsBalanced = result.IsBalanced;
                HasResult = true;
            }
        });
    }
}
