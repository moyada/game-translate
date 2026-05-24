using System.Windows;
using GameTranslate.Services;
using GameTranslate.ViewModels;

namespace GameTranslate;

public partial class MainWindow : Window
{
    private SelectionOverlayWindow? _selectionOverlay;
    private SafeHotKeyMonitor? _hotKeyMonitor;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;

        _hotKeyMonitor = new SafeHotKeyMonitor(() => Dispatcher.Invoke(() =>
        {
            if (viewModel.TranslateCommand.CanExecute(null))
            {
                viewModel.TranslateCommand.Execute(null);
            }
        }));
        _hotKeyMonitor.StartMonitoring();
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

    protected override void OnClosed(EventArgs e)
    {
        _isClosing = true;
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
            disposable.Dispose();
        }

        _isClosing = true;

        base.OnClosing(e);
    }

    private async void SelectionOverlay_Closed(object? sender, EventArgs e)
    {
        if (!_isClosing)
        {
            _selectionOverlay = null;
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.ClearCaptureSelectionAsync();
            }
        }
    }
}
