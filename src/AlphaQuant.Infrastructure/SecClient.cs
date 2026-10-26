using System.Net;
using System.Net.Http.Headers;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

public sealed class SecClientOptions
{
    /// <summary>Minimum gap between request starts. 150 ms is about 6 per second, under the SEC limit of 10.</summary>
    public TimeSpan MinRequestInterval { get; init; } = TimeSpan.FromMilliseconds(150);

    public int MaxRetries { get; init; } = 3;

    public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    public long MaxResponseBytes { get; init; } = 100L * 1024 * 1024;

    public TimeSpan DirectoryMaxAge { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan FactsMaxAge { get; init; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Reads the SEC ticker list and company facts. Honours SEC fair access: declared User-Agent, paced requests,
/// bounded retries on 429 and 5xx that respect Retry-After, HTTPS to SEC hosts only, and bounded response size.
/// </summary>
public sealed class SecClient : ICompanyDirectory, ICompanyFactsSource
{
    private const string TickersUrl = "https://www.sec.gov/files/company_tickers.json";
    private const string TickersKey = "company_tickers.json";

    private readonly HttpClient _http;
    private readonly Func<string?> _contact;
    private readonly ISecCache _cache;
    private readonly SecClientOptions _options;
    private readonly TimeProvider _time;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly SemaphoreSlim _pacing = new(1, 1);
    private DateTimeOffset _lastRequestStart = DateTimeOffset.MinValue;

    /// <summary>The contact is read on every request, so a change in Settings applies without a restart.</summary>
    public SecClient(
        HttpClient http,
        Func<string?> contact,
        ISecCache cache,
        SecClientOptions? options = null,
        TimeProvider? time = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _http = http;
        _contact = contact;
        _cache = cache;
        _options = options ?? new SecClientOptions();
        _time = time ?? TimeProvider.System;
        _delay = delay ?? ((span, token) => Task.Delay(span, token));
    }

    public SecClient(
        HttpClient http,
        string? contact,
        ISecCache cache,
        SecClientOptions? options = null,
        TimeProvider? time = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
        : this(http, () => contact, cache, options, time, delay)
    {
    }

    /// <summary>An HttpClient with decompression and a bounded timeout, for production use.</summary>
    public static HttpClient CreateHttpClient() =>
        new(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

    public async Task<CompanyDirectoryResult> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        var (companies, retrievedAt, isStale) = await LoadAsync(
            TickersUrl,
            TickersKey,
            _options.DirectoryMaxAge,
            SecParser.ParseDirectory,
            cancellationToken);
        return new CompanyDirectoryResult(companies, retrievedAt, isStale);
    }

    public async Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken cancellationToken)
    {
        if (cik <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cik));
        }

        var padded = cik.ToString("D10", System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            var (parsed, retrievedAt, isStale) = await LoadAsync(
                $"https://data.sec.gov/api/xbrl/companyfacts/CIK{padded}.json",
                $"companyfacts/CIK{padded}.json",
                _options.FactsMaxAge,
                SecParser.ParseFacts,
                cancellationToken);
            return new CompanyFacts(parsed.EntityName, parsed.Facts, parsed.SkippedFacts, retrievedAt, isStale);
        }
        catch (DataSourceException ex) when (ex.Kind == DataSourceErrorKind.NotFound)
        {
            throw new DataSourceException(
                DataSourceErrorKind.NotFound,
                "The SEC has no XBRL company facts for this company.",
                ex);
        }
    }

    private async Task<(T Value, DateTimeOffset RetrievedAt, bool IsStale)> LoadAsync<T>(
        string url,
        string key,
        TimeSpan maxAge,
        Func<byte[], T> parse,
        CancellationToken cancellationToken)
    {
        var userAgent = SecContact.TryBuildUserAgent(_contact());
        if (userAgent is null)
        {
            throw new DataSourceException(
                DataSourceErrorKind.ContactMissing,
                $"Enter a working contact email address in Settings, or set the {SecContact.EnvironmentVariable} environment variable. The SEC requires automated clients to identify themselves.");
        }

        CacheEntry? cached = _cache.TryRead(key);
        T cachedValue = default!;
        var haveCached = false;
        if (cached is not null)
        {
            try
            {
                cachedValue = parse(cached.Bytes);
                haveCached = true;
            }
            catch (DataSourceException)
            {
                _cache.Delete(key);
            }
        }

        if (haveCached)
        {
            var age = _time.GetUtcNow() - cached!.WrittenAt;
            if (age >= TimeSpan.Zero && age < maxAge)
            {
                return (cachedValue, cached.WrittenAt, false);
            }
        }

        try
        {
            var bytes = await FetchBytesAsync(url, userAgent, cancellationToken);
            var value = parse(bytes);
            TryWriteCache(key, bytes);
            return (value, _time.GetUtcNow(), false);
        }
        catch (DataSourceException ex) when (haveCached && CanServeStale(ex.Kind))
        {
            return (cachedValue, cached!.WrittenAt, true);
        }
    }

    private static bool CanServeStale(DataSourceErrorKind kind) =>
        kind is DataSourceErrorKind.Network
            or DataSourceErrorKind.RateLimited
            or DataSourceErrorKind.ServerError
            or DataSourceErrorKind.InvalidData;

    private void TryWriteCache(string key, byte[] bytes)
    {
        try
        {
            _cache.Write(key, bytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The cache is an optimisation; a failed write must not fail the load.
        }
    }

    private async Task<byte[]> FetchBytesAsync(string url, string userAgent, CancellationToken cancellationToken)
    {
        var uri = new Uri(url);
        if (uri.Scheme != Uri.UriSchemeHttps || uri.Host is not ("www.sec.gov" or "data.sec.gov"))
        {
            throw new DataSourceException(DataSourceErrorKind.InvalidData, "Refusing to request a non-SEC or non-HTTPS address.");
        }

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response;
            try
            {
                response = await SendPacedAsync(uri, userAgent, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new DataSourceException(DataSourceErrorKind.Network, "The SEC could not be reached.", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new DataSourceException(DataSourceErrorKind.Network, "The request to the SEC timed out.", ex);
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    return await ReadBoundedAsync(response, cancellationToken);
                }

                var status = (int)response.StatusCode;
                switch (status)
                {
                    case 403:
                        throw new DataSourceException(
                            DataSourceErrorKind.Forbidden,
                            $"The SEC rejected the request (403). Check that your contact email in Settings (or {SecContact.EnvironmentVariable}) is a working address.");
                    case 404:
                        throw new DataSourceException(DataSourceErrorKind.NotFound, "The SEC has no data at this address (404).");
                    case 429 or >= 500:
                        if (attempt >= _options.MaxRetries)
                        {
                            throw new DataSourceException(
                                status == 429 ? DataSourceErrorKind.RateLimited : DataSourceErrorKind.ServerError,
                                status == 429
                                    ? "The SEC is rate limiting requests (429). Try again shortly."
                                    : $"The SEC returned a server error ({status}).");
                        }

                        await _delay(RetryDelay(response, attempt), cancellationToken);
                        continue;
                    default:
                        throw new DataSourceException(DataSourceErrorKind.Network, $"The SEC returned an unexpected status ({status}).");
                }
            }
        }
    }

    private async Task<HttpResponseMessage> SendPacedAsync(Uri uri, string userAgent, CancellationToken cancellationToken)
    {
        await _pacing.WaitAsync(cancellationToken);
        try
        {
            var wait = _lastRequestStart + _options.MinRequestInterval - _time.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                await _delay(wait, cancellationToken);
            }

            _lastRequestStart = _time.GetUtcNow();

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        finally
        {
            _pacing.Release();
        }
    }

    private TimeSpan RetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        TimeSpan? requested = retryAfter?.Delta
            ?? (retryAfter?.Date is { } date ? date - _time.GetUtcNow() : null);

        var delay = requested ?? TimeSpan.FromTicks(
            (long)(_options.BaseBackoff.Ticks * Math.Pow(2, attempt) * (1 + Random.Shared.NextDouble() * 0.25)));

        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        return delay > _options.MaxRetryDelay ? _options.MaxRetryDelay : delay;
    }

    private async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var max = _options.MaxResponseBytes;
        if (response.Content.Headers.ContentLength is { } length && length > max)
        {
            throw TooLarge();
        }

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > max)
                {
                    throw TooLarge();
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new DataSourceException(DataSourceErrorKind.Network, "The connection to the SEC was interrupted.", ex);
        }
    }

    private static DataSourceException TooLarge() =>
        new(DataSourceErrorKind.InvalidData, "The SEC response exceeded the maximum allowed size.");
}
