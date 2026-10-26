using System.Text.RegularExpressions;

namespace AlphaQuant.Infrastructure;

public sealed record CacheEntry(byte[] Bytes, DateTimeOffset WrittenAt);

public interface ISecCache
{
    CacheEntry? TryRead(string key);

    /// <summary>Stores bytes without ever leaving a partially written entry behind.</summary>
    void Write(string key, byte[] bytes);

    void Delete(string key);
}

/// <summary>
/// Plain-file cache. Keys are relative paths such as <c>companyfacts/CIK0000320193.json</c>. Entries are written
/// to a temporary file and moved into place, so a failed write keeps the previous usable entry.
/// </summary>
public sealed partial class FileCache : ISecCache
{
    private readonly string _root;

    public FileCache(string root)
    {
        _root = Path.GetFullPath(root);
    }

    public static string DefaultRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AlphaQuant",
        "cache",
        "sec");

    public CacheEntry? TryRead(string key)
    {
        var path = PathFor(key);
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            return new CacheEntry(bytes, new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Write(string key, byte[] bytes)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    public void Delete(string key)
    {
        TryDelete(PathFor(key));
    }

    private string PathFor(string key)
    {
        if (!KeyPattern().IsMatch(key))
        {
            throw new ArgumentException($"Invalid cache key '{key}'.", nameof(key));
        }

        var path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid cache key '{key}'.", nameof(key));
        }

        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: a stuck temp or corrupt file is replaced on the next successful write.
        }
    }

    // Segments of letters, digits, '_' and '-', joined by '.' inside a segment and '/' between segments.
    // No empty segments, so '..' and absolute paths cannot match.
    [GeneratedRegex(@"^[A-Za-z0-9_\-]+(\.[A-Za-z0-9_\-]+)*(/[A-Za-z0-9_\-]+(\.[A-Za-z0-9_\-]+)*)*$")]
    private static partial Regex KeyPattern();
}
