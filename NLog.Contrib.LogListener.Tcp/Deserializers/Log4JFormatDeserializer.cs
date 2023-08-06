using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NLog.Contrib.LogListener.Tcp.Deserializers;

public class Log4JFormatDeserializer : IFormatDeserializer
{
    /// <summary>
    /// Regex any for multiline.
    /// </summary>
    private const string Any = "(.|\n|\r)";

    private readonly Regex _matcher = new(
        $"<log4j:event{Log4JFormatDeserializer.Any}*?/log4j:event>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public Range GetChunk(ExtractInput input)
    {
        var match = this._matcher.Match(input.DataString);
        if (!match.Success)
        {
            return default;
        }

        return new Range(match.Index, match.Length);
    }

    public LogEventInfo Deserialize(ExtractInput input, Range chunk)
    {
        const string log4JNamespace = "http://logging.apache.org/log4j/2.0/events";
        var slice = input.DataString.Substring(chunk.Start.Value, chunk.End.Value);
        using var stringReader = new StringReader(slice);
        using var xmlReader = Log4JFormatDeserializer.CreateXmlReader(stringReader, ("log4j", log4JNamespace));
        var xml = XElement.Load(xmlReader);
        var messageXName = XName.Get("message", log4JNamespace);
        var propertiesXName = XName.Get("properties", log4JNamespace);
        var propertiesDataXName = XName.Get("data", log4JNamespace);
        var log4Jevent = new
        {
            Level = LogLevel.FromString(xml.Attribute("level")?.Value ?? throw new KeyNotFoundException("Attribute: level")),
            Logger = xml.Attribute("logger")?.Value ?? throw new KeyNotFoundException("Attribute: logger"),
            Message = xml.Element(messageXName)?.Value ?? throw new KeyNotFoundException("Element: message"),
            Properties = xml.Element(propertiesXName)?.Elements(propertiesDataXName)
                            .ToDictionary(
                                x => x.Attribute("name")?.Value ?? throw new KeyNotFoundException("Attribute: name"),
                                x => x.Attribute("value")?.Value)
                         ?? new Dictionary<string, string?>()
        };

        log4Jevent.Properties.TryGetValue("log4jmachinename", out var sender);
        sender ??= "Unknown";

        log4Jevent.Properties.TryGetValue("log4japp", out var app);
        app ??= "Unknown";

        var fancyLoggerName = $"App={app.Replace('.', ':')}.Machine={sender}.{log4Jevent.Logger}";
        var result = LogEventInfo.Create(log4Jevent.Level, fancyLoggerName, log4Jevent.Message);
        foreach (var property in log4Jevent.Properties)
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
}
