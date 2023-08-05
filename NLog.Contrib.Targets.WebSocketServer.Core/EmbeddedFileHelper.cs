using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NLog.Contrib.Targets.WebSocketServer.Core;

public class EmbeddedFileHelper
{
    private static readonly (string File, string Mime)[] ContentTypes =
    {
        (".html", "text/html; charset=utf-8"),
        (".js", "application/javascript; charset=utf-8"),
        (".css", "text/css; charset=utf-8"),
        (".txt", "text/plain; charset=utf-8")
    };

    public static async Task<File> GetFileAsync(string embeddedFolder, string requestPath, string indexHtml)
    {
        var resourceSuffix = requestPath.Split('/').Last();
        var contentType = EmbeddedFileHelper.ContentTypes.FirstOrDefault(a => requestPath.EndsWith(a.File)).Mime
                          ?? throw new FileNotFoundException($"Mime type for '{resourceSuffix}' not found.");

        var resourceId = $"{typeof(EmbeddedFileHelper).Namespace}.{embeddedFolder}.{resourceSuffix}";

        try
        {
            await using var stream = typeof(EmbeddedFileHelper).Assembly.GetManifestResourceStream(resourceId) ?? throw new FileNotFoundException();
            using var sr = new StreamReader(stream);
            var content = await sr.ReadToEndAsync();
            if (string.Equals("/" + resourceSuffix, indexHtml, StringComparison.OrdinalIgnoreCase))
            {
                content = content.Replace(
                    """<base href="/">""",
                    $"""<base href="{requestPath.Remove(requestPath.Length - indexHtml.Length)}/">""",
                    StringComparison.OrdinalIgnoreCase);
            }

            return new File(contentType, content);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Embedded resource '{resourceSuffix}' not found.", ex);
        }
    }

    public record File(string ContentType, string Content);
}
