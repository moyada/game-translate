using System.Windows;
using GameTranslate.ViewModels;

namespace GameTranslate;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var overlay = new SelectionOverlayWindow(viewModel.CaptureRegion)
        {
            Owner = this
        };

        if (overlay.ShowDialog() == true)
        {
            viewModel.SetCaptureRegion(overlay.SelectedRegion);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }
}
