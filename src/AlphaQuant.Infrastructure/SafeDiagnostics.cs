using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AlphaQuant.Infrastructure;

/// <summary>Allowlisted metadata only: never accepts logs, errors, settings, paths or research.</summary>
public static class SafeDiagnostics
{
    public static string Capture(string? version) => JsonSerializer.Serialize(new
    {
        Schema = 1,
        Application = "AlphaQuant",
        Version = version is not null && Regex.IsMatch(version, @"\A[0-9]+\.[0-9]+\.[0-9]+(?:[.+-][A-Za-z0-9.-]{1,80})?\z") ? version : "unknown",
        Platform = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : OperatingSystem.IsMacOS() ? "macOS" : "Other",
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        RuntimeMajor = Environment.Version.Major,
        Scope = "Safe local metadata. No settings, contact, headers, paths, credentials, logs or research included."
    }, new JsonSerializerOptions { WriteIndented = true });

    public static string FeedbackDraft(string diagnostics) =>
        "AlphaQuant feedback draft — review and submit yourself. Nothing is sent automatically.\n\n"
        + "Task attempted:\nExpected behavior:\nObserved behavior:\nSteps to reproduce:\n\n"
        + "Remove personal information, credentials and research before sharing.\n\n" + diagnostics;
}

/// <summary>Write a new UTF-8 document atomically without overwriting existing user output.</summary>
public static class ResearchDocumentFile
{
    public static async Task SaveNewAsync(string path, string text, CancellationToken token = default)
    {
        if (Encoding.UTF8.GetByteCount(text) > 4 * 1024 * 1024) throw new ArgumentException("Document exceeds 4 MiB.");
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(Path.GetDirectoryName(full))) throw new IOException("Choose an existing output directory.");
        var temp = full + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
            {
                var data = Encoding.UTF8.GetBytes(text);
                await stream.WriteAsync(data, token);
                await stream.FlushAsync(token);
                stream.Flush(flushToDisk: true);
            }
            token.ThrowIfCancellationRequested();
            File.Move(temp, full, overwrite: false);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
