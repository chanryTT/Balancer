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
