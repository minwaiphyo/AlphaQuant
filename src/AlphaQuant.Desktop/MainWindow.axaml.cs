using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AlphaQuant.Desktop;

public partial class MainWindow : Window
{
    private void AppearanceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current is { } app && sender is ComboBox selector)
            app.RequestedThemeVariant = selector.SelectedIndex switch { 1 => ThemeVariant.Light, 2 => ThemeVariant.Dark, _ => ThemeVariant.Default };
    }

    public MainWindow()
    {
        InitializeComponent();
    }
}