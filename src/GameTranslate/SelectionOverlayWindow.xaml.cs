using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        SourceInitialized += (_, _) => UpdateScreenScale();

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

        Canvas.SetLeft(Toolbar, Math.Clamp(x, 0, Math.Max(0, Width - 420)));
        Canvas.SetTop(Toolbar, Math.Clamp(y + height + 10, 0, Math.Max(0, Height - 48)));
        RegionText.Text = $"{CurrentRegion.ToDisplayText()} · 缩放 {_screenScaleX * 100:0}% x {_screenScaleY * 100:0}%";
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

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        SelectedRegion = CurrentRegion;
        RaiseSelectionChanged();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == WpfKey.Escape)
        {
            Close();
        }
        else if (e.Key == WpfKey.Enter)
        {
            SelectedRegion = CurrentRegion;
            RaiseSelectionChanged();
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
