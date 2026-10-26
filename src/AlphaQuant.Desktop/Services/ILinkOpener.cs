using System;
using System.Diagnostics;
using AlphaQuant.Core;

namespace AlphaQuant.Desktop.Services;

public interface ILinkOpener
{
    void Open(Uri uri);
}

/// <summary>Opens links in the default browser, but only https links on the SEC web host.</summary>
public sealed class ShellLinkOpener : ILinkOpener
{
    public void Open(Uri uri)
    {
        if (!FilingLinks.IsSafeToOpen(uri))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
