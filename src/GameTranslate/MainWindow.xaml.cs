using System.Windows;
using System.ComponentModel;
using GameTranslate.Services;
using GameTranslate.ViewModels;

namespace GameTranslate;

public partial class MainWindow : Window
{
    private const double FallbackPreviewPanelHeight = 136;
    private const double FallbackTranslationPanelHeight = 170;
    private const double AgentCardMinimumWindowHeight = 520;
    private const double StartupScreenMargin = 16;

    private SelectionOverlayWindow? _selectionOverlay;
    private FloatingTranslationWindow? _floatingTranslationWindow;
    private SafeHotKeyMonitor? _hotKeyMonitor;
    private bool _isPreviewVisible = true;
    private double _previewPanelHeightDelta;
    private double _translationPanelHeightDelta;
    private double _heightBeforeAgentCard;

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
            Dispatcher.BeginInvoke(() => TranslationResultScrollViewer.ScrollToEnd());
        }
    }

    private void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (_selectionOverlay is { IsVisible: true })
        {
            _selectionOverlay.Close();
            return;
        }

        _selectionOverlay = new SelectionOverlayWindow(viewModel.CaptureSelection.DisplayRegion)
        {
            Owner = this
        };
        _selectionOverlay.SelectionChanged += (_, selection) => viewModel.SetCaptureSelection(selection);
        _selectionOverlay.Closed += SelectionOverlay_Closed;
        _selectionOverlay.Show();
        viewModel.SetCaptureSelection(_selectionOverlay.SelectedCaptureSelection);
    }

    private void PreviewToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreviewVisible)
        {
            _previewPanelHeightDelta = GetPreviewPanelHeightDelta();
            PreviewPanel.Visibility = Visibility.Collapsed;
            PreviewToggleButton.Content = "显示 OCR";
            Height = Math.Max(MinHeight, ActualHeight - _previewPanelHeightDelta);
            _isPreviewVisible = false;
            return;
        }

        PreviewPanel.Visibility = Visibility.Visible;
        PreviewToggleButton.Content = "隐藏 OCR";
        Height = ActualHeight + (_previewPanelHeightDelta > 0 ? _previewPanelHeightDelta : FallbackPreviewPanelHeight);
        _isPreviewVisible = true;
    }

    private double GetPreviewPanelHeightDelta()
    {
        var margin = PreviewPanel.Margin;
        var height = PreviewPanel.ActualHeight + margin.Top + margin.Bottom;
        return height > 1 ? height : FallbackPreviewPanelHeight;
    }

    private void FloatingToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_floatingTranslationWindow is { IsVisible: true })
        {
            CloseFloatingTranslationWindow(restoreTranslationPanel: true);
            return;
        }

        ShowFloatingTranslationWindow();
    }

    private void ShowFloatingTranslationWindow()
    {
        if (_floatingTranslationWindow is { IsVisible: true })
        {
            return;
        }

        _translationPanelHeightDelta = CollapseTranslationPanelAndShrinkWindow();
        FloatingToggleButton.Content = "关闭悬浮窗";

        _floatingTranslationWindow = new FloatingTranslationWindow
        {
            Owner = this,
            DataContext = DataContext,
            Left = Left + 24,
            Top = Top + 80
        };
        _floatingTranslationWindow.Closed += FloatingTranslationWindow_Closed;
        _floatingTranslationWindow.Show();
    }

    private double CollapseTranslationPanelAndShrinkWindow()
    {
        var heightDelta = GetTranslationPanelHeightDelta();
        TranslationResultPanel.Visibility = Visibility.Collapsed;

        var previousHeight = ActualHeight;
        Height = Math.Max(MinHeight, ActualHeight - heightDelta);
        return Math.Max(0, previousHeight - Height);
    }

    private double GetTranslationPanelHeightDelta()
    {
        var margin = TranslationResultPanel.Margin;
        var height = TranslationResultPanel.ActualHeight + margin.Top + margin.Bottom;
        return height > 1 ? height : FallbackTranslationPanelHeight;
    }

    private void CloseFloatingTranslationWindow(bool restoreTranslationPanel)
    {
        if (_floatingTranslationWindow is not null)
        {
            _floatingTranslationWindow.Closed -= FloatingTranslationWindow_Closed;
            _floatingTranslationWindow.Close();
            _floatingTranslationWindow = null;
        }

        if (restoreTranslationPanel)
        {
            RestoreTranslationPanel();
        }
    }

    private void RestoreTranslationPanel()
    {
        TranslationResultPanel.Visibility = Visibility.Visible;
        FloatingToggleButton.Content = "悬浮窗";

        if (_translationPanelHeightDelta > 0)
        {
            Height = ActualHeight + _translationPanelHeightDelta;
            _translationPanelHeightDelta = 0;
        }
    }

    private void FloatingTranslationWindow_Closed(object? sender, EventArgs e)
    {
        _floatingTranslationWindow = null;
        RestoreTranslationPanel();
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
        CloseFloatingTranslationWindow(restoreTranslationPanel: false);
        _selectionOverlay?.Close();
        _selectionOverlay = null;
        base.OnClosed(e);
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

    private void SelectionOverlay_Closed(object? sender, EventArgs e)
    {
        _selectionOverlay = null;
    }
}
