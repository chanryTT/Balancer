using System.Collections.ObjectModel;
using System.Threading;
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
    private readonly IRecipeService _recipeService;
    private readonly ITestRecordService _testRecordService;

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

    [ObservableProperty] private ObservableCollection<Recipe> _recipes = [];
    [ObservableProperty] private Recipe? _selectedRecipe;

    // Store raw measurement data for persisting
    private double _initialLeftAmp, _initialLeftPhase, _initialRightAmp, _initialRightPhase;
    private double _leftTrialLeftAmp, _leftTrialLeftPhase, _leftTrialRightAmp, _leftTrialRightPhase;
    private double _rightTrialLeftAmp, _rightTrialLeftPhase, _rightTrialRightAmp, _rightTrialRightPhase;

    // Concurrency guard for OnStepChanged
    private int _updateInProgress;

    public int[] SpeedOptions => Constants.SpeedOptions;

    public BalancingTestViewModel(
        IBalancingTestService testService,
        IRecipeService recipeService,
        ITestRecordService testRecordService)
    {
        _testService = testService;
        _recipeService = recipeService;
        _testRecordService = testRecordService;
        _testService.StepChanged += OnStepChanged;
        _testService.StabilityChanged += (_, stable) =>
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                IsStable = stable;
                StabilityText = stable ? "稳定 — 可以记录数据" : "等待稳定...";
            });
        };

        // Fire-and-forget load recipes
        _ = LoadRecipes();
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
        if (SelectedRecipe is not null)
            _testService.RecordCurrentValues(SelectedRecipe);
        else
            _testService.RecordCurrentValues();

        // Stash the recorded values into the step-appropriate fields for TestRecord persistence
        switch (_testService.CurrentStep)
        {
            case TestStep.InitialRun:
                _initialLeftAmp = _testService.LastLeftAmplitude;
                _initialLeftPhase = _testService.LastLeftPhase;
                _initialRightAmp = _testService.LastRightAmplitude;
                _initialRightPhase = _testService.LastRightPhase;
                break;
            case TestStep.LeftTrial:
                _leftTrialLeftAmp = _testService.LastLeftAmplitude;
                _leftTrialLeftPhase = _testService.LastLeftPhase;
                _leftTrialRightAmp = _testService.LastRightAmplitude;
                _leftTrialRightPhase = _testService.LastRightPhase;
                break;
            case TestStep.RightTrial:
                _rightTrialLeftAmp = _testService.LastLeftAmplitude;
                _rightTrialLeftPhase = _testService.LastLeftPhase;
                _rightTrialRightAmp = _testService.LastRightAmplitude;
                _rightTrialRightPhase = _testService.LastRightPhase;
                break;
        }
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

    [RelayCommand]
    private async Task LoadRecipes()
    {
        try
        {
            var list = await _recipeService.GetAllAsync();
            Application.Current.Dispatcher.Invoke(() =>
                Recipes = new ObservableCollection<Recipe>(list));
        }
        catch { /* non-critical */ }
    }

    private void OnStepChanged(object? sender, TestStep step)
    {
        if (Interlocked.Exchange(ref _updateInProgress, 1) == 1)
            return; // already processing

        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
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
                TestStep.LeftTrial => SelectedRecipe is not null
                    ? $"步骤 2/4：左面加试重 {SelectedRecipe.TrialMass1}g@{SelectedRecipe.TrialAngle1}°"
                    : "步骤 2/4：左面加试重 — 请先选择配方",
                TestStep.RightTrial => SelectedRecipe is not null
                    ? $"步骤 3/4：右面加试重 {SelectedRecipe.TrialMass2}g@{SelectedRecipe.TrialAngle2}°"
                    : "步骤 3/4：右面加试重 — 请先选择配方",
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

            // Save test record to database when test completes
            if (step == TestStep.Completed && _testService.Result is { } finalResult)
            {
                try
                {
                    var record = new TestRecord
                    {
                        RecipeId = SelectedRecipe?.Id ?? 0,
                        UserId = 1, // TODO: Phase 3 — use actual logged-in user
                        Speed = Constants.SpeedOptions[SelectedSpeedIndex],
                        TestTime = DateTime.Now,
                        InitialLeftAmplitude = _initialLeftAmp,
                        InitialLeftPhase = _initialLeftPhase,
                        InitialRightAmplitude = _initialRightAmp,
                        InitialRightPhase = _initialRightPhase,
                        LeftTrialMass = SelectedRecipe?.TrialMass1 ?? 50,
                        LeftTrialAngle = SelectedRecipe?.TrialAngle1 ?? 0,
                        RightTrialMass = SelectedRecipe?.TrialMass2 ?? 50,
                        RightTrialAngle = SelectedRecipe?.TrialAngle2 ?? 0,
                        LeftCorrectionMass = finalResult.LeftMass,
                        LeftCorrectionAngle = finalResult.LeftAngle,
                        RightCorrectionMass = finalResult.RightMass,
                        RightCorrectionAngle = finalResult.RightAngle,
                        ResidualLeft = finalResult.ResidualLeftAmplitude,
                        ResidualRight = finalResult.ResidualRightAmplitude,
                        IsPassed = finalResult.IsBalanced
                    };
                    await _testRecordService.CreateAsync(record);
                }
                catch (Exception ex)
                {
                    // Don't block the UI — log would help in real app
                    System.Diagnostics.Debug.WriteLine($"Failed to save test record: {ex.Message}");
                }
            }
            }
            finally
            {
                Interlocked.Exchange(ref _updateInProgress, 0);
            }
        });
    }
}
