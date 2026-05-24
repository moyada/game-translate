using System.Windows;
using System.Windows.Input;

namespace GameTranslate;

public partial class FloatingTranslationWindow : Window
{
    public FloatingTranslationWindow()
    {
        InitializeComponent();
    }

    private void WindowSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
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
}
