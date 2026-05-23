using GameTranslate.Models;

namespace GameTranslate.Services;

public static class ImageChangeDetector
{
    public const double DefaultChangedSampleRatioThreshold = 0.10;
    public const byte DefaultSampleDifferenceThreshold = 24;
    private const int SampleColumns = 16;
    private const int SampleRows = 16;

    public static ImageFingerprint CreateFingerprint(byte[] bgraPixels, int width, int height, int stride)
    {
        if (bgraPixels.Length < stride * height)
        {
            throw new ArgumentException("像素缓冲区长度不足。", nameof(bgraPixels));
        }

        var samples = new byte[SampleColumns * SampleRows];
        var index = 0;
        for (var row = 0; row < SampleRows; row++)
        {
            var y = Math.Clamp((int)Math.Round((row + 0.5) * height / SampleRows), 0, height - 1);
            for (var column = 0; column < SampleColumns; column++)
            {
                var x = Math.Clamp((int)Math.Round((column + 0.5) * width / SampleColumns), 0, width - 1);
                var pixelOffset = y * stride + x * 4;
                var blue = bgraPixels[pixelOffset];
                var green = bgraPixels[pixelOffset + 1];
                var red = bgraPixels[pixelOffset + 2];
                samples[index++] = ToLuminance(red, green, blue);
            }
        }

        return new ImageFingerprint(width, height, samples);
    }

    public static double CalculateDifference(ImageFingerprint previous, ImageFingerprint current)
    {
        if (previous.Width != current.Width || previous.Height != current.Height)
        {
            return double.MaxValue;
        }

        if (previous.Samples.Length != current.Samples.Length)
        {
            return double.MaxValue;
        }

        var total = 0;
        for (var i = 0; i < previous.Samples.Length; i++)
        {
            total += Math.Abs(previous.Samples[i] - current.Samples[i]);
        }

        return total / (double)previous.Samples.Length;
    }

    public static double CalculateChangedSampleRatio(
        ImageFingerprint previous,
        ImageFingerprint current,
        byte sampleDifferenceThreshold = DefaultSampleDifferenceThreshold)
    {
        if (previous.Width != current.Width || previous.Height != current.Height)
        {
            return 1.0;
        }

        if (previous.Samples.Length != current.Samples.Length)
        {
            return 1.0;
        }

        var changedSamples = 0;
        for (var i = 0; i < previous.Samples.Length; i++)
        {
            if (Math.Abs(previous.Samples[i] - current.Samples[i]) > sampleDifferenceThreshold)
            {
                changedSamples++;
            }
        }

        return changedSamples / (double)previous.Samples.Length;
    }

    public static bool HasMeaningfulChange(
        ImageFingerprint? previous,
        ImageFingerprint current,
        double changedSampleRatioThreshold = DefaultChangedSampleRatioThreshold)
    {
        if (previous is null)
        {
            return true;
        }

        return CalculateChangedSampleRatio(previous, current) > changedSampleRatioThreshold;
    }

    private static byte ToLuminance(byte red, byte green, byte blue)
    {
        return (byte)Math.Clamp((red * 299 + green * 587 + blue * 114) / 1000, 0, 255);
    }
}
