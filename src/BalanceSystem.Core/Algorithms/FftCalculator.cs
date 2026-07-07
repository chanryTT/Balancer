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
            magnitudes[i] = 2.0 * complex[i].Magnitude / signal.Length;
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
