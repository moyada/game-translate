namespace GameTranslate.Services;

public static class OcrImagePreprocessor
{
    private const int TextDilationRadius = 1;

    public static byte[] CreateHighContrastBgra(byte[] sourceBgra, int width, int height, int stride)
    {
        if (width <= 0 || height <= 0)
        {
            return Array.Empty<byte>();
        }

        if (sourceBgra.Length < stride * height)
        {
            throw new ArgumentException("像素缓冲区长度不足。", nameof(sourceBgra));
        }

        var textMask = new bool[width * height];
        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = rowOffset + x * 4;
                var blue = sourceBgra[offset];
                var green = sourceBgra[offset + 1];
                var red = sourceBgra[offset + 2];
                textMask[y * width + x] = IsLikelyTextPixel(red, green, blue);
            }
        }

        var outputStride = width * 4;
        var output = new byte[outputStride * height];
        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * outputStride;
            for (var x = 0; x < width; x++)
            {
                var isText = HasTextNeighbor(textMask, width, height, x, y);
                var value = isText ? (byte)0 : (byte)255;
                var offset = rowOffset + x * 4;
                output[offset] = value;
                output[offset + 1] = value;
                output[offset + 2] = value;
                output[offset + 3] = 255;
            }
        }

        return output;
    }

    public static bool IsLikelyTextPixel(byte red, byte green, byte blue)
    {
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var saturation = max - min;
        var luminance = (red * 299 + green * 587 + blue * 114) / 1000;

        var saturatedGameText = saturation >= 35 && max >= 90 && luminance >= 45;
        var brightText = luminance >= 190 && saturation <= 80;
        return saturatedGameText || brightText;
    }

    private static bool HasTextNeighbor(bool[] textMask, int width, int height, int x, int y)
    {
        for (var dy = -TextDilationRadius; dy <= TextDilationRadius; dy++)
        {
            var sampleY = y + dy;
            if (sampleY < 0 || sampleY >= height)
            {
                continue;
            }

            for (var dx = -TextDilationRadius; dx <= TextDilationRadius; dx++)
            {
                var sampleX = x + dx;
                if (sampleX < 0 || sampleX >= width)
                {
                    continue;
                }

                if (textMask[sampleY * width + sampleX])
                {
                    return true;
                }
            }
        }

        return false;
    }
}

