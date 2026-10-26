using System.Text;
using System.Text.Json;

namespace AlphaQuant.Infrastructure;

public sealed record SettingsSaveResult(bool Success, string Message);

public enum ContactSource
{
    None,
    Saved,
    Environment,
}

/// <summary>
/// The SEC contact email, editable in the app and stored in a small local settings file. A valid saved contact
/// wins; otherwise the <c>AQ_SEC_CONTACT</c> environment variable is used. The file is user-owned and local,
/// never part of the repository, and a missing or unreadable file simply means nothing is saved.
/// </summary>
public sealed class ContactSettings
{
    private const int FileVersion = 1;

    private readonly string _path;
    private readonly Func<string?> _environment;
    private string? _saved;

    public ContactSettings(string path, Func<string?>? environment = null)
    {
        _path = path;
        _environment = environment ?? SecContact.FromEnvironment;
        _saved = Load(path);
    }

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AlphaQuant",
        "settings.json");

    public string FilePath => _path;

    /// <summary>The contact saved in the settings file, or null.</summary>
    public string? SavedContact => _saved;

    /// <summary>The contact the app uses for SEC requests, or null when none is valid.</summary>
    public string? EffectiveContact => IsValid(_saved)
        ? _saved
        : IsValid(_environment()) ? _environment()!.Trim() : null;

    public ContactSource Source => IsValid(_saved)
        ? ContactSource.Saved
        : IsValid(_environment()) ? ContactSource.Environment : ContactSource.None;

    /// <summary>Saves a valid email, or clears the saved contact when blank. Nothing changes on failure.</summary>
    public SettingsSaveResult Save(string? contact)
    {
        var trimmed = contact?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Persist(null, "Saved contact cleared.");
        }

        if (!IsValid(trimmed))
        {
            return new SettingsSaveResult(false, "Enter a valid email address, for example name@example.com.");
        }

        return Persist(trimmed, "Saved. The SEC contact applies to the next request.");
    }

    private SettingsSaveResult Persist(string? contact, string successMessage)
    {
        var temp = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            var json = JsonSerializer.Serialize(
                new { version = FileVersion, secContact = contact },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temp, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                // Best effort; the temp file is unused.
            }

            return new SettingsSaveResult(false, $"The setting could not be saved: {ex.Message}");
        }

        _saved = contact;
        return new SettingsSaveResult(true, successMessage);
    }

    private static bool IsValid(string? contact) =>
        SecContact.TryBuildUserAgent(contact) is not null;

    private static string? Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version)
                || !version.TryGetInt32(out var number)
                || number != FileVersion
                || !root.TryGetProperty("secContact", out var contact)
                || contact.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = contact.GetString()?.Trim();
            return IsValid(value) ? value : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }
}
