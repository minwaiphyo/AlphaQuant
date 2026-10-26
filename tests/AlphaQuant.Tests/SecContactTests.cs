using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public class SecContactTests
{
    [Fact]
    public void BuildsADeclaredUserAgentFromAValidEmail()
    {
        Assert.Equal("AlphaQuant/0.1 (dev@example.org)", SecContact.TryBuildUserAgent("dev@example.org"));
        Assert.Equal("AlphaQuant/0.1 (a.b+c@sub.example.co)", SecContact.TryBuildUserAgent("  a.b+c@sub.example.co  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("a b@example.org")]
    [InlineData("x(y)@example.org")]
    [InlineData("a@example.org (extra)")]
    public void RejectsMissingOrInvalidContacts(string? contact)
    {
        Assert.Null(SecContact.TryBuildUserAgent(contact));
    }
}
