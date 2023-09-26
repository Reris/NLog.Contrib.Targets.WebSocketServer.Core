using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

[FormatDiscriminator(Log4JXmlFormat.Discriminator, typeof(Options))]
public class Log4JXmlFormat : IFormat
{
    public const string Discriminator = "log4jxml";

    /// <summary>
    /// Regex any for multiline.
    /// </summary>
    private const string Any = "(.|\n|\r)";

    private static readonly byte[] ValidStart = "<log4j"u8.ToArray();

    private readonly Regex _matcher = new(
        $"<log4j:event{Log4JXmlFormat.Any}*?/log4j:event>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public string GetDiscriminator() => Log4JXmlFormat.Discriminator;

    public bool HasValidStart(ExtractInput input)
    {
        for (var i = 0; i < input.Data.Span.Length; i++)
        {
            var c = input.Data.Span[i];
            switch ((char)c)
            {
                case '<':
                    return input.Data.Span[i..(i + Log4JXmlFormat.ValidStart.Length)].SequenceEqual(Log4JXmlFormat.ValidStart);
                case ' ':
                case '\n':
                case '\r':
                case '\t':
                    continue;
                default:
                {
                    return false;
                }
            }
        }

        return false;
    }

    public Range GetSlice(ExtractInput input)
    {
        var match = this._matcher.Match(input.DataString);
        if (!match.Success)
        {
            return default;
        }

        return new Range(match.Index, match.Length);
    }

    public LogEventInfo Deserialize(ExtractInput input, Range slice)
    {
        var log4JNamespace = "http://logging.apache.org/log4j/2.0/events";
        var span = input.DataString.Substring(slice.Start.Value, slice.End.Value);
        using var stringReader = new StringReader(span);
        using var xmlReader = Log4JXmlFormat.CreateXmlReader(stringReader, ("log4j", log4JNamespace));
        var xml = XElement.Load(xmlReader);
        log4JNamespace = xml.Name.NamespaceName;

        var messageXName = XName.Get("message", log4JNamespace);
        var propertiesXName = XName.Get("properties", log4JNamespace);
        var propertiesDataXName = XName.Get("data", log4JNamespace);

        var level = FormatHelper.ParseLogLevel(xml.Attribute("level")?.Value ?? throw new KeyNotFoundException("Attribute: level"));
        var logger = xml.Attribute("logger")?.Value ?? throw new KeyNotFoundException("Attribute: logger");
        var message = xml.Element(messageXName)?.Value ?? throw new KeyNotFoundException("Element: message");
        var properties = xml.Element(propertiesXName)?.Elements(propertiesDataXName)
                            .ToDictionary(
                                x => x.Attribute("name")?.Value ?? throw new KeyNotFoundException("Attribute: name"),
                                x => x.Attribute("value")?.Value)
                         ?? DictionaryHelper<string, string?>.Empty;

        properties.RenameKey("log4jmachinename", "@mn");
        properties.RenameKey("log4japp", "@pn");

        var result = LogEventInfo.Create(level, logger, message);
        if (long.TryParse(xml.Attribute("timestamp")?.Value, out var timestamp))
        {
            result.TimeStamp = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        }

        foreach (var property in properties)
        {
            result.Properties[property.Key] = property.Value;
        }

        return result;
    }

    private static XmlReader CreateXmlReader(TextReader reader, params (string Key, string Url)[] knownNamespaces)
    {
        var settings = new XmlReaderSettings
        {
            NameTable = new NameTable()
        };
        var xmlns = new XmlNamespaceManager(settings.NameTable);
        foreach (var (key, url) in knownNamespaces)
        {
            xmlns.AddNamespace(key, url);
        }

        var context = new XmlParserContext(null, xmlns, string.Empty, XmlSpace.Default);
        return XmlReader.Create(reader, settings, context);
    }

    public record Options : FormatOptions
    {
        public override string GetDiscriminator() => Log4JXmlFormat.Discriminator;
    }
}
