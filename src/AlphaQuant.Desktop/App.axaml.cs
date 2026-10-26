using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The contact is read from settings on every request, so editing it in Settings applies at once.
            var contact = new ContactSettings(ContactSettings.DefaultPath);

            // One shared client so SEC pacing applies across all requests.
            var sec = new SecClient(
                SecClient.CreateHttpClient(),
                () => contact.EffectiveContact,
                new FileCache(FileCache.DefaultRoot));

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(sec, sec, contact, new ShellLinkOpener()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
