using System.Text.RegularExpressions;

namespace AlphaQuant.Core;

public static partial class FilingLinks
{
    private const string Host = "www.sec.gov";

    /// <summary>Builds the SEC folder for a filing, or null when the CIK or accession number is not valid.</summary>
    public static Uri? TryBuild(long cik, string accession)
    {
        if (cik <= 0 || !AccessionPattern().IsMatch(accession))
        {
            return null;
        }

        var folder = accession.Replace("-", string.Empty, StringComparison.Ordinal);
        return new Uri($"https://{Host}/Archives/edgar/data/{cik}/{folder}/");
    }

    /// <summary>True only for https URLs on the SEC web host; used before opening any link.</summary>
    public static bool IsSafeToOpen(Uri uri) =>
        uri.IsAbsoluteUri
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^\d{10}-\d{2}-\d{6}$")]
    private static partial Regex AccessionPattern();
}
