using System.Diagnostics;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

public sealed class SystemBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri authorizationUri)
    {
        if (authorizationUri.Scheme != "https" || authorizationUri.Host != "app.alpaca.markets"
            || authorizationUri.AbsolutePath != "/oauth/authorize" || authorizationUri.UserInfo.Length != 0)
            throw new MarketDataException(MarketFailure.Configuration, "Only Alpaca browser authorization is supported.");
        try { Process.Start(new ProcessStartInfo(authorizationUri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        { throw new MarketDataException(MarketFailure.Configuration, "The system browser could not be opened."); }
    }
}
