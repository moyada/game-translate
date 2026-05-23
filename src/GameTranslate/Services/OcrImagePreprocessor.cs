namespace GameTranslate.Services;

public static class OcrImagePreprocessor
{
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
                var isText = textMask[y * width + x];
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

    public static int GetOutputStride(int width)
    {
        return width * 4;
    }

    public static bool IsLikelyTextPixel(byte red, byte green, byte blue)
    {
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var saturation = max - min;
        var luminance = (red * 299 + green * 587 + blue * 114) / 1000;

        var yellowNoticeText = red >= 150 && green >= 145 && blue <= 95 && saturation >= 55 && luminance >= 120;
        var magentaChatText = red >= 95 && blue >= 80 && green <= 95 && red > green + 35 && blue > green + 20 && saturation >= 45;
        var cyanIconOrUi = blue >= 150 && green >= 120 && red <= 120;
        return (yellowNoticeText || magentaChatText) && !cyanIconOrUi;
    }
}
