using AlphaQuant.Infrastructure;
using AlphaQuant.Desktop.ViewModels;
using System.Text.Json;

namespace AlphaQuant.Tests;

public sealed class SafeDiagnosticsTests
{
    [Theory]
    [InlineData("/home/private/user/token-secret")]
    [InlineData("person@example.com")]
    [InlineData("1.0.0\nAuthorization: bearer secret")]
    public void Arbitrary_metadata_is_not_exported(string input)
    {
        var text = SafeDiagnostics.Capture(input);
        Assert.DoesNotContain(input, text);
        using var json = JsonDocument.Parse(text);
        Assert.Equal("unknown", json.RootElement.GetProperty("Version").GetString());
        Assert.Equal(7, json.RootElement.EnumerateObject().Count());
    }
    [Fact]
    public async Task Export_requires_preview_and_never_overwrites_user_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "aq-diagnostics-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var vm = new ReleaseNotesViewModel(() => { }); vm.ExportPath = Path.Combine(root, "diagnostics.json");
            await vm.ExportDiagnosticsCommand.ExecuteAsync(null); Assert.False(File.Exists(vm.ExportPath));
            vm.PreviewDiagnosticsCommand.Execute(null); await vm.ExportDiagnosticsCommand.ExecuteAsync(null);
            var original = await File.ReadAllTextAsync(vm.ExportPath); Assert.Equal(vm.DiagnosticPreview, original);
            vm.FeedbackPreview = "private research must not be exported";
            await vm.ExportDiagnosticsCommand.ExecuteAsync(null);
            Assert.Contains("failed", vm.DiagnosticStatus); Assert.Equal(original, await File.ReadAllTextAsync(vm.ExportPath));
            Assert.Single(Directory.GetFiles(root));
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public async Task Cancelled_document_write_preserves_existing_output_and_removes_owned_temp()
    {
        var root = Path.Combine(Path.GetTempPath(), "aq-file-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "output.txt"); await File.WriteAllTextAsync(path, "original");
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ResearchDocumentFile.SaveNewAsync(path, "replacement", cancelled.Token));
            Assert.Equal("original", await File.ReadAllTextAsync(path)); Assert.Single(Directory.GetFiles(root));
        }
        finally { Directory.Delete(root, true); }
    }
}
