namespace AlphaQuant.Infrastructure;

/// <summary>Never let a missing OS data directory turn settings/research into checkout-relative files.</summary>
public static class LocalDataPaths
{
    public static string ApplicationRoot => AtRoot(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify));

    public static string AtRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
            throw new InvalidOperationException("An absolute local application-data directory is required. Configure the operating-system user-data location before starting AlphaQuant.");
        return Path.Combine(root, "AlphaQuant");
    }
}
