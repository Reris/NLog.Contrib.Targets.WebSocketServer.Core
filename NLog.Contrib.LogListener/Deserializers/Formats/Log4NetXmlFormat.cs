using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

[FormatDiscriminator(Log4NetXmlFormat.Discriminator, typeof(Options))]
public class Log4NetXmlFormat : IFormat
{
    public const string Discriminator = "log4netxml";

    /// <summary>
    /// Regex any for multiline.
    /// </summary>
    private const string Any = "(.|\n|\r)";
    private static readonly byte[] ValidStart = "<log4net"u8.ToArray();

    private readonly Regex _matcher = new(
        $"<log4net:event{Log4NetXmlFormat.Any}*?/log4net:event>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public string GetDiscriminator() => Log4JXmlFormat.Discriminator;
    
    public bool HasValidStart(ExtractInput input)
    {
        for (var i = 0; i < input.Data.Span.Length; i++)
        {
            var b = input.Data.Span[i];
            switch ((char)b)
            {
                case '<':
                    return input.Data.Span[i..(i + Log4NetXmlFormat.ValidStart.Length)].SequenceEqual(Log4NetXmlFormat.ValidStart);
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
        var log4NetNamespace = "http://logging.apache.org/log4net/schemas/log4net-events-1.2/";
        var span = input.DataString.Substring(slice.Start.Value, slice.End.Value);
        using var stringReader = new StringReader(span);
        using var xmlReader = Log4NetXmlFormat.CreateXmlReader(stringReader, ("log4net", log4NetNamespace));
        var xml = XElement.Load(xmlReader);
        log4NetNamespace = xml.Name.NamespaceName;
        
        var messageXName = XName.Get("message", log4NetNamespace);
        var propertiesXName = XName.Get("properties", log4NetNamespace);
        var propertiesDataXName = XName.Get("data", log4NetNamespace);

        var level = FormatHelper.ParseLogLevel(xml.Attribute("level")?.Value ?? throw new KeyNotFoundException("Attribute: level"));
        var logger = xml.Attribute("logger")?.Value ?? throw new KeyNotFoundException("Attribute: logger");
        var message = xml.Element(messageXName)?.Value ?? throw new KeyNotFoundException("Element: message");
        var properties = xml.Element(propertiesXName)?.Elements(propertiesDataXName)
                            .ToDictionary(
                                x => x.Attribute("name")?.Value ?? throw new KeyNotFoundException("Attribute: name"),
                                x => x.Attribute("value")?.Value)
                         ?? DictionaryHelper<string, string?>.Empty;

        properties.RenameKey("log4net:HostName", "@mn");
        properties.RenameKey("ProcessName", "@pn");

        var result = LogEventInfo.Create(level, logger, message);
        if (DateTime.TryParse(xml.Attribute("timestamp")?.Value , out var timestamp))
        {
            result.TimeStamp = timestamp;
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
        public override string GetDiscriminator() => Log4NetXmlFormat.Discriminator;
    }
}
