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
