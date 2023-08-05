using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace NLog.Contrib.Targets.WebSocketServer.UnitTests;

public class EmbeddedFileHelper_Tests
{
    private static readonly string ViewerNameSpace = $"{typeof(EmbeddedFileHelper).Namespace}.ViewerSpa.";

    [Fact]
    public async Task GetFileAsync__ShouldReturnEmbeddedFile()
    {
        // Arrange
        const string expectedMime = "text/plain; charset=utf-8";
        var expected = await EmbeddedFileHelper_Tests.GetResourceFileAsync($"{EmbeddedFileHelper_Tests.ViewerNameSpace}3rdpartylicenses.txt");

        // Act
        var (mime, content) = await EmbeddedFileHelper.GetFileAsync("ViewerSpa", "3rdpartylicenses.txt", "index.html");

        // Assert
        mime.Should().Be(expectedMime);
        content.Should().Be(expected);
    }

    [Fact]
    public async Task GetFileAsync__ShouldReturnAll()
    {
        // Arrange
        var fileNames = typeof(EmbeddedFileHelper).Assembly.GetManifestResourceNames()
                                                  .Where(a => a.StartsWith(EmbeddedFileHelper_Tests.ViewerNameSpace))
                                                  .Select(a => a.Remove(0, EmbeddedFileHelper_Tests.ViewerNameSpace.Length))
                                                  .ToImmutableList();
        var expected = await Task.WhenAll(
                           fileNames.Select(
                               async a => (Name: a, File: await EmbeddedFileHelper_Tests.GetResourceFileAsync(EmbeddedFileHelper_Tests.ViewerNameSpace + a))));

        // Act
        var result = await Task.WhenAll(
                         fileNames.Select(
                             async a => (Name: a, File: (await EmbeddedFileHelper.GetFileAsync("ViewerSpa", a, "index.html")).Content)));

        // Assert
        result.Should().Equal(expected);
    }

    [Fact]
    public async Task GetFileAsync_IndexHtml_ShouldReplaceBaseHrefForSubfolders()
    {
        // Arrange
        const string expectedMime = "text/html; charset=utf-8";
        var expected = await EmbeddedFileHelper_Tests.GetResourceFileAsync($"{EmbeddedFileHelper_Tests.ViewerNameSpace}index.html");
        expected = expected.Replace("""<base href="/">""", """<base href="/pretty/deep/">""");

        // Act
        var (mime, content) = await EmbeddedFileHelper.GetFileAsync("ViewerSpa", "/pretty/deep/index.html", "/index.html");

        // Assert
        mime.Should().Be(expectedMime);
        content.Should().Be(expected);
    }

    private static async Task<string> GetResourceFileAsync(string fullName)
    {
        await using var stream = typeof(EmbeddedFileHelper).Assembly.GetManifestResourceStream(fullName) ?? throw new FileNotFoundException();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
