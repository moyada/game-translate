using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;
using GameTranslate.Models;
using GameTranslate.Services;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace GameTranslate;

public partial class SelectionOverlayWindow : Window
{
    private const double MinimumSelectionWidth = 80;
    private const double MinimumSelectionHeight = 40;
    private const int WmNcHitTest = 0x0084;
    private static readonly IntPtr HitTestClient = new(1);
    private static readonly IntPtr HitTestTransparent = new(-1);
    private WpfPoint _startMouse;
    private CaptureRegion _startRegion;
    private DragMode _dragMode = DragMode.None;
    private double _screenScaleX = 1;
    private double _screenScaleY = 1;

    public SelectionOverlayWindow(CaptureRegion initialRegion)
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        SourceInitialized += (_, _) =>
        {
            UpdateScreenScale();
            AddClickThroughHook();
        };

        if (!initialRegion.IsEmpty)
        {
            SetSelection(initialRegion);
        }
        else
        {
            SetSelection(CreateDefaultRegion());
        }
    }

    public CaptureRegion SelectedRegion { get; private set; }

    public CaptureSelection SelectedCaptureSelection => new(
        SelectedRegion,
        CoordinateScaler.Scale(SelectedRegion, _screenScaleX, _screenScaleY),
        _screenScaleX,
        _screenScaleY);

    public event EventHandler<CaptureSelection>? SelectionChanged;

    private CaptureRegion CurrentRegion
    {
        get
        {
            return new CaptureRegion(
                Canvas.GetLeft(SelectionBorder) + Left,
                Canvas.GetTop(SelectionBorder) + Top,
                SelectionBorder.Width,
                SelectionBorder.Height);
        }
    }

    private CaptureRegion CreateDefaultRegion()
    {
        var width = Math.Min(760, Width * 0.58);
        var height = Math.Min(220, Height * 0.22);
        var x = (Width - width) / 2 + Left;
        var y = Height * 0.68 + Top;
        return new CaptureRegion(x, y, width, height);
    }

    private void SelectionBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginDrag(e, DragMode.Move);
        SelectionBorder.CaptureMouse();
    }

    private void SelectionBorder_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_dragMode == DragMode.Move)
        {
            UpdateSelection(e.GetPosition(RootCanvas));
        }
    }

    private void SelectionBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
        SelectionBorder.ReleaseMouseCapture();
    }

    private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginDrag(e, GetHandleDragMode(sender));
        if (sender is WpfRectangle handle)
        {
            handle.CaptureMouse();
        }
    }

    private void Handle_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_dragMode != DragMode.None && _dragMode != DragMode.Move)
        {
            UpdateSelection(e.GetPosition(RootCanvas));
        }
    }

    private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
        if (sender is WpfRectangle handle)
        {
            handle.ReleaseMouseCapture();
        }
    }

    private void BeginDrag(MouseButtonEventArgs e, DragMode dragMode)
    {
        _dragMode = dragMode;
        _startMouse = e.GetPosition(RootCanvas);
        _startRegion = CurrentRegion;
        e.Handled = true;
    }

    private void EndDrag()
    {
        _dragMode = DragMode.None;
        SelectedRegion = CurrentRegion;
        RaiseSelectionChanged();
    }

    private void UpdateSelection(WpfPoint currentMouse)
    {
        var deltaX = currentMouse.X - _startMouse.X;
        var deltaY = currentMouse.Y - _startMouse.Y;
        var localX = _startRegion.X - Left;
        var localY = _startRegion.Y - Top;
        var width = _startRegion.Width;
        var height = _startRegion.Height;

        switch (_dragMode)
        {
            case DragMode.Move:
                localX += deltaX;
                localY += deltaY;
                break;
            case DragMode.NorthWest:
                localX += deltaX;
                localY += deltaY;
                width -= deltaX;
                height -= deltaY;
                break;
            case DragMode.NorthEast:
                localY += deltaY;
                width += deltaX;
                height -= deltaY;
                break;
            case DragMode.SouthWest:
                localX += deltaX;
                width -= deltaX;
                height += deltaY;
                break;
            case DragMode.SouthEast:
                width += deltaX;
                height += deltaY;
                break;
        }

        width = Math.Max(width, MinimumSelectionWidth);
        height = Math.Max(height, MinimumSelectionHeight);
        localX = Math.Clamp(localX, 0, Width - width);
        localY = Math.Clamp(localY, 0, Height - height);

        SetSelection(new CaptureRegion(localX + Left, localY + Top, width, height));
    }

    private void SetSelection(CaptureRegion region)
    {
        var clamped = region.Clamp(Left, Top, Width, Height);
        var localX = clamped.X - Left;
        var localY = clamped.Y - Top;
        var selectionWidth = Math.Min(Math.Max(clamped.Width, MinimumSelectionWidth), Width);
        var selectionHeight = Math.Min(Math.Max(clamped.Height, MinimumSelectionHeight), Height);

        localX = Math.Clamp(localX, 0, Math.Max(0, Width - selectionWidth));
        localY = Math.Clamp(localY, 0, Math.Max(0, Height - selectionHeight));

        Canvas.SetLeft(SelectionBorder, localX);
        Canvas.SetTop(SelectionBorder, localY);
        SelectionBorder.Width = selectionWidth;
        SelectionBorder.Height = selectionHeight;

        SelectedRegion = CurrentRegion;
        RaiseSelectionChanged();
        UpdateHandles();
    }

    private void UpdateHandles()
    {
        var x = Canvas.GetLeft(SelectionBorder);
        var y = Canvas.GetTop(SelectionBorder);
        var width = SelectionBorder.Width;
        var height = SelectionBorder.Height;
        const double halfHandle = 6;

        Canvas.SetLeft(NorthWestHandle, x - halfHandle);
        Canvas.SetTop(NorthWestHandle, y - halfHandle);
        Canvas.SetLeft(NorthEastHandle, x + width - halfHandle);
        Canvas.SetTop(NorthEastHandle, y - halfHandle);
        Canvas.SetLeft(SouthWestHandle, x - halfHandle);
        Canvas.SetTop(SouthWestHandle, y + height - halfHandle);
        Canvas.SetLeft(SouthEastHandle, x + width - halfHandle);
        Canvas.SetTop(SouthEastHandle, y + height - halfHandle);
    }

    private DragMode GetHandleDragMode(object sender)
    {
        return sender switch
        {
            var handle when ReferenceEquals(handle, NorthWestHandle) => DragMode.NorthWest,
            var handle when ReferenceEquals(handle, NorthEastHandle) => DragMode.NorthEast,
            var handle when ReferenceEquals(handle, SouthWestHandle) => DragMode.SouthWest,
            var handle when ReferenceEquals(handle, SouthEastHandle) => DragMode.SouthEast,
            _ => DragMode.None
        };
    }

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == WpfKey.Escape)
        {
            Close();
        }
    }

    private void UpdateScreenScale()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return;
        }

        _screenScaleX = source.CompositionTarget.TransformToDevice.M11;
        _screenScaleY = source.CompositionTarget.TransformToDevice.M22;
        RaiseSelectionChanged();
        UpdateHandles();
    }

    private void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(this, SelectedCaptureSelection);
    }

    private void AddClickThroughHook()
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmNcHitTest || !SelectionOverlayBehavior.AllowsClickThroughOutsideSelection)
        {
            return IntPtr.Zero;
        }

        var screenPoint = GetScreenPoint(lParam);
        var localPoint = PointFromScreen(screenPoint);
        if (IsInteractivePoint(localPoint))
        {
            handled = true;
            return HitTestClient;
        }

        handled = true;
        return HitTestTransparent;
    }

    private bool IsInteractivePoint(WpfPoint point)
    {
        return IsPointInsideElement(point, SelectionBorder)
            || IsPointInsideElement(point, NorthWestHandle)
            || IsPointInsideElement(point, NorthEastHandle)
            || IsPointInsideElement(point, SouthWestHandle)
            || IsPointInsideElement(point, SouthEastHandle);
    }

    private static bool IsPointInsideElement(WpfPoint point, FrameworkElement element)
    {
        var x = Canvas.GetLeft(element);
        var y = Canvas.GetTop(element);
        var width = element.ActualWidth > 0 ? element.ActualWidth : element.Width;
        var height = element.ActualHeight > 0 ? element.ActualHeight : element.Height;
        return point.X >= x
            && point.X <= x + width
            && point.Y >= y
            && point.Y <= y + height;
    }

    private static WpfPoint GetScreenPoint(IntPtr lParam)
    {
        var value = lParam.ToInt64();
        var x = unchecked((short)(value & 0xFFFF));
        var y = unchecked((short)((value >> 16) & 0xFFFF));
        return new WpfPoint(x, y);
    }

    private enum DragMode
    {
        None,
        Move,
        NorthWest,
        NorthEast,
        SouthWest,
        SouthEast
    }
}
