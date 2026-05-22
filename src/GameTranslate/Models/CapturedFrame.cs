using System.Windows.Media.Imaging;

namespace GameTranslate.Models;

public sealed record CapturedFrame(
    int Width,
    int Height,
    int Stride,
    byte[] BgraPixels,
    BitmapSource Preview);

