using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using GameTranslate.Services;
using GameTranslate.Models;
using GameTranslate.ViewModels;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WpfPoint = System.Windows.Point;

namespace GameTranslate;

public partial class MainWindow : Window
{
    private const double FallbackPreviewPanelHeight = 136;
    private const double AgentCardMinimumWindowHeight = 520;
    private const double StartupScreenMargin = 16;

    private readonly ScreenCaptureService _selectionPreviewCaptureService = new();
    private SafeHotKeyMonitor? _hotKeyMonitor;
    private bool _isPreviewVisible;
    private bool _isSelectingRegion;
    private double _previewPanelHeightDelta = FallbackPreviewPanelHeight;
    private double _heightBeforeAgentCard;
    private PreviewTransformState _selectionPreviewTransform = new(1, 0, 0);
    private CapturedFrame? _selectionPreviewFrame;
    private CaptureRegion _selectionScreenRegion;
    private PreviewContentPoint? _selectionDragStart;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += MainWindow_Loaded;

        _hotKeyMonitor = new SafeHotKeyMonitor(() => Dispatcher.Invoke(() =>
        {
            if (viewModel.TranslateCommand.CanExecute(null))
            {
                viewModel.TranslateCommand.Execute(null);
            }
        }));
        _hotKeyMonitor.StartMonitoring();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left, workArea.Right - ActualWidth - StartupScreenMargin);
        Top = workArea.Top + StartupScreenMargin;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TranslatedText))
        {
            Dispatcher.BeginInvoke(() => TranslationResultTextBox.ScrollToEnd());
        }
    }

    private async void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        await BeginRegionSelectionAsync(viewModel);
    }

    private void PreviewToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreviewVisible)
        {
            _previewPanelHeightDelta = GetPreviewPanelHeightDelta();
            PreviewPanel.Visibility = Visibility.Collapsed;
            PreviewToggleButton.Content = "显示识别";
            Height = Math.Max(MinHeight, ActualHeight - _previewPanelHeightDelta);
            _isPreviewVisible = false;
            return;
        }

        PreviewPanel.Visibility = Visibility.Visible;
        PreviewToggleButton.Content = "隐藏识别";
        Height = ActualHeight + (_previewPanelHeightDelta > 0 ? _previewPanelHeightDelta : FallbackPreviewPanelHeight);
        _isPreviewVisible = true;
    }

    private double GetPreviewPanelHeightDelta()
    {
        var margin = PreviewPanel.Margin;
        var height = PreviewPanel.ActualHeight + margin.Top + margin.Bottom;
        return height > 1 ? height : FallbackPreviewPanelHeight;
    }

    private void AgentCardButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActualHeight < AgentCardMinimumWindowHeight)
        {
            _heightBeforeAgentCard = ActualHeight;
            Height = AgentCardMinimumWindowHeight;
        }

        AgentCardOverlay.Visibility = Visibility.Visible;
    }

    private void CloseAgentCardButton_Click(object sender, RoutedEventArgs e)
    {
        CloseAgentCard();
    }

    private void AgentCardOverlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        CloseAgentCard();
    }

    private void AgentCardSurface_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void CloseAgentCard()
    {
        AgentCardOverlay.Visibility = Visibility.Collapsed;
        if (_heightBeforeAgentCard > 0)
        {
            Height = Math.Max(MinHeight, _heightBeforeAgentCard);
            _heightBeforeAgentCard = 0;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    protected override void OnPreviewKeyDown(WpfKeyEventArgs e)
    {
        if (_isSelectingRegion && e.Key == Key.Escape)
        {
            ExitRegionSelectionMode(clearImage: true);
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _hotKeyMonitor?.Dispose();
        _hotKeyMonitor = null;

        if (DataContext is IDisposable disposable)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            disposable.Dispose();
        }

        base.OnClosing(e);
    }

    private async Task BeginRegionSelectionAsync(MainViewModel viewModel)
    {
        if (_isSelectingRegion)
        {
            ExitRegionSelectionMode(clearImage: true);
            return;
        }

        var screenRegion = GetVirtualScreenPixelRegion();
        CapturedFrame frame;
        try
        {
            Hide();
            await Task.Delay(150);
            frame = await Task.Run(() => _selectionPreviewCaptureService.Capture(screenRegion));
        }
        catch (Exception ex)
        {
            viewModel.StatusText = "截图预览失败";
            viewModel.TranslatedText = ExceptionFormatter.Format(ex);
            return;
        }
        finally
        {
            Show();
            Activate();
        }

        _selectionScreenRegion = screenRegion;
        _selectionPreviewFrame = frame;
        RegionSelectionImage.Source = frame.Preview;
        RegionSelectionRectangle.Visibility = Visibility.Collapsed;
        ResetRegionSelectionZoom();
        RegionSelectionPanel.Visibility = Visibility.Visible;
        _isSelectingRegion = true;
    }

    private static CaptureRegion GetVirtualScreenPixelRegion()
    {
        var bounds = Forms.SystemInformation.VirtualScreen;
        return new CaptureRegion(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
    }

    private void RegionSelectionViewport_MouseLeftButtonDown(object sender, WpfMouseButtonEventArgs e)
    {
        if (!_isSelectingRegion)
        {
            return;
        }

        _selectionDragStart = ToRegionSelectionContentPoint(e.GetPosition(RegionSelectionViewport));
        RegionSelectionViewport.CaptureMouse();
        UpdateRegionSelectionRectangle(_selectionDragStart.Value);
    }

    private void RegionSelectionViewport_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_selectionDragStart is null || !RegionSelectionViewport.IsMouseCaptured)
        {
            return;
        }

        UpdateRegionSelectionRectangle(ToRegionSelectionContentPoint(e.GetPosition(RegionSelectionViewport)));
    }

    private void RegionSelectionViewport_MouseLeftButtonUp(object sender, WpfMouseButtonEventArgs e)
    {
        if (_selectionDragStart is null || _selectionPreviewFrame is null)
        {
            return;
        }

        var current = ToRegionSelectionContentPoint(e.GetPosition(RegionSelectionViewport));
        var dragRegion = CaptureRegion.Normalize(
            _selectionDragStart.Value.X,
            _selectionDragStart.Value.Y,
            current.X - _selectionDragStart.Value.X,
            current.Y - _selectionDragStart.Value.Y);

        RegionSelectionViewport.ReleaseMouseCapture();
        _selectionDragStart = null;

        var selection = PreviewSelectionMapper.CreateSelection(
            _selectionScreenRegion,
            _selectionPreviewFrame.Width,
            _selectionPreviewFrame.Height,
            RegionSelectionContent.ActualWidth,
            RegionSelectionContent.ActualHeight,
            dragRegion);

        if (selection.IsEmpty)
        {
            RegionSelectionRectangle.Visibility = Visibility.Collapsed;
            return;
        }

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SetCaptureSelection(selection);
        }

        ExitRegionSelectionMode(clearImage: true);
    }

    private void RegionSelectionViewport_MouseWheel(object sender, WpfMouseWheelEventArgs e)
    {
        if (!_isSelectingRegion)
        {
            return;
        }

        var anchor = e.GetPosition(RegionSelectionViewport);
        _selectionPreviewTransform = PreviewSelectionTransform.ZoomAround(
            anchor.X,
            anchor.Y,
            _selectionPreviewTransform.Zoom,
            _selectionPreviewTransform.PanX,
            _selectionPreviewTransform.PanY,
            e.Delta);
        ApplyRegionSelectionTransform();
        e.Handled = true;
    }

    private PreviewContentPoint ToRegionSelectionContentPoint(WpfPoint viewportPoint)
    {
        return PreviewSelectionTransform.ToContentPoint(
            viewportPoint.X,
            viewportPoint.Y,
            _selectionPreviewTransform.Zoom,
            _selectionPreviewTransform.PanX,
            _selectionPreviewTransform.PanY);
    }

    private void UpdateRegionSelectionRectangle(PreviewContentPoint current)
    {
        if (_selectionDragStart is null)
        {
            return;
        }

        var region = CaptureRegion.Normalize(
            _selectionDragStart.Value.X,
            _selectionDragStart.Value.Y,
            current.X - _selectionDragStart.Value.X,
            current.Y - _selectionDragStart.Value.Y)
            .Clamp(0, 0, RegionSelectionContent.ActualWidth, RegionSelectionContent.ActualHeight);

        Canvas.SetLeft(RegionSelectionRectangle, region.X);
        Canvas.SetTop(RegionSelectionRectangle, region.Y);
        RegionSelectionRectangle.Width = region.Width;
        RegionSelectionRectangle.Height = region.Height;
        RegionSelectionRectangle.Visibility = region.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ResetRegionSelectionZoom()
    {
        _selectionPreviewTransform = new PreviewTransformState(1, 0, 0);
        ApplyRegionSelectionTransform();
    }

    private void ApplyRegionSelectionTransform()
    {
        RegionSelectionScaleTransform.ScaleX = _selectionPreviewTransform.Zoom;
        RegionSelectionScaleTransform.ScaleY = _selectionPreviewTransform.Zoom;
        RegionSelectionTranslateTransform.X = _selectionPreviewTransform.PanX;
        RegionSelectionTranslateTransform.Y = _selectionPreviewTransform.PanY;
    }

    private void ExitRegionSelectionMode(bool clearImage)
    {
        RegionSelectionViewport.ReleaseMouseCapture();
        RegionSelectionPanel.Visibility = Visibility.Collapsed;
        RegionSelectionRectangle.Visibility = Visibility.Collapsed;
        _selectionDragStart = null;
        _isSelectingRegion = false;

        if (clearImage)
        {
            ResetRegionSelectionZoom();
            RegionSelectionImage.Source = null;
            _selectionPreviewFrame = null;
            _selectionScreenRegion = default;
        }
    }
}
