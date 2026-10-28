using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

public sealed record OAuthBrokerStart(string SessionId, string AuthorizationUrl);
public enum OAuthPollState { Pending, Complete, Failed, Missing }
public sealed class OAuthBrokerPoll(OAuthPollState state, string? accessToken = null)
{
    public OAuthPollState State { get; } = state;
    public string? AccessToken { get; } = accessToken;
    public override string ToString() => State.ToString();
}

/// <summary>Server-side only: registered application credentials must never enter the desktop configuration.</summary>
public sealed class OAuthBrokerService
{
    private sealed class Pending(string challenge, DateTimeOffset expires)
    {
        public string Challenge = challenge;
        public DateTimeOffset Expires = expires;
        public bool Completing;
        public bool Failed;
        public string? Token;
    }
    private readonly Dictionary<string, Pending> _pending = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _secret;
    private readonly Uri _redirect;
    private readonly TimeProvider _time;

    public OAuthBrokerService(HttpClient http, string clientId, string clientSecret, Uri redirect, TimeProvider? time = null)
    {
        AlpacaBrowserOAuth.ValidateBroker(redirect);
        if (redirect.AbsolutePath != "/oauth/callback" || redirect.Query.Length != 0 || redirect.Fragment.Length != 0
            || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("Configure a registered client and the exact /oauth/callback redirect.");
        _http = http; _clientId = clientId; _secret = clientSecret; _redirect = redirect; _time = time ?? TimeProvider.System;
    }

    public OAuthBrokerStart Start(string challenge)
    {
        if (!Valid(challenge)) throw new ArgumentException("Invalid challenge.");
        lock (_gate)
        {
            Purge();
            if (_pending.Count >= 256) throw new InvalidOperationException("Authorization service is busy.");
            var id = AlpacaBrowserOAuth.Base64(RandomNumberGenerator.GetBytes(32));
            _pending.Add(id, new(challenge, _time.GetUtcNow().AddMinutes(5)));
            var url = "https://app.alpaca.markets/oauth/authorize?response_type=code&scope=data&env=paper"
                + $"&client_id={Uri.EscapeDataString(_clientId)}&redirect_uri={Uri.EscapeDataString(_redirect.AbsoluteUri)}&state={id}";
            return new(id, url);
        }
    }

    public async Task CompleteAsync(string state, string? code, bool denied, CancellationToken cancellationToken)
    {
        Pending pending;
        lock (_gate)
        {
            Purge();
            if (!_pending.TryGetValue(state, out pending!) || pending.Completing || pending.Failed || pending.Token is not null)
                return;
            pending.Completing = true;
        }
        string? token = null;
        try
        {
            if (denied || string.IsNullOrWhiteSpace(code) || code.Length > 2048) return;
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.alpaca.markets/oauth/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code", ["code"] = code, ["client_id"] = _clientId,
                    ["client_secret"] = _secret, ["redirect_uri"] = _redirect.AbsoluteUri
                })
            };
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode) return;
            using var json = await AlpacaBrowserOAuth.ReadBoundedAsync(response, cancellationToken);
            var root = json.RootElement;
            if (!root.TryGetProperty("token_type", out var kind) || !string.Equals(kind.GetString(), "bearer", StringComparison.OrdinalIgnoreCase)
                || !root.TryGetProperty("scope", out var scope) || scope.GetString() != "data"
                || !root.TryGetProperty("access_token", out var access)) return;
            var candidate = access.GetString();
            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 8192 && !candidate.Any(char.IsControl)) token = candidate;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or MarketDataException) { }
        finally
        {
            lock (_gate)
            {
                if (_pending.TryGetValue(state, out var existing) && ReferenceEquals(existing, pending))
                { existing.Token = token; existing.Failed = token is null; }
            }
        }
    }

    public OAuthBrokerPoll Poll(string id, string verifier)
    {
        lock (_gate)
        {
            Purge();
            if (!TryOwned(id, verifier, out var entry)) return new(OAuthPollState.Missing);
            if (entry.Failed) { _pending.Remove(id); return new(OAuthPollState.Failed); }
            if (entry.Token is null) return new(OAuthPollState.Pending);
            _pending.Remove(id);
            return new(OAuthPollState.Complete, entry.Token);
        }
    }

    public void Cancel(string id, string verifier)
    { lock (_gate) { Purge(); if (TryOwned(id, verifier, out _)) _pending.Remove(id); } }

    private bool TryOwned(string id, string verifier, out Pending entry)
    {
        entry = null!;
        if (!Valid(id) || !Valid(verifier) || !_pending.TryGetValue(id, out var found)) return false;
        var hash = AlpacaBrowserOAuth.Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash), Encoding.ASCII.GetBytes(found.Challenge))) return false;
        entry = found;
        return true;
    }
    private void Purge()
    { foreach (var id in _pending.Where(p => p.Value.Expires <= _time.GetUtcNow()).Select(p => p.Key).ToArray()) _pending.Remove(id); }
    public void ExpireSessions() { lock (_gate) { Purge(); } }
    private static bool Valid(string? s) => s is { Length: 43 } && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    public override string ToString() => "Server-side OAuth broker (credentials redacted)";
}
