using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using AlphaQuant.Tests.Support;

namespace AlphaQuant.Tests;

public class SecClientTests
{
    private const string Contact = "dev@example.org";
    private const string FactsKey = "companyfacts/CIK0000320193.json";
    private const string FactsUrl = "https://data.sec.gov/api/xbrl/companyfacts/CIK0000320193.json";

    private static readonly SecClientOptions Fast = new()
    {
        MinRequestInterval = TimeSpan.Zero,
        BaseBackoff = TimeSpan.Zero,
    };

    private sealed class Harness
    {
        public Harness(string? contact = Contact, SecClientOptions? options = null)
        {
            Cache = new FakeCache { Clock = Time };
            Client = new SecClient(
                new HttpClient(Handler),
                contact,
                Cache,
                options ?? Fast,
                Time,
                (span, _) =>
                {
                    Delays.Add(span);
                    return Task.CompletedTask;
                });
        }

        public FakeHandler Handler { get; } = new();

        public FakeTime Time { get; } = new();

        public FakeCache Cache { get; }

        public List<TimeSpan> Delays { get; } = [];

        public SecClient Client { get; }

        public void SeedFacts(byte[] bytes) =>
            Cache.Entries[FactsKey] = new CacheEntry(bytes, Time.GetUtcNow());
    }

    private static byte[] GoodFacts() => Fixtures.Read("companyfacts.sample.json");

    private static byte[] Garbage() => Encoding.UTF8.GetBytes("<html>not json</html>");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bogus")]
    public async Task MakesNoRequestWhenTheContactIsMissingOrInvalid(string? contact)
    {
        var h = new Harness(contact);

        var facts = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));
        var directory = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetCompaniesAsync(default));

        Assert.Equal(DataSourceErrorKind.ContactMissing, facts.Kind);
        Assert.Equal(DataSourceErrorKind.ContactMissing, directory.Kind);
        Assert.Contains(SecContact.EnvironmentVariable, facts.Message);
        Assert.Empty(h.Handler.Requests);
    }

    [Fact]
    public async Task SendsADeclaredUserAgentOverHttpsToTheFactsEndpointWithAPaddedCik()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(GoodFacts());

        var facts = await h.Client.GetFactsAsync(320193, default);

        var request = Assert.Single(h.Handler.Requests);
        Assert.Equal(FactsUrl, request.Url);
        Assert.Equal("AlphaQuant/0.1 (dev@example.org)", request.UserAgent);
        Assert.Equal("application/json", request.Accept);
        Assert.Equal("EXAMPLE CORP", facts.EntityName);
        Assert.Equal(7, facts.Facts.Count);
        Assert.False(facts.IsStale);
        Assert.Equal(h.Time.GetUtcNow(), facts.RetrievedAt);
        Assert.Equal(1, h.Cache.Writes);
    }

    [Fact]
    public async Task AppliesAContactChangedAtRuntimeWithoutRecreatingTheClient()
    {
        var handler = new FakeHandler();
        var time = new FakeTime();
        string? contact = null;
        var client = new SecClient(
            new HttpClient(handler),
            () => contact,
            new FakeCache { Clock = time },
            Fast,
            time,
            (_, _) => Task.CompletedTask);

        var before = await Assert.ThrowsAsync<DataSourceException>(() => client.GetFactsAsync(320193, default));
        Assert.Equal(DataSourceErrorKind.ContactMissing, before.Kind);
        Assert.Empty(handler.Requests);

        contact = "late@example.org";
        handler.EnqueueOk(GoodFacts());
        await client.GetFactsAsync(320193, default);

        Assert.Equal("AlphaQuant/0.1 (late@example.org)", Assert.Single(handler.Requests).UserAgent);

        contact = "changed@example.org";
        handler.EnqueueOk(Fixtures.Read("company_tickers.sample.json"));
        await client.GetCompaniesAsync(default);

        Assert.Equal("AlphaQuant/0.1 (changed@example.org)", handler.Requests[1].UserAgent);
    }

    [Fact]
    public async Task FetchesTheDirectoryFromTheSecTickerFile()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(Fixtures.Read("company_tickers.sample.json"));

        var directory = await h.Client.GetCompaniesAsync(default);

        Assert.Equal("https://www.sec.gov/files/company_tickers.json", Assert.Single(h.Handler.Requests).Url);
        Assert.Equal(3, directory.Companies.Count);
        Assert.True(h.Cache.Entries.ContainsKey("company_tickers.json"));
    }

    [Fact]
    public async Task ServesAFreshCacheEntryWithoutARequestAndRefetchesAfterExpiry()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(GoodFacts()).EnqueueOk(GoodFacts());

        await h.Client.GetFactsAsync(320193, default);
        await h.Client.GetFactsAsync(320193, default);
        Assert.Single(h.Handler.Requests);

        h.Time.Advance(TimeSpan.FromHours(25));
        var refreshed = await h.Client.GetFactsAsync(320193, default);

        Assert.Equal(2, h.Handler.Requests.Count);
        Assert.False(refreshed.IsStale);
    }

    [Fact]
    public async Task DeletesACorruptCacheEntryAndRefetches()
    {
        var h = new Harness();
        h.SeedFacts(Garbage());
        h.Handler.EnqueueOk(GoodFacts());

        var facts = await h.Client.GetFactsAsync(320193, default);

        Assert.Equal(1, h.Cache.Deletes);
        Assert.Single(h.Handler.Requests);
        Assert.Equal(7, facts.Facts.Count);
        Assert.Equal(GoodFacts(), h.Cache.Entries[FactsKey].Bytes);
    }

    [Fact]
    public async Task ServesTheStaleCacheEntryWhenTheNetworkFails()
    {
        var h = new Harness();
        h.SeedFacts(GoodFacts());
        var cachedAt = h.Time.GetUtcNow();
        h.Time.Advance(TimeSpan.FromHours(25));
        h.Handler.Enqueue(_ => throw new HttpRequestException("offline"));

        var facts = await h.Client.GetFactsAsync(320193, default);

        Assert.True(facts.IsStale);
        Assert.Equal(cachedAt, facts.RetrievedAt);
        Assert.Equal(7, facts.Facts.Count);
    }

    [Fact]
    public async Task ReportsANetworkErrorWhenThereIsNothingCached()
    {
        var h = new Harness();
        h.Handler.Enqueue(_ => throw new HttpRequestException("offline"));

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.Network, ex.Kind);
    }

    [Fact]
    public async Task TreatsATimeoutAsANetworkError()
    {
        var h = new Harness();
        h.Handler.Enqueue(_ => throw new TaskCanceledException("timed out"));

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.Network, ex.Kind);
    }

    [Fact]
    public async Task ForbiddenIsNeverRetriedAndNeverHiddenByStaleData()
    {
        var h = new Harness();
        h.SeedFacts(GoodFacts());
        h.Time.Advance(TimeSpan.FromHours(25));
        h.Handler.EnqueueStatus(HttpStatusCode.Forbidden);

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.Forbidden, ex.Kind);
        Assert.Contains(SecContact.EnvironmentVariable, ex.Message);
        Assert.Single(h.Handler.Requests);
    }

    [Fact]
    public async Task NotFoundMeansTheCompanyHasNoFacts()
    {
        var h = new Harness();
        h.Handler.EnqueueStatus(HttpStatusCode.NotFound);

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.NotFound, ex.Kind);
        Assert.Contains("no XBRL company facts", ex.Message);
        Assert.Single(h.Handler.Requests);
    }

    [Fact]
    public async Task RetriesAfterTooManyRequestsHonouringRetryAfter()
    {
        var h = new Harness();
        var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
        h.Handler.Enqueue(limited).EnqueueOk(GoodFacts());

        var facts = await h.Client.GetFactsAsync(320193, default);

        Assert.Equal(2, h.Handler.Requests.Count);
        Assert.Equal([TimeSpan.FromSeconds(2)], h.Delays);
        Assert.Equal(7, facts.Facts.Count);
    }

    [Fact]
    public async Task CapsAnExcessiveRetryAfter()
    {
        var h = new Harness();
        var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(10));
        h.Handler.Enqueue(limited).EnqueueOk(GoodFacts());

        await h.Client.GetFactsAsync(320193, default);

        Assert.Equal([TimeSpan.FromSeconds(30)], h.Delays);
    }

    [Fact]
    public async Task GivesUpAfterTheRetryLimitOnServerErrors()
    {
        var h = new Harness();
        for (var i = 0; i < 4; i++)
        {
            h.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        }

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.ServerError, ex.Kind);
        Assert.Equal(4, h.Handler.Requests.Count); // the first try plus three retries
        Assert.Equal(3, h.Delays.Count);
    }

    [Fact]
    public async Task ReportsRateLimitingOnceRetriesAreExhausted()
    {
        var h = new Harness();
        for (var i = 0; i < 4; i++)
        {
            h.Handler.EnqueueStatus(HttpStatusCode.TooManyRequests);
        }

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.RateLimited, ex.Kind);
    }

    [Fact]
    public async Task BacksOffExponentiallyWithoutRetryAfter()
    {
        var h = new Harness(options: new SecClientOptions
        {
            MinRequestInterval = TimeSpan.Zero,
            BaseBackoff = TimeSpan.FromSeconds(1),
        });
        h.Handler.EnqueueStatus(HttpStatusCode.BadGateway).EnqueueStatus(HttpStatusCode.BadGateway).EnqueueOk(GoodFacts());

        await h.Client.GetFactsAsync(320193, default);

        Assert.Equal(2, h.Delays.Count);
        Assert.InRange(h.Delays[0], TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.25));
        Assert.InRange(h.Delays[1], TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2.5));
    }

    [Fact]
    public async Task PacesRequestsToTheConfiguredInterval()
    {
        var h = new Harness(options: new SecClientOptions
        {
            MinRequestInterval = TimeSpan.FromSeconds(1),
            BaseBackoff = TimeSpan.Zero,
        });
        h.Handler.EnqueueOk(Fixtures.Read("company_tickers.sample.json")).EnqueueOk(GoodFacts());

        await h.Client.GetCompaniesAsync(default);
        await h.Client.GetFactsAsync(320193, default);

        Assert.Equal([TimeSpan.FromSeconds(1)], h.Delays);
    }

    [Fact]
    public async Task PropagatesCancellationBeforeAnyRequest()
    {
        var h = new Harness();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Client.GetFactsAsync(320193, cts.Token));

        Assert.Empty(h.Handler.Requests);
    }

    [Fact]
    public async Task PropagatesCancellationDuringARequestWithoutTouchingTheCache()
    {
        var h = new Harness();
        h.Handler.Enqueue(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Client.GetFactsAsync(320193, cts.Token));

        Assert.Equal(0, h.Cache.Writes);
    }

    [Fact]
    public async Task RejectsAResponseLargerThanTheConfiguredLimit()
    {
        var h = new Harness(options: new SecClientOptions
        {
            MinRequestInterval = TimeSpan.Zero,
            MaxResponseBytes = 10,
        });
        h.Handler.EnqueueOk(new byte[100]);

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.InvalidData, ex.Kind);
        Assert.Equal(0, h.Cache.Writes);
    }

    [Fact]
    public async Task InvalidJsonIsReportedAndNeverCached()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(Garbage());

        var ex = await Assert.ThrowsAsync<DataSourceException>(() => h.Client.GetFactsAsync(320193, default));

        Assert.Equal(DataSourceErrorKind.InvalidData, ex.Kind);
        Assert.Equal(0, h.Cache.Writes);
        Assert.Empty(h.Cache.Entries);
    }

    [Fact]
    public async Task AnInvalidResponseDoesNotOverwriteAGoodStaleEntry()
    {
        var h = new Harness();
        h.SeedFacts(GoodFacts());
        h.Time.Advance(TimeSpan.FromHours(25));
        h.Handler.EnqueueOk(Garbage());

        var facts = await h.Client.GetFactsAsync(320193, default);

        Assert.True(facts.IsStale);
        Assert.Equal(0, h.Cache.Writes);
        Assert.Equal(GoodFacts(), h.Cache.Entries[FactsKey].Bytes);
    }

    [Fact]
    public async Task RejectsANonPositiveCik()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => h.Client.GetFactsAsync(0, default));
    }
}
