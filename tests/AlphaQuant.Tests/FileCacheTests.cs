using System.Text;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public sealed class FileCacheTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aq-cache-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void RoundTripsBytesAndReportsTheWriteTime()
    {
        var cache = new FileCache(_root);

        cache.Write("companyfacts/CIK0000320193.json", Encoding.UTF8.GetBytes("{\"a\":1}"));
        var entry = cache.TryRead("companyfacts/CIK0000320193.json");

        Assert.NotNull(entry);
        Assert.Equal("{\"a\":1}", Encoding.UTF8.GetString(entry.Bytes));
        Assert.True(DateTimeOffset.UtcNow - entry.WrittenAt < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ReportsAMissingEntryAsNull()
    {
        Assert.Null(new FileCache(_root).TryRead("company_tickers.json"));
    }

    [Fact]
    public void OverwritingReplacesTheEntryAndLeavesNoTemporaryFiles()
    {
        var cache = new FileCache(_root);

        cache.Write("company_tickers.json", [1, 2, 3]);
        cache.Write("company_tickers.json", [9]);

        Assert.Equal([9], cache.TryRead("company_tickers.json")!.Bytes);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void ReflectsTheFileAgeSoExpiryCanBeDetected()
    {
        var cache = new FileCache(_root);
        cache.Write("company_tickers.json", [1]);
        File.SetLastWriteTimeUtc(Path.Combine(_root, "company_tickers.json"), DateTime.UtcNow.AddHours(-30));

        var entry = cache.TryRead("company_tickers.json");

        Assert.True(DateTimeOffset.UtcNow - entry!.WrittenAt > TimeSpan.FromHours(29));
    }

    [Fact]
    public void DeleteRemovesTheEntryAndToleratesMissingOnes()
    {
        var cache = new FileCache(_root);
        cache.Write("company_tickers.json", [1]);

        cache.Delete("company_tickers.json");
        cache.Delete("company_tickers.json");

        Assert.Null(cache.TryRead("company_tickers.json"));
    }

    [Theory]
    [InlineData("../escape.json")]
    [InlineData("a/../b.json")]
    [InlineData("/absolute.json")]
    [InlineData("C:/windows/x.json")]
    [InlineData("a//b.json")]
    [InlineData("")]
    [InlineData("name with space.json")]
    [InlineData("a\\b.json")]
    public void RejectsKeysThatCouldEscapeTheCacheFolder(string key)
    {
        var cache = new FileCache(_root);

        Assert.Throws<ArgumentException>(() => cache.Write(key, [1]));
        Assert.Throws<ArgumentException>(() => cache.TryRead(key));
    }
}
