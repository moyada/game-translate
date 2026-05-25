using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace GameTranslate;

public partial class FloatingTranslationWindow : Window
{
    public FloatingTranslationWindow()
    {
        InitializeComponent();
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
