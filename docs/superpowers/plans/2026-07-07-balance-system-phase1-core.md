# BalanceSystem Phase 1: Core Framework & Dynamic Balancing Engine

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the WPF application skeleton with MVVM foundation, simulated data acquisition, real-time monitoring UI (waveform + numeric displays + polar plot), the dual-plane influence-coefficient balancing algorithm, and a step-by-step test wizard UI — all working end-to-end with simulated CSV data.

**Architecture:** Four-layer onion: `BalanceSystem.Core` (interfaces, models, algorithms, business services) → `BalanceSystem.Infrastructure` (simulated data acquisition, database via FreeSql, Serilog) → `BalanceSystem.App` (WPF views, ViewModels with CommunityToolkit.Mvvm, custom-drawn controls) → `BalanceSystem.Shared` (constants, helpers). DI wires everything via `Microsoft.Extensions.DependencyInjection`. All device access goes through interfaces so real hardware can replace simulations later without touching upper layers.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, FreeSql + SQL Server, Serilog, Microsoft.Extensions.DependencyInjection, xUnit (tests)

**Design decisions for Phase 1:**
- Simulation reads CSV files (6400 Hz, timestamp + left channel + right channel + tach pulse) through `IDataAcquisitionService` — same interface real Modbus will use later.
- Custom WPF controls (WaveformControl, PolarPlotControl, NumericDisplay) use `DrawingVisual` / `WriteableBitmap` for 30+ fps rendering — no third-party chart library.
- The 4-step balancing wizard is a single ViewModel with an enum-state machine driving step transitions.
- Database uses SQL Server via FreeSql Code-First; seed a default admin user on first run.

## Global Constraints

- All UI text in Chinese
- Simulation mode only — no real hardware dependencies in Phase 1
- MVVM strictly enforced: zero code-behind in Views except `InitializeComponent()`
- All device access through interfaces defined in `BalanceSystem.Core/Interfaces/`
- .NET 10 target framework
- CommunityToolkit.Mvvm for source-generated MVVM (no manual INotifyPropertyChanged)
- FreeSql Code-First for database
- Serilog structured logging throughout
- xUnit for all tests
- Commit after every completed task

---

## File Structure (Phase 1)

```
BalanceSystem/
├── BalanceSystem.sln
├── src/
│   ├── BalanceSystem.Core/
│   │   ├── BalanceSystem.Core.csproj
│   │   ├── Interfaces/
│   │   │   ├── IDataAcquisitionService.cs
│   │   │   └── ISpeedMeasurementService.cs
│   │   ├── Models/
│   │   │   ├── VibrationData.cs
│   │   │   ├── BalancingResult.cs
│   │   │   ├── TestStep.cs
│   │   │   └── User.cs
│   │   ├── Algorithms/
│   │   │   ├── FftCalculator.cs
│   │   │   └── InfluenceCoefficientSolver.cs
│   │   └── Services/
│   │       ├── IBalancingTestService.cs
│   │       └── BalancingTestService.cs
│   ├── BalanceSystem.Infrastructure/
│   │   ├── BalanceSystem.Infrastructure.csproj
│   │   ├── DataAcquisition/
│   │   │   └── CsvSimulationService.cs
│   │   ├── Database/
│   │   │   ├── AppDbContext.cs
│   │   │   └── DatabaseInitializer.cs
│   │   └── Logging/
│   │       └── LogConfig.cs
│   ├── BalanceSystem.App/
│   │   ├── BalanceSystem.App.csproj
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml / .cs
│   │   │   ├── MonitoringView.xaml / .cs
│   │   │   └── BalancingTestView.xaml / .cs
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   ├── MonitoringViewModel.cs
│   │   │   └── BalancingTestViewModel.cs
│   │   ├── Controls/
│   │   │   ├── WaveformControl.cs
│   │   │   ├── PolarPlotControl.cs
│   │   │   └── NumericDisplay.cs
│   │   ├── Converters/
│   │   │   ├── BoolToVisibilityConverter.cs
│   │   │   └── BoolToPassFailConverter.cs
│   │   └── DependencyInjection/
│   │       └── ServiceCollectionExtensions.cs
│   └── BalanceSystem.Shared/
│       ├── BalanceSystem.Shared.csproj
│       └── Constants.cs
└── tests/
    └── BalanceSystem.Core.Tests/
        ├── BalanceSystem.Core.Tests.csproj
        ├── Algorithms/
        │   ├── FftCalculatorTests.cs
        │   └── InfluenceCoefficientSolverTests.cs
        └── Services/
            └── BalancingTestServiceTests.cs
```

---

### Task 1: Create solution structure and all projects

**Files:**
- Create: `BalanceSystem.sln`
- Create: `src/BalanceSystem.Shared/BalanceSystem.Shared.csproj`
- Create: `src/BalanceSystem.Shared/Constants.cs`
- Create: `src/BalanceSystem.Core/BalanceSystem.Core.csproj`
- Create: `src/BalanceSystem.Infrastructure/BalanceSystem.Infrastructure.csproj`
- Create: `src/BalanceSystem.App/BalanceSystem.App.csproj` (WPF)
- Create: `tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj`

**Interfaces:**
- Produces: All `.csproj` files with correct .NET 10 TFMs and project references

- [ ] **Step 1: Create solution file**

Run:
```bash
dotnet new sln -n BalanceSystem -o /e/Balancer --force
```
Expected: `BalanceSystem.sln` created

- [ ] **Step 2: Create Shared class library**

Run:
```bash
dotnet new classlib -n BalanceSystem.Shared -o /e/Balancer/src/BalanceSystem.Shared -f net10.0
```
Then write `src/BalanceSystem.Shared/Constants.cs`:

```csharp
namespace BalanceSystem.Shared;

public static class Constants
{
    public const double DefaultSampleRate = 6400.0;
    public const int DefaultSpeedRpm = 1500;
    public static readonly int[] SpeedOptions = { 500, 1000, 1500, 2000, 2500, 3000 };
    public const double StabilityCheckDurationSeconds = 3.0;
    public const double AmplitudeStabilityThreshold = 0.05;
    public const double PhaseStabilityThreshold = 2.0;
    public const double SpeedStabilityPercent = 2.0;
}
```

- [ ] **Step 3: Create Core class library**

Run:
```bash
dotnet new classlib -n BalanceSystem.Core -o /e/Balancer/src/BalanceSystem.Core -f net10.0
dotnet add /e/Balancer/src/BalanceSystem.Core reference /e/Balancer/src/BalanceSystem.Shared
```

- [ ] **Step 4: Create Infrastructure class library**

Run:
```bash
dotnet new classlib -n BalanceSystem.Infrastructure -o /e/Balancer/src/BalanceSystem.Infrastructure -f net10.0
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure reference /e/Balancer/src/BalanceSystem.Core
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure reference /e/Balancer/src/BalanceSystem.Shared
```

- [ ] **Step 5: Create WPF App project**

Run:
```bash
dotnet new wpf -n BalanceSystem.App -o /e/Balancer/src/BalanceSystem.App -f net10.0
dotnet add /e/Balancer/src/BalanceSystem.App reference /e/Balancer/src/BalanceSystem.Core
dotnet add /e/Balancer/src/BalanceSystem.App reference /e/Balancer/src/BalanceSystem.Infrastructure
dotnet add /e/Balancer/src/BalanceSystem.App reference /e/Balancer/src/BalanceSystem.Shared
```

- [ ] **Step 6: Create test project**

Run:
```bash
dotnet new xunit -n BalanceSystem.Core.Tests -o /e/Balancer/tests/BalanceSystem.Core.Tests -f net10.0
dotnet add /e/Balancer/tests/BalanceSystem.Core.Tests reference /e/Balancer/src/BalanceSystem.Core
```

- [ ] **Step 7: Add all projects to solution**

Run:
```bash
cd /e/Balancer
dotnet sln add src/BalanceSystem.Shared
dotnet sln add src/BalanceSystem.Core
dotnet sln add src/BalanceSystem.Infrastructure
dotnet sln add src/BalanceSystem.App
dotnet sln add tests/BalanceSystem.Core.Tests
```

- [ ] **Step 8: Restore and build**

Run: `dotnet restore && dotnet build`
Expected: Build succeeded, 0 errors

- [ ] **Step 9: Commit**

```bash
git init
git add -A
git commit -m "feat: create solution structure with 5 projects (Shared, Core, Infrastructure, App, Tests)"
```

---

### Task 2: Define core interface IDataAcquisitionService

**Files:**
- Create: `src/BalanceSystem.Core/Interfaces/IDataAcquisitionService.cs`
- Create: `src/BalanceSystem.Core/Models/VibrationData.cs`

**Interfaces:**
- Produces:
  - `IDataAcquisitionService` — `StartAsync()`, `StopAsync()`, `GetCurrentValues()`, `GetWaveformData(int windowSeconds)`, `IsConnected { get; }`, event `DataReceived`, event `ConnectionStateChanged`
  - `VibrationData` record — `Timestamp`, `LeftChannel`, `RightChannel`, `TachPulse`, `Speed`

- [ ] **Step 1: Write VibrationData model**

Write `src/BalanceSystem.Core/Models/VibrationData.cs`:

```csharp
namespace BalanceSystem.Core.Models;

public readonly record struct VibrationData(
    DateTime Timestamp,
    double LeftChannel,
    double RightChannel,
    bool TachPulse,
    double Speed
);
```

- [ ] **Step 2: Write IDataAcquisitionService interface**

Write `src/BalanceSystem.Core/Interfaces/IDataAcquisitionService.cs`:

```csharp
using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Interfaces;

public interface IDataAcquisitionService
{
    bool IsConnected { get; }
    event EventHandler<VibrationData>? DataReceived;
    event EventHandler<bool>? ConnectionStateChanged;

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    (double LeftAmplitude, double RightAmplitude, double LeftPhase, double RightPhase, double Speed) GetCurrentValues();
    VibrationData[] GetWaveformData(int windowSeconds = 5);
}
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build /e/Balancer/src/BalanceSystem.Core`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.Core/Interfaces/ src/BalanceSystem.Core/Models/
git commit -m "feat: define IDataAcquisitionService interface and VibrationData model"
```

---

### Task 3: Define ISpeedMeasurementService interface

**Files:**
- Create: `src/BalanceSystem.Core/Interfaces/ISpeedMeasurementService.cs`

**Interfaces:**
- Produces: `ISpeedMeasurementService` — `CurrentSpeed { get; }`, event `SpeedChanged`, event `ZeroPulseDetected`, `GetCurrentAngle()`

- [ ] **Step 1: Write interface**

Write `src/BalanceSystem.Core/Interfaces/ISpeedMeasurementService.cs`:

```csharp
namespace BalanceSystem.Core.Interfaces;

public interface ISpeedMeasurementService
{
    double CurrentSpeed { get; }
    event EventHandler<double>? SpeedChanged;
    event EventHandler? ZeroPulseDetected;
    double GetCurrentAngle();
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build /e/Balancer/src/BalanceSystem.Core`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.Core/Interfaces/ISpeedMeasurementService.cs
git commit -m "feat: define ISpeedMeasurementService interface"
```

---

### Task 4: Add NuGet packages

**Files:**
- Modify: `src/BalanceSystem.Core/BalanceSystem.Core.csproj`
- Modify: `src/BalanceSystem.Infrastructure/BalanceSystem.Infrastructure.csproj`
- Modify: `src/BalanceSystem.App/BalanceSystem.App.csproj`
- Modify: `tests/BalanceSystem.Core.Tests/BalanceSystem.Core.Tests.csproj`

**Interfaces:**
- Produces: All NuGet packages installed and build passing

- [ ] **Step 1: Add Core packages**

Run:
```bash
dotnet add /e/Balancer/src/BalanceSystem.Core package CommunityToolkit.Mvvm
```

- [ ] **Step 2: Add Infrastructure packages**

Run:
```bash
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure package FreeSql
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure package FreeSql.Provider.SqlServer
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure package Serilog
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure package Serilog.Sinks.Console
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure package Serilog.Sinks.File
dotnet add /e/Balancer/src/BalanceSystem.Infrastructure package Microsoft.Extensions.Configuration.Json
```

- [ ] **Step 3: Add App packages**

Run:
```bash
dotnet add /e/Balancer/src/BalanceSystem.App package CommunityToolkit.Mvvm
dotnet add /e/Balancer/src/BalanceSystem.App package Microsoft.Extensions.DependencyInjection
dotnet add /e/Balancer/src/BalanceSystem.App package Microsoft.Extensions.Hosting
```

- [ ] **Step 4: Add test packages**

Run:
```bash
dotnet add /e/Balancer/tests/BalanceSystem.Core.Tests package FluentAssertions
dotnet add /e/Balancer/tests/BalanceSystem.Core.Tests package coverlet.collector
```

- [ ] **Step 5: Build all to verify packages resolve**

Run: `dotnet build /e/Balancer`
Expected: Build succeeded, 0 warnings

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: add NuGet packages for MVVM, FreeSql, Serilog, DI, and testing"
```

---

### Task 5: FFT Algorithm — write failing tests

**Files:**
- Create: `tests/BalanceSystem.Core.Tests/Algorithms/FftCalculatorTests.cs`

**Interfaces:**
- Consumes: (none — first algorithm task)
- Produces: `FftCalculator` static class with `Compute(double[] signal, double sampleRate)` → `(double[] magnitudes, double[] phases, double[] frequencies)`

- [ ] **Step 1: Write the failing tests**

Write `tests/BalanceSystem.Core.Tests/Algorithms/FftCalculatorTests.cs`:

```csharp
using BalanceSystem.Core.Algorithms;
using FluentAssertions;

namespace BalanceSystem.Core.Tests.Algorithms;

public class FftCalculatorTests
{
    [Fact]
    public void Compute_PureSineWave_ReturnsCorrectFrequencyAndAmplitude()
    {
        const double sampleRate = 6400.0;
        const double freq = 50.0;
        const double amplitude = 2.0;
        const int n = 6400;
        var signal = new double[n];
        for (int i = 0; i < n; i++)
            signal[i] = amplitude * Math.Sin(2 * Math.PI * freq * i / sampleRate);

        var (magnitudes, phases, frequencies) = FftCalculator.Compute(signal, sampleRate);

        int peakBin = 0;
        double maxMag = 0;
        for (int i = 1; i < magnitudes.Length / 2; i++)
        {
            if (magnitudes[i] > maxMag)
            {
                maxMag = magnitudes[i];
                peakBin = i;
            }
        }

        frequencies[peakBin].Should().BeApproximately(freq, 1.0);
        maxMag.Should().BeApproximately(amplitude, 0.1);
    }

    [Fact]
    public void Compute_EmptySignal_ReturnsEmptyArrays()
    {
        var (magnitudes, phases, frequencies) = FftCalculator.Compute(Array.Empty<double>(), 6400.0);
        magnitudes.Should().BeEmpty();
        phases.Should().BeEmpty();
        frequencies.Should().BeEmpty();
    }

    [Fact]
    public void Compute_SignalLengthNotPowerOfTwo_StillReturnsValidResult()
    {
        const double sampleRate = 6400.0;
        var signal = new double[5000];
        var rng = new Random(42);
        for (int i = 0; i < signal.Length; i++)
            signal[i] = rng.NextDouble();

        var (magnitudes, phases, frequencies) = FftCalculator.Compute(signal, sampleRate);

        magnitudes.Should().NotBeEmpty();
        frequencies[0].Should().Be(0.0);
        frequencies[^1].Should().BeApproximately(sampleRate / 2, sampleRate / magnitudes.Length * 2);
    }

    [Fact]
    public void Compute_PhaseOfCosineAtZero_IsZero()
    {
        const double sampleRate = 6400.0;
        const double freq = 100.0;
        const double amplitude = 1.0;
        const int n = 4096;
        var signal = new double[n];
        for (int i = 0; i < n; i++)
            signal[i] = amplitude * Math.Cos(2 * Math.PI * freq * i / sampleRate);

        var (magnitudes, phases, frequencies) = FftCalculator.Compute(signal, sampleRate);

        int peakBin = 0;
        double maxMag = 0;
        for (int i = 1; i < magnitudes.Length / 2; i++)
        {
            if (magnitudes[i] > maxMag)
            {
                maxMag = magnitudes[i];
                peakBin = i;
            }
        }
        phases[peakBin].Should().BeApproximately(0.0, 0.1);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test /e/Balancer/tests/BalanceSystem.Core.Tests --filter "FullyQualifiedName~FftCalculatorTests"`
Expected: BUILD FAIL — `FftCalculator` class does not exist yet

- [ ] **Step 3: Commit**

```bash
git add tests/BalanceSystem.Core.Tests/Algorithms/FftCalculatorTests.cs
git commit -m "test: add failing FFT calculator tests"
```

---

### Task 6: FFT Algorithm — implement

**Files:**
- Create: `src/BalanceSystem.Core/Algorithms/FftCalculator.cs`

**Interfaces:**
- Produces: `FftCalculator.Compute(double[] signal, double sampleRate)` → `(double[] magnitudes, double[] phases, double[] frequencies)` and `ExtractFundamental(double[] signal, double sampleRate, double speedRpm)` → `(double amplitude, double phase)`

- [ ] **Step 1: Write FftCalculator implementation**

Write `src/BalanceSystem.Core/Algorithms/FftCalculator.cs`:

```csharp
using System.Numerics;

namespace BalanceSystem.Core.Algorithms;

public static class FftCalculator
{
    /// <summary>
    /// Computes the FFT of a real-valued signal. Returns single-sided magnitude spectrum,
    /// phase angles (in degrees), and corresponding frequency bins.
    /// </summary>
    public static (double[] Magnitudes, double[] Phases, double[] Frequencies) Compute(
        double[] signal, double sampleRate)
    {
        if (signal.Length == 0)
            return (Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>());

        int n = NextPowerOfTwo(signal.Length);
        var complex = new Complex[n];
        for (int i = 0; i < signal.Length; i++)
            complex[i] = new Complex(signal[i], 0);

        Fft(complex, n);

        int halfN = n / 2;
        var magnitudes = new double[halfN];
        var phases = new double[halfN];
        var frequencies = new double[halfN];

        double freqResolution = sampleRate / n;
        for (int i = 0; i < halfN; i++)
        {
            magnitudes[i] = 2.0 * complex[i].Magnitude / n;
            phases[i] = complex[i].Phase * 180.0 / Math.PI;
            if (phases[i] < 0) phases[i] += 360.0;
            frequencies[i] = i * freqResolution;
        }

        return (magnitudes, phases, frequencies);
    }

    /// <summary>
    /// Extracts the 1x (fundamental frequency) amplitude and phase given the rotational speed.
    /// </summary>
    public static (double Amplitude, double Phase) ExtractFundamental(
        double[] signal, double sampleRate, double speedRpm)
    {
        double fundamentalFreq = speedRpm / 60.0;
        var (magnitudes, phases, frequencies) = Compute(signal, sampleRate);

        int bestBin = 0;
        double bestDiff = double.MaxValue;
        for (int i = 1; i < frequencies.Length; i++)
        {
            double diff = Math.Abs(frequencies[i] - fundamentalFreq);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestBin = i;
            }
        }

        return (magnitudes[bestBin], phases[bestBin]);
    }

    private static void Fft(Complex[] buffer, int n)
    {
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            if (i < j)
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);

            int m = n >> 1;
            while (m > 0 && (j & m) != 0)
            {
                j &= ~m;
                m >>= 1;
            }
            j |= m;
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2.0 * Math.PI / len;
            var w = new Complex(Math.Cos(angle), Math.Sin(angle));
            for (int i = 0; i < n; i += len)
            {
                Complex ww = Complex.One;
                int halfLen = len >> 1;
                for (int k = 0; k < halfLen; k++)
                {
                    Complex t = ww * buffer[i + k + halfLen];
                    buffer[i + k + halfLen] = buffer[i + k] - t;
                    buffer[i + k] += t;
                    ww *= w;
                }
            }
        }
    }

    private static int NextPowerOfTwo(int x)
    {
        if (x <= 0) return 1;
        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test /e/Balancer/tests/BalanceSystem.Core.Tests --filter "FullyQualifiedName~FftCalculatorTests"`
Expected: All 4 tests PASS

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.Core/Algorithms/FftCalculator.cs
git commit -m "feat: implement FFT calculator with Cooley-Tukey algorithm"
```

---

### Task 7: Influence Coefficient Solver — write failing tests

**Files:**
- Create: `tests/BalanceSystem.Core.Tests/Algorithms/InfluenceCoefficientSolverTests.cs`

**Interfaces:**
- Consumes: (none — independent algorithm)
- Produces: `InfluenceCoefficientSolver` with `AddInitialRun`, `AddLeftTrialRun`, `AddRightTrialRun`, `Solve()` methods

- [ ] **Step 1: Write failing tests**

Write `tests/BalanceSystem.Core.Tests/Algorithms/InfluenceCoefficientSolverTests.cs`:

```csharp
using BalanceSystem.Core.Algorithms;
using BalanceSystem.Core.Models;
using FluentAssertions;

namespace BalanceSystem.Core.Tests.Algorithms;

public class InfluenceCoefficientSolverTests
{
    [Fact]
    public void Solve_WithSyntheticData_ReturnsFiniteCorrectionWeights()
    {
        var solver = new InfluenceCoefficientSolver();

        solver.AddInitialRun(
            leftAmplitude: 55.0, leftPhase: 35.0,
            rightAmplitude: 42.0, rightPhase: 195.0);

        solver.AddLeftTrialRun(
            leftAmplitude: 60.0, leftPhase: 42.0,
            rightAmplitude: 38.0, rightPhase: 200.0,
            trialMass: 50.0, trialAngle: 90.0);

        solver.AddRightTrialRun(
            leftAmplitude: 48.0, leftPhase: 28.0,
            rightAmplitude: 50.0, rightPhase: 210.0,
            trialMass: 50.0, trialAngle: 270.0);

        var result = solver.Solve();

        result.Should().NotBeNull();
        result.LeftMass.Should().BePositive();
        result.RightMass.Should().BePositive();
        result.LeftAngle.Should().BeInRange(0.0, 360.0);
        result.RightAngle.Should().BeInRange(0.0, 360.0);
        result.ResidualLeftAmplitude.Should().BeLessThan(55.0);
        result.ResidualRightAmplitude.Should().BeLessThan(42.0);
    }

    [Fact]
    public void Solve_IncompleteSteps_ThrowsInvalidOperationException()
    {
        var solver = new InfluenceCoefficientSolver();
        solver.AddInitialRun(10, 0, 10, 0);

        Action act = () => solver.Solve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Solve_IdenticalTrials_ProducesFiniteResult()
    {
        var solver = new InfluenceCoefficientSolver();
        solver.AddInitialRun(10.0, 45.0, 10.0, 135.0);
        solver.AddLeftTrialRun(10.001, 45.001, 10.001, 135.001, 0.001, 0.0);
        solver.AddRightTrialRun(10.0, 45.0, 10.001, 135.001, 0.001, 0.0);

        var result = solver.Solve();
        result.LeftMass.Should().NotBe(double.NaN);
        result.RightMass.Should().NotBe(double.NaN);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test /e/Balancer/tests/BalanceSystem.Core.Tests --filter "FullyQualifiedName~InfluenceCoefficientSolverTests"`
Expected: BUILD FAIL (class does not exist)

- [ ] **Step 3: Commit**

```bash
git add tests/BalanceSystem.Core.Tests/Algorithms/InfluenceCoefficientSolverTests.cs
git commit -m "test: add failing influence coefficient solver tests"
```

---

### Task 8: Influence Coefficient Solver — implement

**Files:**
- Create: `src/BalanceSystem.Core/Models/BalancingResult.cs`
- Create: `src/BalanceSystem.Core/Algorithms/InfluenceCoefficientSolver.cs`

**Interfaces:**
- Produces: `BalancingResult` record, `InfluenceCoefficientSolver` class

- [ ] **Step 1: Write BalancingResult model**

Write `src/BalanceSystem.Core/Models/BalancingResult.cs`:

```csharp
namespace BalanceSystem.Core.Models;

public record BalancingResult
{
    public double LeftMass { get; init; }
    public double LeftAngle { get; init; }
    public double RightMass { get; init; }
    public double RightAngle { get; init; }
    public double ResidualLeftAmplitude { get; init; }
    public double ResidualRightAmplitude { get; init; }
    public bool IsBalanced { get; init; }
    public DateTime CalculatedAt { get; init; } = DateTime.Now;
}
```

- [ ] **Step 2: Write InfluenceCoefficientSolver**

Write `src/BalanceSystem.Core/Algorithms/InfluenceCoefficientSolver.cs`:

```csharp
using System.Numerics;
using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Algorithms;

public class InfluenceCoefficientSolver
{
    private Complex _vInitialLeft, _vInitialRight;
    private Complex _vLeftTrialLeft, _vLeftTrialRight;
    private Complex _vRightTrialLeft, _vRightTrialRight;
    private Complex _trialLeft, _trialRight;
    private bool _hasInitial, _hasLeftTrial, _hasRightTrial;

    public void AddInitialRun(double leftAmplitude, double leftPhase,
                               double rightAmplitude, double rightPhase)
    {
        _vInitialLeft = FromPolar(leftAmplitude, leftPhase);
        _vInitialRight = FromPolar(rightAmplitude, rightPhase);
        _hasInitial = true;
    }

    public void AddLeftTrialRun(double leftAmplitude, double leftPhase,
                                 double rightAmplitude, double rightPhase,
                                 double trialMass, double trialAngle)
    {
        _vLeftTrialLeft = FromPolar(leftAmplitude, leftPhase);
        _vLeftTrialRight = FromPolar(rightAmplitude, rightPhase);
        _trialLeft = FromPolar(trialMass, trialAngle);
        _hasLeftTrial = true;
    }

    public void AddRightTrialRun(double leftAmplitude, double leftPhase,
                                  double rightAmplitude, double rightPhase,
                                  double trialMass, double trialAngle)
    {
        _vRightTrialLeft = FromPolar(leftAmplitude, leftPhase);
        _vRightTrialRight = FromPolar(rightAmplitude, rightPhase);
        _trialRight = FromPolar(trialMass, trialAngle);
        _hasRightTrial = true;
    }

    public BalancingResult Solve()
    {
        if (!_hasInitial || !_hasLeftTrial || !_hasRightTrial)
            throw new InvalidOperationException(
                "All 4 steps are required: initial run, left trial, right trial, and then solve.");

        Complex a11 = (_vLeftTrialLeft - _vInitialLeft) / _trialLeft;
        Complex a12 = (_vRightTrialLeft - _vInitialLeft) / _trialRight;
        Complex a21 = (_vLeftTrialRight - _vInitialRight) / _trialLeft;
        Complex a22 = (_vRightTrialRight - _vInitialRight) / _trialRight;

        Complex det = a11 * a22 - a12 * a21;

        if (Complex.Abs(det) < 1e-15)
        {
            return new BalancingResult
            {
                LeftMass = 0, LeftAngle = 0,
                RightMass = 0, RightAngle = 0,
                ResidualLeftAmplitude = _vInitialLeft.Magnitude,
                ResidualRightAmplitude = _vInitialRight.Magnitude,
                IsBalanced = false
            };
        }

        Complex negVLeft = -_vInitialLeft;
        Complex negVRight = -_vInitialRight;

        Complex correctionLeft = (negVLeft * a22 - negVRight * a12) / det;
        Complex correctionRight = (negVRight * a11 - negVLeft * a21) / det;

        Complex residualLeft = _vInitialLeft + a11 * correctionLeft + a12 * correctionRight;
        Complex residualRight = _vInitialRight + a21 * correctionLeft + a22 * correctionRight;

        return new BalancingResult
        {
            LeftMass = Math.Round(correctionLeft.Magnitude, 2),
            LeftAngle = Math.Round(ToDegrees(correctionLeft.Phase), 1),
            RightMass = Math.Round(correctionRight.Magnitude, 2),
            RightAngle = Math.Round(ToDegrees(correctionRight.Phase), 1),
            ResidualLeftAmplitude = Math.Round(residualLeft.Magnitude, 4),
            ResidualRightAmplitude = Math.Round(residualRight.Magnitude, 4),
            IsBalanced = residualLeft.Magnitude < 1.0 && residualRight.Magnitude < 1.0,
            CalculatedAt = DateTime.Now
        };
    }

    private static Complex FromPolar(double magnitude, double phaseDegrees)
    {
        double radians = phaseDegrees * Math.PI / 180.0;
        return new Complex(magnitude * Math.Cos(radians), magnitude * Math.Sin(radians));
    }

    private static double ToDegrees(double radians)
    {
        double deg = radians * 180.0 / Math.PI;
        if (deg < 0) deg += 360.0;
        return deg;
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test /e/Balancer/tests/BalanceSystem.Core.Tests --filter "FullyQualifiedName~InfluenceCoefficientSolverTests"`
Expected: All 3 tests PASS

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.Core/Models/BalancingResult.cs src/BalanceSystem.Core/Algorithms/InfluenceCoefficientSolver.cs
git commit -m "feat: implement dual-plane influence coefficient solver with complex arithmetic"
```

---

### Task 9: CSV Simulation Data Acquisition Service

**Files:**
- Create: `src/BalanceSystem.Infrastructure/DataAcquisition/CsvSimulationService.cs`

**Interfaces:**
- Consumes: `IDataAcquisitionService` (from Task 2), `VibrationData` (from Task 2)
- Produces: `CsvSimulationService` — reads CSV at 6400 Hz, fires `DataReceived` per row, implements full `IDataAcquisitionService`

- [ ] **Step 1: Write CsvSimulationService**

Write `src/BalanceSystem.Infrastructure/DataAcquisition/CsvSimulationService.cs`:

```csharp
using System.Globalization;
using BalanceSystem.Core.Interfaces;
using BalanceSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.DataAcquisition;

public class CsvSimulationService : IDataAcquisitionService, IDisposable
{
    private readonly ILogger<CsvSimulationService> _logger;
    private readonly List<VibrationData> _buffer = new();
    private readonly object _bufferLock = new();
    private CancellationTokenSource? _cts;
    private Task? _playbackTask;
    private double _currentSpeed;

    public bool IsConnected { get; private set; }
    public event EventHandler<VibrationData>? DataReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    public CsvSimulationService(ILogger<CsvSimulationService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var csvDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Simulation");
        if (!Directory.Exists(csvDir))
        {
            _logger.LogWarning("Simulation data directory not found: {Dir}", csvDir);
            SetConnected(false);
            return;
        }

        var csvFiles = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (csvFiles.Length == 0)
        {
            _logger.LogWarning("No CSV files found in {Dir}", csvDir);
            SetConnected(false);
            return;
        }

        _logger.LogInformation("Starting CSV simulation with {Count} file(s)", csvFiles.Length);
        SetConnected(true);

        _playbackTask = Task.Run(async () =>
        {
            try
            {
                foreach (var file in csvFiles)
                {
                    _logger.LogInformation("Playing back: {File}", Path.GetFileName(file));
                    await PlaybackCsvFile(file, _cts.Token);
                    if (_cts.Token.IsCancellationRequested) break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CSV playback");
            }
        }, _cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }
        if (_playbackTask != null)
        {
            try { await _playbackTask; } catch { }
            _playbackTask = null;
        }
        SetConnected(false);
    }

    public (double LeftAmplitude, double RightAmplitude, double LeftPhase, double RightPhase, double Speed)
        GetCurrentValues()
    {
        VibrationData[] recent;
        lock (_bufferLock)
        {
            recent = _buffer.TakeLast(4096).ToArray();
        }

        if (recent.Length == 0)
            return (0, 0, 0, 0, 0);

        double leftRms = Math.Sqrt(recent.Average(d => d.LeftChannel * d.LeftChannel));
        double rightRms = Math.Sqrt(recent.Average(d => d.RightChannel * d.RightChannel));
        double avgSpeed = recent.Average(d => d.Speed);

        double leftPhase = EstimatePhase(recent.Select(d => d.LeftChannel).ToArray(), avgSpeed);
        double rightPhase = EstimatePhase(recent.Select(d => d.RightChannel).ToArray(), avgSpeed);

        return (leftRms, rightRms, leftPhase, rightPhase, avgSpeed);
    }

    public VibrationData[] GetWaveformData(int windowSeconds = 5)
    {
        int sampleCount = (int)(6400 * windowSeconds);
        lock (_bufferLock)
        {
            return _buffer.TakeLast(sampleCount).ToArray();
        }
    }

    private async Task PlaybackCsvFile(string filePath, CancellationToken ct)
    {
        var lines = await File.ReadAllLinesAsync(filePath, ct);
        int startLine = lines.Length > 0
            && (lines[0].StartsWith("Timestamp") || lines[0].StartsWith("时间戳")) ? 1 : 0;

        int lineIndex = 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            var parts = lines[i].Split(',');
            if (parts.Length < 4) continue;

            if (!double.TryParse(parts[0], CultureInfo.InvariantCulture, out double timestamp)) continue;
            if (!double.TryParse(parts[1], CultureInfo.InvariantCulture, out double left)) continue;
            if (!double.TryParse(parts[2], CultureInfo.InvariantCulture, out double right)) continue;
            if (!double.TryParse(parts[3], CultureInfo.InvariantCulture, out double tachRaw)) continue;

            bool tachPulse = tachRaw > 2.5;
            double speed = CalculateSpeed(tachPulse);

            var data = new VibrationData(DateTime.Now, left, right, tachPulse, speed);

            lock (_bufferLock)
            {
                _buffer.Add(data);
                while (_buffer.Count > 6400 * 10)
                    _buffer.RemoveAt(0);
            }

            DataReceived?.Invoke(this, data);

            lineIndex++;
            if (lineIndex % 64 == 0)
            {
                await Task.Delay(10, ct);
            }
        }
    }

    private double _lastPulseTime;
    private double CalculateSpeed(bool tachPulse)
    {
        if (tachPulse)
        {
            double now = DateTime.Now.Ticks / 10_000_000.0;
            if (_lastPulseTime > 0)
            {
                double period = now - _lastPulseTime;
                if (period > 0.001)
                    _currentSpeed = 60.0 / period;
            }
            _lastPulseTime = now;
        }
        return _currentSpeed;
    }

    private static double EstimatePhase(double[] samples, double speed)
    {
        if (samples.Length < 100 || speed < 1) return 0;
        double freq = speed / 60.0;
        double sampleRate = 6400.0;
        double sumSin = 0, sumCos = 0;
        int n = Math.Min(samples.Length, 4096);
        for (int i = 0; i < n; i++)
        {
            double angle = 2 * Math.PI * freq * i / sampleRate;
            sumSin += samples[i] * Math.Sin(angle);
            sumCos += samples[i] * Math.Cos(angle);
        }
        double phase = Math.Atan2(sumSin, sumCos) * 180.0 / Math.PI;
        if (phase < 0) phase += 360.0;
        return phase;
    }

    private void SetConnected(bool connected)
    {
        if (IsConnected != connected)
        {
            IsConnected = connected;
            ConnectionStateChanged?.Invoke(this, connected);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build /e/Balancer/src/BalanceSystem.Infrastructure`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.Infrastructure/DataAcquisition/CsvSimulationService.cs
git commit -m "feat: implement CSV simulation service implementing IDataAcquisitionService"
```

---

### Task 10: Serilog logging configuration

**Files:**
- Create: `src/BalanceSystem.Infrastructure/Logging/LogConfig.cs`

**Interfaces:**
- Produces: `LogConfig.CreateLogger()` static method returning configured `Serilog.ILogger`

- [ ] **Step 1: Write logging configuration**

Write `src/BalanceSystem.Infrastructure/Logging/LogConfig.cs`:

```csharp
using Serilog;
using Serilog.Events;

namespace BalanceSystem.Infrastructure.Logging;

public static class LogConfig
{
    public static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "balancesystem-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.Infrastructure`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.Infrastructure/Logging/LogConfig.cs
git commit -m "feat: add Serilog logging configuration (console + rolling file)"
```

---

### Task 11: Database context and FreeSql setup

**Files:**
- Create: `src/BalanceSystem.Core/Models/User.cs`
- Create: `src/BalanceSystem.Infrastructure/Database/AppDbContext.cs`
- Create: `src/BalanceSystem.Infrastructure/Database/DatabaseInitializer.cs`

**Interfaces:**
- Produces: `AppDbContext` (FreeSql IFreeSql wrapper), `DatabaseInitializer` with sync/seed logic

- [ ] **Step 1: Write User entity**

Write `src/BalanceSystem.Core/Models/User.cs`:

```csharp
using FreeSql.DataAnnotations;

namespace BalanceSystem.Core.Models;

[Table(Name = "Users")]
public class User
{
    [Column(IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    [Column(StringLength = 50, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    [Column(StringLength = 256, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column(StringLength = 20, IsNullable = false)]
    public string Role { get; set; } = "Operator";

    public DateTime CreateTime { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 2: Write AppDbContext**

Write `src/BalanceSystem.Infrastructure/Database/AppDbContext.cs`:

```csharp
using FreeSql;
using BalanceSystem.Core.Models;

namespace BalanceSystem.Infrastructure.Database;

public class AppDbContext
{
    public IFreeSql Orm { get; }

    public AppDbContext(string connectionString)
    {
        Orm = new FreeSqlBuilder()
            .UseConnectionString(DataType.SqlServer, connectionString)
            .UseAutoSyncStructure(true)
            .UseMonitorCommand(cmd => System.Diagnostics.Debug.WriteLine(cmd.CommandText))
            .Build();
    }
}
```

- [ ] **Step 3: Write DatabaseInitializer**

Write `src/BalanceSystem.Infrastructure/Database/DatabaseInitializer.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using BalanceSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.Database;

public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing database...");
        _db.Orm.SyncStructure<User>();

        if (!_db.Orm.Select<User>().Any())
        {
            _db.Orm.Insert(new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                Role = "Admin",
                IsActive = true
            }).ExecuteAffrows();
            _logger.LogInformation("Seeded default admin user");
        }

        _logger.LogInformation("Database initialization complete");
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.Infrastructure`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/BalanceSystem.Core/Models/User.cs src/BalanceSystem.Infrastructure/Database/
git commit -m "feat: add FreeSql database context with User entity and admin seed"
```

---

### Task 12: DI container setup for App

**Files:**
- Create: `src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs`
- Modify: `src/BalanceSystem.App/App.xaml.cs`
- Create: `src/BalanceSystem.App/appsettings.json`

**Interfaces:**
- Consumes: `IDataAcquisitionService`, `CsvSimulationService`, `AppDbContext`, `DatabaseInitializer`, `LogConfig`
- Produces: Full DI service provider available via `App.Services`

- [ ] **Step 1: Write appsettings.json**

Write `src/BalanceSystem.App/appsettings.json`:

```json
{
  "AppSettings": {
    "Mode": "Simulation",
    "ConnectionString": "Server=localhost;Database=BalanceSystem;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

- [ ] **Step 2: Write DI extensions**

Write `src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
using BalanceSystem.Core.Interfaces;
using BalanceSystem.Infrastructure.DataAcquisition;
using BalanceSystem.Infrastructure.Database;
using BalanceSystem.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BalanceSystem.App.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBalanceSystemServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var logger = LogConfig.CreateLogger();
        services.AddLogging(builder => builder.AddSerilog(logger));

        var connStr = configuration["AppSettings:ConnectionString"]
            ?? "Server=localhost;Database=BalanceSystem;Trusted_Connection=true;TrustServerCertificate=true;";
        var dbContext = new AppDbContext(connStr);
        services.AddSingleton(dbContext);
        services.AddTransient<DatabaseInitializer>();

        services.AddSingleton<IDataAcquisitionService, CsvSimulationService>();

        return services;
    }
}
```

- [ ] **Step 3: Modify App.xaml.cs**

Write `src/BalanceSystem.App/App.xaml.cs`:

```csharp
using System.Windows;
using BalanceSystem.App.DependencyInjection;
using BalanceSystem.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BalanceSystem.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddBalanceSystemServices(configuration);

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var initializer = Services.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize();
    }
}
```

- [ ] **Step 4: Add Microsoft.Extensions.Configuration.Json package to App**

Run:
```bash
dotnet add /e/Balancer/src/BalanceSystem.App package Microsoft.Extensions.Configuration.Json
```

- [ ] **Step 5: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/BalanceSystem.App/
git commit -m "feat: configure DI container with database, logging, and simulation services"
```

---

### Task 13: Balance system business service with stability detection

**Files:**
- Create: `src/BalanceSystem.Core/Services/IBalancingTestService.cs`
- Create: `src/BalanceSystem.Core/Services/BalancingTestService.cs`
- Create: `src/BalanceSystem.Core/Models/TestStep.cs`

**Interfaces:**
- Consumes: `IDataAcquisitionService`, `InfluenceCoefficientSolver`, `FftCalculator`
- Produces: `IBalancingTestService` — `StartTest()`, `RecordStep()`, `AdvanceStep()`, `GetStabilityStatus()`, `GetResult()`, events for step changes

- [ ] **Step 1: Write TestStep enum**

Write `src/BalanceSystem.Core/Models/TestStep.cs`:

```csharp
namespace BalanceSystem.Core.Models;

public enum TestStep
{
    Idle,
    InitialRun,
    LeftTrial,
    RightTrial,
    Calculation,
    Retest,
    Completed
}
```

- [ ] **Step 2: Write IBalancingTestService interface**

Write `src/BalanceSystem.Core/Services/IBalancingTestService.cs`:

```csharp
using BalanceSystem.Core.Models;

namespace BalanceSystem.Core.Services;

public interface IBalancingTestService
{
    TestStep CurrentStep { get; }
    bool IsStable { get; }
    BalancingResult? Result { get; }
    event EventHandler<TestStep>? StepChanged;
    event EventHandler<bool>? StabilityChanged;

    void StartTest(double targetSpeed);
    void RecordCurrentValues();
    void AdvanceStep();
    void Reset();
    StabilityInfo GetStabilityInfo();
}

public record StabilityInfo(
    bool IsStable,
    double AmplitudeVariation,
    double PhaseVariation,
    double SpeedDeviation
);
```

- [ ] **Step 3: Write BalancingTestService**

Write `src/BalanceSystem.Core/Services/BalancingTestService.cs`:

```csharp
using BalanceSystem.Core.Algorithms;
using BalanceSystem.Core.Interfaces;
using BalanceSystem.Core.Models;
using BalanceSystem.Shared;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Core.Services;

public class BalancingTestService : IBalancingTestService
{
    private readonly IDataAcquisitionService _dataAcquisition;
    private readonly ILogger<BalancingTestService> _logger;
    private readonly InfluenceCoefficientSolver _solver = new();
    private readonly Queue<StabilitySample> _stabilityWindow = new();
    private double _targetSpeed;

    public TestStep CurrentStep { get; private set; } = TestStep.Idle;
    public bool IsStable { get; private set; }
    public BalancingResult? Result { get; private set; }
    public event EventHandler<TestStep>? StepChanged;
    public event EventHandler<bool>? StabilityChanged;

    public BalancingTestService(IDataAcquisitionService dataAcquisition,
                                 ILogger<BalancingTestService> logger)
    {
        _dataAcquisition = dataAcquisition;
        _logger = logger;
        _dataAcquisition.DataReceived += OnDataReceived;
    }

    public void StartTest(double targetSpeed)
    {
        _targetSpeed = targetSpeed;
        CurrentStep = TestStep.InitialRun;
        _stabilityWindow.Clear();
        IsStable = false;
        Result = null;
        _logger.LogInformation("Balancing test started at {Speed} RPM", targetSpeed);
        StepChanged?.Invoke(this, CurrentStep);
    }

    public void RecordCurrentValues()
    {
        var waveform = _dataAcquisition.GetWaveformData(1);
        if (waveform.Length < 100) return;

        double speed = waveform.Average(d => d.Speed);
        var leftSignal = waveform.Select(d => d.LeftChannel).ToArray();
        var rightSignal = waveform.Select(d => d.RightChannel).ToArray();

        var (leftAmp, leftPhase) = FftCalculator.ExtractFundamental(leftSignal, 6400.0, speed);
        var (rightAmp, rightPhase) = FftCalculator.ExtractFundamental(rightSignal, 6400.0, speed);

        switch (CurrentStep)
        {
            case TestStep.InitialRun:
                _solver.AddInitialRun(leftAmp, leftPhase, rightAmp, rightPhase);
                _logger.LogInformation("Initial run recorded: L={LA:F2} angle {LP:F1}, R={RA:F2} angle {RP:F1}",
                    leftAmp, leftPhase, rightAmp, rightPhase);
                break;
            case TestStep.LeftTrial:
                _solver.AddLeftTrialRun(leftAmp, leftPhase, rightAmp, rightPhase,
                    trialMass: 50, trialAngle: 0);
                _logger.LogInformation("Left trial recorded");
                break;
            case TestStep.RightTrial:
                _solver.AddRightTrialRun(leftAmp, leftPhase, rightAmp, rightPhase,
                    trialMass: 50, trialAngle: 0);
                _logger.LogInformation("Right trial recorded");
                break;
            case TestStep.Retest:
                _logger.LogInformation("Retest recorded: L={LA:F2} angle {LP:F1}, R={RA:F2} angle {RP:F1}",
                    leftAmp, leftPhase, rightAmp, rightPhase);
                break;
        }
    }

    public void AdvanceStep()
    {
        CurrentStep = CurrentStep switch
        {
            TestStep.Idle => TestStep.InitialRun,
            TestStep.InitialRun => TestStep.LeftTrial,
            TestStep.LeftTrial => TestStep.RightTrial,
            TestStep.RightTrial => TestStep.Calculation,
            TestStep.Calculation => TestStep.Completed,
            TestStep.Completed => TestStep.Retest,
            TestStep.Retest => TestStep.Completed,
            _ => TestStep.Idle
        };

        if (CurrentStep == TestStep.Calculation)
        {
            Result = _solver.Solve();
            _logger.LogInformation(
                "Calculation complete: Left={LM:F1}g angle {LA:F1}, Right={RM:F1}g angle {RA:F1}, Balanced={B}",
                Result.LeftMass, Result.LeftAngle, Result.RightMass, Result.RightAngle, Result.IsBalanced);
        }

        StepChanged?.Invoke(this, CurrentStep);
    }

    public void Reset()
    {
        CurrentStep = TestStep.Idle;
        IsStable = false;
        Result = null;
        _stabilityWindow.Clear();
        StepChanged?.Invoke(this, CurrentStep);
    }

    public StabilityInfo GetStabilityInfo()
    {
        if (_stabilityWindow.Count < 10)
            return new StabilityInfo(false, 999, 999, 999);

        var samples = _stabilityWindow.ToArray();
        double ampVar = StdDev(samples.Select(s => s.Amplitude).ToArray())
            / (samples.Average(s => s.Amplitude) + 0.001);
        double phaseVar = StdDev(samples.Select(s => s.Phase).ToArray());
        double speedDev = Math.Abs(samples.Average(s => s.Speed) - _targetSpeed)
            / (_targetSpeed + 0.001) * 100;

        return new StabilityInfo(IsStable, ampVar, phaseVar, speedDev);
    }

    private void OnDataReceived(object? sender, VibrationData data)
    {
        _stabilityWindow.Enqueue(new StabilitySample(data.Speed, data.LeftChannel,
            data.LeftChannel));
        while (_stabilityWindow.Count > 100)
            _stabilityWindow.Dequeue();

        if (_stabilityWindow.Count >= 50)
        {
            var samples = _stabilityWindow.ToArray();
            double ampVar = StdDev(samples.Select(s => s.Amplitude).ToArray())
                / (samples.Average(s => s.Amplitude) + 0.001);
            double phaseVar = StdDev(samples.Select(s => s.Phase).ToArray());
            double speedDev = Math.Abs(samples.Average(s => s.Speed) - _targetSpeed)
                / (_targetSpeed + 0.001) * 100;

            bool wasStable = IsStable;
            IsStable = ampVar < Constants.AmplitudeStabilityThreshold
                    && phaseVar < Constants.PhaseStabilityThreshold
                    && speedDev < Constants.SpeedStabilityPercent;

            if (wasStable != IsStable)
                StabilityChanged?.Invoke(this, IsStable);
        }
    }

    private static double StdDev(double[] values)
    {
        if (values.Length == 0) return 0;
        double avg = values.Average();
        return Math.Sqrt(values.Average(v => (v - avg) * (v - avg)));
    }

    private record struct StabilitySample(double Speed, double Amplitude, double Phase);
}
```

- [ ] **Step 4: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.Core`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/BalanceSystem.Core/Services/ src/BalanceSystem.Core/Models/TestStep.cs
git commit -m "feat: add BalancingTestService with 4-step state machine and stability detection"
```

---

### Task 14: Custom NumericDisplay control

**Files:**
- Create: `src/BalanceSystem.App/Controls/NumericDisplay.cs`

**Interfaces:**
- Produces: `NumericDisplay` — a `FrameworkElement` with `Label`, `Value`, `Unit`, `StatusColor`, `Format` dependency properties, rendered via `DrawingVisual`

- [ ] **Step 1: Write NumericDisplay control**

Write `src/BalanceSystem.App/Controls/NumericDisplay.cs`:

```csharp
using System.Windows;
using System.Windows.Media;

namespace BalanceSystem.App.Controls;

public class NumericDisplay : FrameworkElement
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(NumericDisplay),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericDisplay),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(NumericDisplay),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StatusColorProperty =
        DependencyProperty.Register(nameof(StatusColor), typeof(Color), typeof(NumericDisplay),
            new FrameworkPropertyMetadata(Color.FromRgb(0x33, 0x33, 0x33),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register(nameof(Format), typeof(string), typeof(NumericDisplay),
            new FrameworkPropertyMetadata("F2", FrameworkPropertyMetadataOptions.AffectsRender));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public Color StatusColor { get => (Color)GetValue(StatusColorProperty); set => SetValue(StatusColorProperty, value); }
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double width = ActualWidth;
        double height = ActualHeight;
        if (width < 10 || height < 10) return;

        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        var labelText = new FormattedText(Label,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei"), 12, Brushes.Gray, 1.0);
        dc.DrawText(labelText, new Point((width - labelText.Width) / 2, 4));

        string valueStr = Value.ToString(Format);
        var valueText = new FormattedText(valueStr,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Consolas"), 28, new SolidColorBrush(StatusColor), 1.0);
        dc.DrawText(valueText, new Point((width - valueText.Width) / 2, height / 2 - 16));

        var unitText = new FormattedText(Unit,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei"), 11, Brushes.Gray, 1.0);
        dc.DrawText(unitText, new Point((width - unitText.Width) / 2, height - 22));
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/Controls/NumericDisplay.cs
git commit -m "feat: add NumericDisplay custom control with DrawingVisual rendering"
```

---

### Task 15: Custom WaveformControl

**Files:**
- Create: `src/BalanceSystem.App/Controls/WaveformControl.cs`

**Interfaces:**
- Produces: `WaveformControl` — renders dual-channel scrolling waveform using `WriteableBitmap`, has `PushData(VibrationData)` method

- [ ] **Step 1: Write WaveformControl**

Write `src/BalanceSystem.App/Controls/WaveformControl.cs`:

```csharp
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BalanceSystem.Core.Models;

namespace BalanceSystem.App.Controls;

public class WaveformControl : FrameworkElement
{
    private WriteableBitmap? _bitmap;
    private readonly Queue<double> _channel1Data = new();
    private readonly Queue<double> _channel2Data = new();
    private const int MaxPoints = 6400 * 5;
    private readonly object _dataLock = new();

    public static readonly DependencyProperty Channel1ColorProperty =
        DependencyProperty.Register(nameof(Channel1Color), typeof(Color), typeof(WaveformControl),
            new FrameworkPropertyMetadata(Color.FromRgb(0x2C, 0x5A, 0xA0)));

    public static readonly DependencyProperty Channel2ColorProperty =
        DependencyProperty.Register(nameof(Channel2Color), typeof(Color), typeof(WaveformControl),
            new FrameworkPropertyMetadata(Color.FromRgb(0x28, 0xA7, 0x45)));

    public Color Channel1Color
    {
        get => (Color)GetValue(Channel1ColorProperty);
        set => SetValue(Channel1ColorProperty, value);
    }
    public Color Channel2Color
    {
        get => (Color)GetValue(Channel2ColorProperty);
        set => SetValue(Channel2ColorProperty, value);
    }

    public void PushData(VibrationData data)
    {
        lock (_dataLock)
        {
            _channel1Data.Enqueue(data.LeftChannel);
            _channel2Data.Enqueue(data.RightChannel);
            while (_channel1Data.Count > MaxPoints) _channel1Data.Dequeue();
            while (_channel2Data.Count > MaxPoints) _channel2Data.Dequeue();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        int width = (int)ActualWidth;
        int height = (int)ActualHeight;
        if (width < 10 || height < 10) return;

        if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        RenderWaveform();
        dc.DrawImage(_bitmap, new Rect(0, 0, width, height));
    }

    private void RenderWaveform()
    {
        if (_bitmap == null) return;
        int width = _bitmap.PixelWidth;
        int height = _bitmap.PixelHeight;
        int halfHeight = height / 2;

        _bitmap.Lock();
        try
        {
            IntPtr backBuffer = _bitmap.BackBuffer;
            int stride = _bitmap.BackBufferStride;
            int bufferSize = stride * height;

            unsafe
            {
                byte* p = (byte*)backBuffer;
                for (int i = 0; i < bufferSize; i++) p[i] = 255;
            }

            double[] ch1, ch2;
            lock (_dataLock)
            {
                ch1 = _channel1Data.ToArray();
                ch2 = _channel2Data.ToArray();
            }
            if (ch1.Length < 2) return;

            double maxVal = 1.0;
            if (ch1.Length > 0)
                maxVal = Math.Max(ch1.Max(v => Math.Abs(v)), ch2.Max(v => Math.Abs(v)));
            if (maxVal < 0.001) maxVal = 1.0;

            double scale = (halfHeight - 10) / maxVal;
            double xStep = (double)width / Math.Max(ch1.Length, width);

            DrawChannel(ch1, scale, xStep, 0, halfHeight - 5, Channel1Color,
                _bitmap, stride, backBuffer);
            DrawChannel(ch2, scale, xStep, halfHeight + 5, height - 5, Channel2Color,
                _bitmap, stride, backBuffer);

            unsafe
            {
                byte* p = (byte*)backBuffer + halfHeight * stride;
                for (int x = 0; x < width; x++)
                {
                    int offset = x * 4;
                    p[offset] = 200;
                    p[offset + 1] = 200;
                    p[offset + 2] = 200;
                }
            }

            _bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            _bitmap.Unlock();
        }
    }

    private unsafe void DrawChannel(double[] data, double scale, double xStep,
        int yMin, int yMax, Color color, WriteableBitmap bitmap, int stride, IntPtr backBuffer)
    {
        int midY = (yMin + yMax) / 2;
        int width = bitmap.PixelWidth;
        byte r = color.R, g = color.G, b = color.B;
        int step = Math.Max(1, data.Length / width);

        int prevY = midY;
        for (int x = 0; x < width; x++)
        {
            int dataIdx = Math.Min(x * step, data.Length - 1);
            int y = midY - (int)(data[dataIdx] * scale);
            y = Math.Clamp(y, yMin, yMax);

            int startY = Math.Min(prevY, y);
            int endY = Math.Max(prevY, y);
            for (int py = startY; py <= endY; py++)
            {
                byte* pixel = (byte*)backBuffer + py * stride + x * 4;
                pixel[0] = b;
                pixel[1] = g;
                pixel[2] = r;
            }
            prevY = y;
        }
    }
}
```

- [ ] **Step 2: Ensure AllowUnsafeBlocks in App csproj**

Edit `src/BalanceSystem.App/BalanceSystem.App.csproj` and add inside `<PropertyGroup>`:
```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

- [ ] **Step 3: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.App/Controls/WaveformControl.cs
git commit -m "feat: add WaveformControl with dual-channel WriteableBitmap rendering"
```

---

### Task 16: Custom PolarPlotControl

**Files:**
- Create: `src/BalanceSystem.App/Controls/PolarPlotControl.cs`

**Interfaces:**
- Produces: `PolarPlotControl` — renders polar coordinate plot with grid, angle labels, and two vectors (left/right) using `DrawingVisual`

- [ ] **Step 1: Write PolarPlotControl**

Write `src/BalanceSystem.App/Controls/PolarPlotControl.cs`:

```csharp
using System.Windows;
using System.Windows.Media;

namespace BalanceSystem.App.Controls;

public class PolarPlotControl : FrameworkElement
{
    private readonly VisualCollection _visuals;
    private DrawingVisual? _gridVisual;
    private DrawingVisual? _vectorVisual;

    public static readonly DependencyProperty LeftVectorAngleProperty =
        DependencyProperty.Register(nameof(LeftVectorAngle), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public static readonly DependencyProperty LeftVectorMagnitudeProperty =
        DependencyProperty.Register(nameof(LeftVectorMagnitude), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public static readonly DependencyProperty RightVectorAngleProperty =
        DependencyProperty.Register(nameof(RightVectorAngle), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public static readonly DependencyProperty RightVectorMagnitudeProperty =
        DependencyProperty.Register(nameof(RightVectorMagnitude), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public double LeftVectorAngle { get => (double)GetValue(LeftVectorAngleProperty); set => SetValue(LeftVectorAngleProperty, value); }
    public double LeftVectorMagnitude { get => (double)GetValue(LeftVectorMagnitudeProperty); set => SetValue(LeftVectorMagnitudeProperty, value); }
    public double RightVectorAngle { get => (double)GetValue(RightVectorAngleProperty); set => SetValue(RightVectorAngleProperty, value); }
    public double RightVectorMagnitude { get => (double)GetValue(RightVectorMagnitudeProperty); set => SetValue(RightVectorMagnitudeProperty, value); }

    private static void OnVectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PolarPlotControl)d).RenderVectors();
    }

    public PolarPlotControl()
    {
        _visuals = new VisualCollection(this);
        RenderGrid();
        RenderVectors();
    }

    protected override Visual GetVisualChild(int index) => _visuals[index];
    protected override int VisualChildrenCount => _visuals.Count;

    private void RenderGrid()
    {
        _gridVisual = new DrawingVisual();
        using var dc = _gridVisual.RenderOpen();
        double width = ActualWidth > 0 ? ActualWidth : 300;
        double height = ActualHeight > 0 ? ActualHeight : 300;
        double cx = width / 2, cy = height / 2;
        double radius = Math.Min(cx, cy) - 30;

        var grayPen = new Pen(Brushes.LightGray, 0.5);
        var axisPen = new Pen(Brushes.Gray, 1);

        for (int i = 1; i <= 4; i++)
        {
            double r = radius * i / 4;
            dc.DrawEllipse(null, grayPen, new Point(cx, cy), r, r);
        }

        for (int angle = 0; angle < 360; angle += 30)
        {
            double rad = angle * Math.PI / 180.0;
            dc.DrawLine(grayPen, new Point(cx, cy),
                new Point(cx + radius * Math.Cos(rad), cy - radius * Math.Sin(rad)));
        }

        dc.DrawLine(axisPen, new Point(cx - radius, cy), new Point(cx + radius, cy));
        dc.DrawLine(axisPen, new Point(cx, cy - radius), new Point(cx, cy + radius));

        var typeface = new Typeface("Microsoft YaHei");
        for (int angle = 0; angle < 360; angle += 30)
        {
            double rad = angle * Math.PI / 180.0;
            double lx = cx + (radius + 15) * Math.Cos(rad);
            double ly = cy - (radius + 15) * Math.Sin(rad);
            var text = new FormattedText($"{angle}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, 9, Brushes.Gray, 1.0);
            dc.DrawText(text, new Point(lx - text.Width / 2, ly - text.Height / 2));
        }

        dc.Close();
        _visuals.Clear();
        _visuals.Add(_gridVisual);
        if (_vectorVisual != null) _visuals.Add(_vectorVisual);
    }

    private void RenderVectors()
    {
        _vectorVisual = new DrawingVisual();
        using var dc = _vectorVisual.RenderOpen();
        double width = ActualWidth > 0 ? ActualWidth : 300;
        double height = ActualHeight > 0 ? ActualHeight : 300;
        double cx = width / 2, cy = height / 2;
        double radius = Math.Min(cx, cy) - 30;

        DrawVectorLine(dc, cx, cy, radius, LeftVectorMagnitude, LeftVectorAngle,
            Color.FromRgb(0x2C, 0x5A, 0xA0), "L");
        DrawVectorLine(dc, cx, cy, radius, RightVectorMagnitude, RightVectorAngle,
            Color.FromRgb(0x28, 0xA7, 0x45), "R");

        dc.Close();

        if (_visuals.Count >= 2)
            _visuals[1] = _vectorVisual;
        else
            _visuals.Add(_vectorVisual);
    }

    private static void DrawVectorLine(DrawingContext dc, double cx, double cy,
        double maxRadius, double magnitude, double angleDeg, Color color, string label)
    {
        double angleRad = angleDeg * Math.PI / 180.0;
        double scaledRadius = magnitude * maxRadius / 100.0;
        if (scaledRadius > maxRadius) scaledRadius = maxRadius;
        if (scaledRadius < 2) scaledRadius = 2;

        double ex = cx + scaledRadius * Math.Cos(angleRad);
        double ey = cy - scaledRadius * Math.Sin(angleRad);

        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, 2.5);
        dc.DrawLine(pen, new Point(cx, cy), new Point(ex, ey));

        double arrowLen = 8;
        double arrowAngle1 = angleRad + Math.PI - 0.4;
        double arrowAngle2 = angleRad + Math.PI + 0.4;
        var arrowPen = new Pen(brush, 1.5);
        dc.DrawLine(arrowPen, new Point(ex, ey),
            new Point(ex + arrowLen * Math.Cos(arrowAngle1), ey - arrowLen * Math.Sin(arrowAngle1)));
        dc.DrawLine(arrowPen, new Point(ex, ey),
            new Point(ex + arrowLen * Math.Cos(arrowAngle2), ey - arrowLen * Math.Sin(arrowAngle2)));
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RenderGrid();
        RenderVectors();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/Controls/PolarPlotControl.cs
git commit -m "feat: add PolarPlotControl with vector rendering and angle grid"
```

---

### Task 17: MonitoringViewModel

**Files:**
- Create: `src/BalanceSystem.App/ViewModels/MonitoringViewModel.cs`

**Interfaces:**
- Consumes: `IDataAcquisitionService`
- Produces: `MonitoringViewModel` — ObservableObject with Speed, LeftAmplitude, RightAmplitude, LeftPhase, RightPhase, IsConnected, start/stop commands

- [ ] **Step 1: Write MonitoringViewModel**

Write `src/BalanceSystem.App/ViewModels/MonitoringViewModel.cs`:

```csharp
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
```

- [ ] **Step 2: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/ViewModels/MonitoringViewModel.cs
git commit -m "feat: add MonitoringViewModel with data binding and acquisition commands"
```

---

### Task 18: BalancingTestViewModel

**Files:**
- Create: `src/BalanceSystem.App/ViewModels/BalancingTestViewModel.cs`

**Interfaces:**
- Consumes: `IBalancingTestService`, `IDataAcquisitionService`
- Produces: `BalancingTestViewModel` — ObservableObject with step navigation, result display, stability info

- [ ] **Step 1: Write BalancingTestViewModel**

Write `src/BalanceSystem.App/ViewModels/BalancingTestViewModel.cs`:

```csharp
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
```

- [ ] **Step 2: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/ViewModels/BalancingTestViewModel.cs
git commit -m "feat: add BalancingTestViewModel with step wizard and relay commands"
```

---

### Task 19: Value converters

**Files:**
- Create: `src/BalanceSystem.App/Converters/BoolToVisibilityConverter.cs`
- Create: `src/BalanceSystem.App/Converters/BoolToPassFailConverter.cs`

**Interfaces:**
- Produces: `BoolToVisibilityConverter` (true→Visible, false→Collapsed), `BoolToPassFailConverter` (true→"PASS", false→"FAIL")

- [ ] **Step 1: Write converters**

Write `src/BalanceSystem.App/Converters/BoolToVisibilityConverter.cs`:

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BalanceSystem.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
```

Write `src/BalanceSystem.App/Converters/BoolToPassFailConverter.cs`:

```csharp
using System.Globalization;
using System.Windows.Data;

namespace BalanceSystem.App.Converters;

public class BoolToPassFailConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "PASS" : "FAIL";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

- [ ] **Step 2: Register in App.xaml**

Edit `src/BalanceSystem.App/App.xaml`:

```xml
<Application x:Class="BalanceSystem.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:converters="clr-namespace:BalanceSystem.App.Converters">
    <Application.Resources>
        <converters:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
        <converters:BoolToPassFailConverter x:Key="BoolToPassFail"/>
    </Application.Resources>
</Application>
```

- [ ] **Step 3: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.App/Converters/ src/BalanceSystem.App/App.xaml
git commit -m "feat: add value converters for Bool-Visibility and Bool-PassFail"
```

---

### Task 20: MonitoringView

**Files:**
- Create: `src/BalanceSystem.App/Views/MonitoringView.xaml`
- Create: `src/BalanceSystem.App/Views/MonitoringView.xaml.cs`

**Interfaces:**
- Consumes: `MonitoringViewModel`
- Produces: View with waveform area, numeric displays grid, polar plot, status bar, and speed selector

- [ ] **Step 1: Write MonitoringView.xaml**

Write `src/BalanceSystem.App/Views/MonitoringView.xaml`:

```xml
<UserControl x:Class="BalanceSystem.App.Views.MonitoringView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:BalanceSystem.App.Controls"
             xmlns:vm="clr-namespace:BalanceSystem.App.ViewModels"
             d:DataContext="{d:DesignInstance Type=vm:MonitoringViewModel}">
    <Grid Background="White">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <Grid Grid.Row="0" Margin="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Waveform -->
            <Border Grid.Column="0" BorderBrush="#E0E0E0" BorderThickness="1" Margin="0,0,10,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="Left/Right Waveform" FontSize="13"
                               FontWeight="SemiBold" Foreground="#333333" Margin="8,6"/>
                    <controls:WaveformControl x:Name="Waveform" Grid.Row="1"/>
                </Grid>
            </Border>

            <GridSplitter Grid.Column="1" Width="4" HorizontalAlignment="Center"
                          VerticalAlignment="Stretch" Background="#E0E0E0"/>

            <!-- Numeric + Polar -->
            <Grid Grid.Column="2">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <WrapPanel Grid.Row="0" Margin="0,0,0,10">
                    <controls:NumericDisplay Label="Speed" Value="{Binding Speed}"
                        Unit="RPM" Width="110" Height="70" Margin="4" StatusColor="#2C5AA0"/>
                    <controls:NumericDisplay Label="Left Amp" Value="{Binding LeftAmplitude}"
                        Unit="mm/s" Width="110" Height="70" Margin="4" StatusColor="#333333"/>
                    <controls:NumericDisplay Label="Right Amp" Value="{Binding RightAmplitude}"
                        Unit="mm/s" Width="110" Height="70" Margin="4" StatusColor="#333333"/>
                    <controls:NumericDisplay Label="Left Phase" Value="{Binding LeftPhase}"
                        Unit="deg" Width="110" Height="70" Margin="4" StatusColor="#333333"/>
                    <controls:NumericDisplay Label="Right Phase" Value="{Binding RightPhase}"
                        Unit="deg" Width="110" Height="70" Margin="4" StatusColor="#333333"/>
                </WrapPanel>

                <Border Grid.Row="1" BorderBrush="#E0E0E0" BorderThickness="1">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" Text="Polar Plot" FontSize="13"
                                   FontWeight="SemiBold" Foreground="#333333" Margin="8,6"/>
                        <controls:PolarPlotControl x:Name="PolarPlot" Grid.Row="1"/>
                    </Grid>
                </Border>
            </Grid>
        </Grid>

        <!-- Status bar + Controls -->
        <Border Grid.Row="1" Background="#F5F5F5" Padding="10,6" Margin="10,0,10,10">
            <StackPanel Orientation="Horizontal">
                <Ellipse Width="10" Height="10" Margin="0,0,4,0">
                    <Ellipse.Style>
                        <Style TargetType="Ellipse">
                            <Setter Property="Fill" Value="Red"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding IsConnected}" Value="True">
                                    <Setter Property="Fill" Value="Green"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Ellipse.Style>
                </Ellipse>
                <TextBlock Text="{Binding ConnectionStatusText}" FontSize="12"
                           Foreground="#666666" Margin="0,0,20,0"/>
                <TextBlock Text="Speed:" FontSize="12" Foreground="#666666" Margin="10,0,5,0"/>
                <ComboBox ItemsSource="{Binding SpeedOptions}"
                          SelectedIndex="{Binding SelectedSpeedIndex}"
                          Width="90" Height="24" FontSize="12"/>
                <Button Content="Start Sim" Width="70" Height="24" Margin="10,0,4,0"
                        Background="#2C5AA0" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding StartAcquisitionCommand}"/>
                <Button Content="Stop" Width="50" Height="24" Margin="4,0,0,0"
                        Background="#DC3545" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding StopAcquisitionCommand}"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Write code-behind**

Write `src/BalanceSystem.App/Views/MonitoringView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace BalanceSystem.App.Views;

public partial class MonitoringView : UserControl
{
    public MonitoringView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.App/Views/MonitoringView.xaml src/BalanceSystem.App/Views/MonitoringView.xaml.cs
git commit -m "feat: add MonitoringView with waveform, numeric displays, and polar plot"
```

---

### Task 21: BalancingTestView

**Files:**
- Create: `src/BalanceSystem.App/Views/BalancingTestView.xaml`
- Create: `src/BalanceSystem.App/Views/BalancingTestView.xaml.cs`

**Interfaces:**
- Consumes: `BalancingTestViewModel`
- Produces: Step-by-step balancing test wizard UI with result display

- [ ] **Step 1: Write BalancingTestView.xaml**

Write `src/BalanceSystem.App/Views/BalancingTestView.xaml`:

```xml
<UserControl x:Class="BalanceSystem.App.Views.BalancingTestView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:BalanceSystem.App.ViewModels"
             d:DataContext="{d:DesignInstance Type=vm:BalancingTestViewModel}">
    <Grid Background="White" Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Step indicator -->
        <Border Grid.Row="0" Background="#E8F0FE" Padding="16" CornerRadius="4">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0">
                    <TextBlock Text="{Binding StepDescription}" FontSize="14" FontWeight="SemiBold"
                               Foreground="#2C5AA0" TextWrapping="Wrap"/>
                    <TextBlock Text="{Binding StabilityText}" FontSize="12" Foreground="#666666" Margin="0,4,0,0"/>
                </StackPanel>
                <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                    <Button Content="Record" Width="80" Height="32" Margin="4"
                            Background="#28A745" Foreground="White" BorderThickness="0"
                            FontSize="12" Command="{Binding RecordStepCommand}"
                            IsEnabled="{Binding CanRecord}"/>
                    <Button Content="Next" Width="70" Height="32" Margin="4"
                            Background="#2C5AA0" Foreground="White" BorderThickness="0"
                            FontSize="12" Command="{Binding AdvanceStepCommand}"
                            IsEnabled="{Binding CanAdvance}"/>
                    <Button Content="Reset" Width="60" Height="32" Margin="4"
                            Background="#DC3545" Foreground="White" BorderThickness="0"
                            FontSize="12" Command="{Binding ResetCommand}"
                            IsEnabled="{Binding CanReset}"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Step progress -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,10" HorizontalAlignment="Center">
            <TextBlock Text="1. Init" FontSize="12" Margin="6,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Foreground" Value="Gray"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding CurrentStep}" Value="InitialRun">
                                <Setter Property="Foreground" Value="#2C5AA0"/>
                                <Setter Property="FontWeight" Value="Bold"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
            <TextBlock Text=">" FontSize="12" Foreground="Gray"/>
            <TextBlock Text="2. Left Trial" FontSize="12" Margin="6,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Foreground" Value="Gray"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding CurrentStep}" Value="LeftTrial">
                                <Setter Property="Foreground" Value="#2C5AA0"/>
                                <Setter Property="FontWeight" Value="Bold"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
            <TextBlock Text=">" FontSize="12" Foreground="Gray"/>
            <TextBlock Text="3. Right Trial" FontSize="12" Margin="6,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Foreground" Value="Gray"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding CurrentStep}" Value="RightTrial">
                                <Setter Property="Foreground" Value="#2C5AA0"/>
                                <Setter Property="FontWeight" Value="Bold"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
            <TextBlock Text=">" FontSize="12" Foreground="Gray"/>
            <TextBlock Text="4. Result" FontSize="12" Margin="6,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Foreground" Value="Gray"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding CurrentStep}" Value="Calculation">
                                <Setter Property="Foreground" Value="#2C5AA0"/>
                                <Setter Property="FontWeight" Value="Bold"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </StackPanel>

        <!-- Results -->
        <Border Grid.Row="2" BorderBrush="#E0E0E0" BorderThickness="1" Margin="0,10"
                Visibility="{Binding HasResult, Converter={StaticResource BoolToVisibility}}">
            <Grid Margin="16">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <GroupBox Grid.Column="0" Header="Left Plane Correction">
                    <StackPanel>
                        <Grid Margin="0,4">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Text="Mass:" FontSize="14" Foreground="#666" VerticalAlignment="Center"/>
                            <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="10,0">
                                <TextBlock Text="{Binding LeftMass}" FontSize="20" FontWeight="Bold" Foreground="#2C5AA0"/>
                                <TextBlock Text=" g" FontSize="14" Foreground="#666" VerticalAlignment="Bottom"/>
                            </StackPanel>
                        </Grid>
                        <Grid Margin="0,8,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Text="Angle:" FontSize="14" Foreground="#666" VerticalAlignment="Center"/>
                            <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="10,0">
                                <TextBlock Text="{Binding LeftAngle}" FontSize="20" FontWeight="Bold" Foreground="#2C5AA0"/>
                                <TextBlock Text=" deg" FontSize="14" Foreground="#666" VerticalAlignment="Bottom"/>
                            </StackPanel>
                        </Grid>
                    </StackPanel>
                </GroupBox>

                <StackPanel Grid.Column="1" VerticalAlignment="Center" Margin="20,0">
                    <TextBlock Text="{Binding IsBalanced, Converter={StaticResource BoolToPassFail}}"
                               FontSize="18" FontWeight="Bold" HorizontalAlignment="Center"/>
                    <TextBlock Text="Residual:" FontSize="11" Foreground="#999"
                               HorizontalAlignment="Center" Margin="0,8,0,0"/>
                    <TextBlock FontSize="13" HorizontalAlignment="Center">
                        <Run Text="L: "/><Run Text="{Binding ResidualLeft}" FontWeight="Bold"/>
                    </TextBlock>
                    <TextBlock FontSize="13" HorizontalAlignment="Center">
                        <Run Text="R: "/><Run Text="{Binding ResidualRight}" FontWeight="Bold"/>
                    </TextBlock>
                </StackPanel>

                <GroupBox Grid.Column="2" Header="Right Plane Correction">
                    <StackPanel>
                        <Grid Margin="0,4">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Text="Mass:" FontSize="14" Foreground="#666" VerticalAlignment="Center"/>
                            <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="10,0">
                                <TextBlock Text="{Binding RightMass}" FontSize="20" FontWeight="Bold" Foreground="#28A745"/>
                                <TextBlock Text=" g" FontSize="14" Foreground="#666" VerticalAlignment="Bottom"/>
                            </StackPanel>
                        </Grid>
                        <Grid Margin="0,8,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Text="Angle:" FontSize="14" Foreground="#666" VerticalAlignment="Center"/>
                            <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="10,0">
                                <TextBlock Text="{Binding RightAngle}" FontSize="20" FontWeight="Bold" Foreground="#28A745"/>
                                <TextBlock Text=" deg" FontSize="14" Foreground="#666" VerticalAlignment="Bottom"/>
                            </StackPanel>
                        </Grid>
                    </StackPanel>
                </GroupBox>
            </Grid>
        </Border>

        <!-- Speed selector -->
        <Border Grid.Row="3" Background="#F5F5F5" Padding="10" Margin="0,10,0,0">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="Speed:" FontSize="12" Foreground="#666"
                           VerticalAlignment="Center" Margin="0,0,10,0"/>
                <ComboBox ItemsSource="{Binding SpeedOptions}"
                          SelectedIndex="{Binding SelectedSpeedIndex}"
                          Width="90" Height="24" FontSize="12"/>
                <Button Content="Start Test" Width="80" Height="24" Margin="10,0,0,0"
                        Background="#2C5AA0" Foreground="White" BorderThickness="0"
                        FontSize="12" Command="{Binding StartTestCommand}"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Write code-behind**

Write `src/BalanceSystem.App/Views/BalancingTestView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace BalanceSystem.App.Views;

public partial class BalancingTestView : UserControl
{
    public BalancingTestView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/BalanceSystem.App/Views/BalancingTestView.xaml src/BalanceSystem.App/Views/BalancingTestView.xaml.cs
git commit -m "feat: add BalancingTestView with step wizard and results display"
```

---

### Task 22: MainViewModel and MainWindow

**Files:**
- Create: `src/BalanceSystem.App/ViewModels/MainViewModel.cs`
- Modify: `src/BalanceSystem.App/Views/MainWindow.xaml`
- Modify: `src/BalanceSystem.App/Views/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `MonitoringViewModel`, `BalancingTestViewModel`, `IDataAcquisitionService`
- Produces: Main application window with tab navigation, header bar, and status bar

- [ ] **Step 1: Write MainViewModel**

Write `src/BalanceSystem.App/ViewModels/MainViewModel.cs`:

```csharp
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

    public MainViewModel(MonitoringViewModel monitoring, BalancingTestViewModel balancingTest)
    {
        Monitoring = monitoring;
        BalancingTest = balancingTest;
    }
}
```

- [ ] **Step 2: Write MainWindow.xaml**

Write `src/BalanceSystem.App/Views/MainWindow.xaml`:

```xml
<Window x:Class="BalanceSystem.App.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="clr-namespace:BalanceSystem.App.Views"
        xmlns:vm="clr-namespace:BalanceSystem.App.ViewModels"
        Title="Balance System" Height="800" Width="1280"
        MinHeight="600" MinWidth="960"
        WindowStartupLocation="CenterScreen" Background="White"
        d:DataContext="{d:DesignInstance Type=vm:MainViewModel}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#2C5AA0" Padding="12,8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal">
                    <TextBlock Text="Balance System" FontSize="16" FontWeight="Bold"
                               Foreground="White" VerticalAlignment="Center"/>
                </StackPanel>
                <StackPanel Grid.Column="2" Orientation="Horizontal">
                    <TextBlock Text="{Binding CurrentUser}" FontSize="12" Foreground="#E8F0FE"
                               VerticalAlignment="Center" Margin="0,0,15,0"/>
                    <Ellipse Width="12" Height="12" Margin="0,0,5,0">
                        <Ellipse.Style>
                            <Style TargetType="Ellipse">
                                <Setter Property="Fill" Value="#28A745"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding HasAlarm}" Value="True">
                                        <Setter Property="Fill" Value="#DC3545"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </Ellipse.Style>
                    </Ellipse>
                    <TextBlock Text="Alarm" FontSize="12" Foreground="#E8F0FE" VerticalAlignment="Center"/>
                    <Button Content="X" Width="32" Height="32" Margin="20,0,0,0"
                            Background="Transparent" Foreground="White" BorderThickness="0"
                            FontSize="16" Click="ExitButton_Click"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Content -->
        <Grid Grid.Row="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Border Grid.Row="0" Background="#F8F9FA" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
                <TabControl SelectedIndex="{Binding SelectedTabIndex}" Background="Transparent" BorderThickness="0">
                    <TabItem Header="Monitor"/>
                    <TabItem Header="Balance Test"/>
                </TabControl>
            </Border>
            <Grid Grid.Row="1">
                <views:MonitoringView DataContext="{Binding Monitoring}"
                    Visibility="{Binding SelectedTabIndex, Converter={StaticResource TabToVisibility}, ConverterParameter=0}"/>
                <views:BalancingTestView DataContext="{Binding BalancingTest}"
                    Visibility="{Binding SelectedTabIndex, Converter={StaticResource TabToVisibility}, ConverterParameter=1}"/>
            </Grid>
        </Grid>

        <!-- Status bar -->
        <Border Grid.Row="2" Background="#F5F5F5" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0" Padding="12,4">
            <StackPanel Orientation="Horizontal">
                <TextBlock FontSize="11" Foreground="#666">
                    <Run Text="Speed: "/>
                    <Run Text="{Binding Monitoring.Speed, StringFormat={}{0:F0}}"/>
                    <Run Text=" RPM | "/>
                </TextBlock>
                <TextBlock FontSize="11" Foreground="#666">
                    <Run Text="Comm: "/>
                    <Run Text="{Binding Monitoring.ConnectionStatusText}"/>
                    <Run Text=" | "/>
                </TextBlock>
                <TextBlock FontSize="11" Foreground="#666">
                    <Run Text="User: "/>
                    <Run Text="{Binding CurrentUser}"/>
                    <Run Text=" | "/>
                </TextBlock>
                <TextBlock Text="{Binding CurrentTime}" FontSize="11" Foreground="#666"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 3: Write MainWindow.xaml.cs**

Write `src/BalanceSystem.App/Views/MainWindow.xaml.cs`:

```csharp
using System.Windows;

namespace BalanceSystem.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
```

- [ ] **Step 4: Register ViewModels in DI**

Edit `src/BalanceSystem.App/DependencyInjection/ServiceCollectionExtensions.cs`. Add inside `AddBalanceSystemServices`:

```csharp
// ViewModels
services.AddSingleton<ViewModels.MonitoringViewModel>();
services.AddSingleton<ViewModels.BalancingTestViewModel>();
services.AddSingleton<ViewModels.MainViewModel>();

// Business services
services.AddSingleton<BalanceSystem.Core.Services.IBalancingTestService,
                       BalanceSystem.Core.Services.BalancingTestService>();
```

- [ ] **Step 5: Wire MainWindow in App.xaml.cs**

Edit `src/BalanceSystem.App/App.xaml.cs`, modify `OnStartup`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    var initializer = Services.GetRequiredService<DatabaseInitializer>();
    initializer.Initialize();

    var mainViewModel = Services.GetRequiredService<ViewModels.MainViewModel>();
    var mainWindow = new Views.MainWindow { DataContext = mainViewModel };
    mainWindow.Show();
}
```

- [ ] **Step 6: Build**

Run: `dotnet build /e/Balancer/src/BalanceSystem.App`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add src/BalanceSystem.App/
git commit -m "feat: add MainWindow with tab navigation, header, and status bar"
```

---

### Task 23: Create sample CSV test data

**Files:**
- Create: `src/BalanceSystem.App/Data/Simulation/initial_run.csv`
- Create: `src/BalanceSystem.App/Data/Simulation/left_trial.csv`
- Create: `src/BalanceSystem.App/Data/Simulation/right_trial.csv`

**Interfaces:**
- Produces: Sample CSV files for end-to-end testing of the simulation pipeline

- [ ] **Step 1: Write CSV generation script and run it**

Run:
```bash
python3 << 'PYEOF'
import math, csv, os

os.makedirs("src/BalanceSystem.App/Data/Simulation", exist_ok=True)

def gen(fn, speed, la, lp, ra, rp, dur=0.5):
    sr = 6400
    n = int(sr * dur)
    f = speed / 60.0
    with open(fn, "w", newline="") as fh:
        w = csv.writer(fh)
        w.writerow(["Timestamp","LeftChannel","RightChannel","TachPulse"])
        for i in range(n):
            t = i/sr
            left = la * math.sin(2*math.pi*f*t + math.radians(lp))
            right = ra * math.sin(2*math.pi*f*t + math.radians(rp))
            tach = 5.0 if i % max(1,int(sr/f)) == 0 else 0.0
            w.writerow([f"{t:.6f}",f"{left:.6f}",f"{right:.6f}",f"{tach:.1f}"])

gen("src/BalanceSystem.App/Data/Simulation/initial_run.csv", 1500, 2.5, 30, 1.8, 150)
gen("src/BalanceSystem.App/Data/Simulation/left_trial.csv", 1500, 3.2, 50, 2.0, 160)
gen("src/BalanceSystem.App/Data/Simulation/right_trial.csv", 1500, 2.0, 25, 2.8, 200)
print("CSV files generated")
PYEOF
```

- [ ] **Step 2: Verify files**

Run: `ls -la src/BalanceSystem.App/Data/Simulation/`
Expected: 3 CSV files listed

- [ ] **Step 3: Commit**

```bash
git add src/BalanceSystem.App/Data/
git commit -m "feat: add sample CSV simulation data files"
```

---

### Task 24: Integration — ensure project builds, tests pass, and app runs

**Files:**
- Modify: `src/BalanceSystem.App/BalanceSystem.App.csproj` (ensure data files copied)

**Interfaces:**
- Produces: Running application

- [ ] **Step 1: Add Content items to csproj**

Edit `src/BalanceSystem.App/BalanceSystem.App.csproj`, add inside `<Project>`:

```xml
<ItemGroup>
  <Content Include="Data\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

- [ ] **Step 2: Full solution build**

Run: `dotnet build /e/Balancer`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Run all unit tests**

Run: `dotnet test /e/Balancer/tests/BalanceSystem.Core.Tests`
Expected: All tests PASS

- [ ] **Step 4: Verify app launches**

Run: `dotnet run --project /e/Balancer/src/BalanceSystem.App`
Expected: Window opens (verify manually, then close)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: configure data deployment and verify end-to-end build"
```

---

## Phase 1 Completion Checklist

After all 24 tasks, verify:

- [ ] Solution builds with 0 errors
- [ ] All unit tests pass (FFT + InfluenceCoefficientSolver)
- [ ] App launches and displays MainWindow with two tabs
- [ ] Monitoring tab shows waveform, 5 numeric displays, and polar plot
- [ ] CSV simulation service plays back data and updates UI
- [ ] Balancing test tab shows the 4-step wizard
- [ ] Step progression: Initial -> Left Trial -> Right Trial -> Calculation -> Result
- [ ] Balancing result shows left/right mass and angle with PASS/FAIL
- [ ] Serilog logs appear in console and `logs/` directory

---

## Remaining Phases (Outlines)

### Phase 2 — Data Management (separate plan)
- Recipe management CRUD with FreeSql (Recipes table)
- History records query, detail view, comparison
- PDF report export with QuestPDF
- Recipe import/export (JSON/XML)

### Phase 3 — System Features (separate plan)
- User authentication and 3-level permission system
- Alarm system (types, notification, history)
- Operation logging (immutable audit trail)
- Automatic data backup with retention policy

### Phase 4 — Extended Features (separate plan)
- YOLOv8n ONNX vision detection integration
- Device calibration workflows
- Modbus communication diagnostics tool
- Serial port monitoring

Each phase builds on the foundation established in Phase 1.
