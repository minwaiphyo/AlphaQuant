namespace AlphaQuant.Core;

public enum DataSourceErrorKind
{
    /// <summary>The SEC contact needed for the User-Agent is missing or invalid; no request was made.</summary>
    ContactMissing,
    NotFound,
    Forbidden,
    RateLimited,
    ServerError,
    Network,
    InvalidData,
}

public sealed class DataSourceException : Exception
{
    public DataSourceException(DataSourceErrorKind kind, string message, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
    }

    public DataSourceErrorKind Kind { get; }
}

public interface ICompanyDirectory
{
    Task<CompanyDirectoryResult> GetCompaniesAsync(CancellationToken cancellationToken);
}

public interface ICompanyFactsSource
{
    Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken cancellationToken);
}
