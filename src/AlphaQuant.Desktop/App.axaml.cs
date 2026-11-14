using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Infrastructure;
using System;
using System.Net.Http;

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

            var brokerText = Environment.GetEnvironmentVariable("AQ_OAUTH_BROKER_ORIGIN");
            var configured = Uri.TryCreate(brokerText, UriKind.Absolute, out var broker)
                && (broker.Scheme == "https" || (broker.Scheme == "http" && broker.IsLoopback))
                && broker.AbsolutePath == "/" && broker.Query.Length == 0 && broker.Fragment.Length == 0
                && broker.UserInfo.Length == 0;
            // A placeholder supports disconnected fundamentals; Connect is disabled without valid configuration.
            var brokerHttp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(90) };
            var marketHttp = AlpacaMarketDataProvider.CreateHttpClient();
            var backend = new ComparisonBackend(sec, brokerHttp, configured ? broker! : new Uri("https://not-configured.invalid/"),
                marketHttp, new SystemBrowserLauncher());
            var evidence = new ReviewedEvidenceStore();
            var main = new MainWindowViewModel(sec, sec, contact, new ShellLinkOpener(), new ResearchBackend(backend, configured, evidence), evidence,
                new SqliteResearchStore(SqliteResearchStore.DefaultPath));
            desktop.Exit += (_, _) => { main.Shutdown(); brokerHttp.Dispose(); marketHttp.Dispose(); };
            desktop.MainWindow = new MainWindow
            {
                DataContext = main,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
