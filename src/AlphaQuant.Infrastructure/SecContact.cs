using System.Text.RegularExpressions;

namespace AlphaQuant.Infrastructure;

/// <summary>
/// The SEC requires automated clients to declare themselves with a contact. The contact comes from the
/// environment, never from the repository.
/// </summary>
public static partial class SecContact
{
    public const string EnvironmentVariable = "AQ_SEC_CONTACT";

    private const string AppName = "AlphaQuant/0.1";

    public static string? FromEnvironment() => Environment.GetEnvironmentVariable(EnvironmentVariable);

    /// <summary>Returns the User-Agent value for a plausible contact email, or null when it is missing or invalid.</summary>
    public static string? TryBuildUserAgent(string? contact)
    {
        var trimmed = contact?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !EmailPattern().IsMatch(trimmed))
        {
            return null;
        }

        // Validate against the real header collection: "product (comment)" is two header values, not one.
        var value = $"{AppName} ({trimmed})";
        using var probe = new HttpRequestMessage();
        return probe.Headers.UserAgent.TryParseAdd(value) ? value : null;
    }

    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)+$")]
    private static partial Regex EmailPattern();
}
