using AlphaQuant.Infrastructure;
namespace AlphaQuant.Tests;
public sealed class LocalDataPathsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("relative/data")]
    public void Missing_or_relative_OS_roots_fail_closed_instead_of_using_checkout(string root)
        =>Assert.Throws<InvalidOperationException>(()=>LocalDataPaths.AtRoot(root));
    [Fact]
    public void New_absolute_data_location_does_not_require_preexisting_directory()
    {
        var root=Path.Combine(Path.GetTempPath(),"aq-new-data-"+Guid.NewGuid());Assert.False(Directory.Exists(root));
        var resolved=LocalDataPaths.AtRoot(root);Assert.Equal(Path.Combine(root,"AlphaQuant"),resolved);Assert.True(Path.IsPathFullyQualified(resolved));Assert.False(Directory.Exists(root));
    }
}
