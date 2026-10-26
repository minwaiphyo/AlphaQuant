using System.Net;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests.Support;

internal sealed record SeenRequest(string Url, string? UserAgent, string? Accept);

/// <summary>Returns queued responses in order and records what was requested. Never touches the network.</summary>
internal sealed class FakeHandler : HttpMessageHandler
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public List<SeenRequest> Requests { get; } = [];

    public FakeHandler Enqueue(HttpResponseMessage response)
    {
        _responses.Enqueue(_ => Task.FromResult(response));
        return this;
    }

    public FakeHandler Enqueue(Func<CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responses.Enqueue(responder);
        return this;
    }

    public FakeHandler EnqueueOk(byte[] body) => Enqueue(Ok(body));

    public FakeHandler EnqueueStatus(HttpStatusCode status) => Enqueue(new HttpResponseMessage(status));

    public static HttpResponseMessage Ok(byte[] body) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new SeenRequest(
            request.RequestUri!.AbsoluteUri,
            request.Headers.TryGetValues("User-Agent", out var agent) ? string.Join(" ", agent) : null,
            request.Headers.Accept.FirstOrDefault()?.ToString()));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"Unexpected request to {request.RequestUri}.");
        }

        return _responses.Dequeue()(cancellationToken);
    }
}

internal sealed class FakeCache : ISecCache
{
    public Dictionary<string, CacheEntry> Entries { get; } = [];

    public int Writes { get; private set; }

    public int Deletes { get; private set; }

    public CacheEntry? TryRead(string key) => Entries.GetValueOrDefault(key);

    public void Write(string key, byte[] bytes)
    {
        Writes++;
        Entries[key] = new CacheEntry(bytes, Clock.GetUtcNow());
    }

    public void Delete(string key)
    {
        Deletes++;
        Entries.Remove(key);
    }

    public FakeTime Clock { get; init; } = new();
}

internal sealed class FakeTime : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan span) => _now += span;
}
