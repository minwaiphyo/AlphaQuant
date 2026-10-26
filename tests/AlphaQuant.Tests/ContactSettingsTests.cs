using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public sealed class ContactSettingsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aq-settings-tests-" + Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_root, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private ContactSettings Create(string? environment = null, string? path = null) =>
        new(path ?? SettingsPath, () => environment);

    [Fact]
    public void WithNothingSavedAndNoEnvironmentThereIsNoContact()
    {
        var settings = Create();

        Assert.Null(settings.SavedContact);
        Assert.Null(settings.EffectiveContact);
        Assert.Equal(ContactSource.None, settings.Source);
    }

    [Fact]
    public void FallsBackToTheEnvironmentVariable()
    {
        var settings = Create(environment: "  env@example.org ");

        Assert.Equal("env@example.org", settings.EffectiveContact);
        Assert.Equal(ContactSource.Environment, settings.Source);
        Assert.Null(settings.SavedContact);
    }

    [Fact]
    public void IgnoresAnInvalidEnvironmentValue()
    {
        var settings = Create(environment: "not-an-email");

        Assert.Null(settings.EffectiveContact);
        Assert.Equal(ContactSource.None, settings.Source);
    }

    [Fact]
    public void SavesAValidContactAndAnotherInstanceReadsItBack()
    {
        var result = Create().Save("  me@example.org ");

        Assert.True(result.Success);
        var reloaded = Create();
        Assert.Equal("me@example.org", reloaded.SavedContact);
        Assert.Equal("me@example.org", reloaded.EffectiveContact);
        Assert.Equal(ContactSource.Saved, reloaded.Source);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public void ASavedContactWinsOverTheEnvironment()
    {
        var settings = Create(environment: "env@example.org");

        settings.Save("saved@example.org");

        Assert.Equal("saved@example.org", settings.EffectiveContact);
        Assert.Equal(ContactSource.Saved, settings.Source);
    }

    [Fact]
    public void ABlankSaveClearsTheSavedContactAndFallsBackToTheEnvironment()
    {
        var settings = Create(environment: "env@example.org");
        settings.Save("saved@example.org");

        var result = settings.Save("   ");

        Assert.True(result.Success);
        Assert.Null(settings.SavedContact);
        Assert.Equal("env@example.org", settings.EffectiveContact);
        Assert.Null(Create().SavedContact);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("two words@example.org")]
    [InlineData("x(y)@example.org")]
    public void RejectsAnInvalidEmailWithoutChangingAnything(string input)
    {
        var settings = Create();
        settings.Save("good@example.org");

        var result = settings.Save(input);

        Assert.False(result.Success);
        Assert.Contains("valid email", result.Message);
        Assert.Equal("good@example.org", settings.SavedContact);
        Assert.Equal("good@example.org", Create().SavedContact);
    }

    [Fact]
    public void AnInvalidSaveCreatesNoFile()
    {
        Create().Save("nope");

        Assert.False(File.Exists(SettingsPath));
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"version\":1}")]
    [InlineData("{\"version\":1,\"secContact\":42}")]
    [InlineData("{\"version\":2,\"secContact\":\"future@example.org\"}")]
    [InlineData("{\"version\":1,\"secContact\":\"hand edited not an email\"}")]
    public void AnUnreadableOrIncompatibleFileMeansNothingIsSaved(string content)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(SettingsPath, content);

        var settings = Create(environment: "env@example.org");

        Assert.Null(settings.SavedContact);
        Assert.Equal("env@example.org", settings.EffectiveContact);
    }

    [Fact]
    public void ReportsAFailedWriteAndKeepsTheCurrentValue()
    {
        Directory.CreateDirectory(_root);
        var blocker = Path.Combine(_root, "blocker");
        File.WriteAllText(blocker, "a file where a folder is needed");
        var settings = Create(path: Path.Combine(blocker, "settings.json"));

        var result = settings.Save("me@example.org");

        Assert.False(result.Success);
        Assert.Contains("could not be saved", result.Message);
        Assert.Null(settings.SavedContact);
    }

    [Fact]
    public void WritesAVersionedFile()
    {
        Create().Save("me@example.org");

        var text = File.ReadAllText(SettingsPath);
        Assert.Contains("\"version\": 1", text);
        Assert.Contains("\"secContact\": \"me@example.org\"", text);
    }
}
