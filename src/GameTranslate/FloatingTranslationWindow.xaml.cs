using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GameTranslate.ViewModels;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace GameTranslate;

public partial class FloatingTranslationWindow : Window
{
    public FloatingTranslationWindow()
    {
        InitializeComponent();
        DataContextChanged += FloatingTranslationWindow_DataContextChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        UnsubscribeFromViewModel(DataContext);
        base.OnClosed(e);
    }

    private void FloatingTranslationWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeFromViewModel(e.OldValue);
        SubscribeToViewModel(e.NewValue);
        Dispatcher.BeginInvoke(() => FloatingResultScrollViewer.ScrollToEnd());
    }

    private void SubscribeToViewModel(object? dataContext)
    {
        if (dataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void UnsubscribeFromViewModel(object? dataContext)
    {
        if (dataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TranslatedText))
        {
            Dispatcher.BeginInvoke(() => FloatingResultScrollViewer.ScrollToEnd());
        }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || IsScrollBarPart(e.OriginalSource as DependencyObject))
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static bool IsScrollBarPart(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is WpfScrollBar or Thumb)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
