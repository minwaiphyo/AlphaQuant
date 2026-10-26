using AlphaQuant.Core;

namespace AlphaQuant.Tests;

public class FilingLinksTests
{
    [Fact]
    public void BuildsTheAccessionFolderWithoutDashes()
    {
        var uri = FilingLinks.TryBuild(320193, "0000320193-24-000123");

        Assert.Equal("https://www.sec.gov/Archives/edgar/data/320193/000032019324000123/", uri?.AbsoluteUri);
        Assert.True(FilingLinks.IsSafeToOpen(uri!));
    }

    [Theory]
    [InlineData(0, "0000320193-24-000123")]
    [InlineData(-5, "0000320193-24-000123")]
    [InlineData(320193, "")]
    [InlineData(320193, "000032019324000123")]
    [InlineData(320193, "0000320193-24-000123/../../x")]
    public void RejectsInvalidInputs(long cik, string accession)
    {
        Assert.Null(FilingLinks.TryBuild(cik, accession));
    }

    [Theory]
    [InlineData("http://www.sec.gov/Archives/")]
    [InlineData("https://evil.example.com/www.sec.gov/")]
    [InlineData("https://www.sec.gov.evil.example.com/")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    public void RefusesToOpenAnythingButHttpsSecLinks(string url)
    {
        Assert.False(FilingLinks.IsSafeToOpen(new Uri(url)));
    }
}
