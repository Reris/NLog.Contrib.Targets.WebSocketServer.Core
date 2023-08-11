using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public class Log4JXmlFormat : IFormat
{
    /// <summary>
    /// Regex any for multiline.
    /// </summary>
    private const string Any = "(.|\n|\r)";

    private readonly Regex _matcher = new(
        $"<log4j:event{Log4JXmlFormat.Any}*?/log4j:event>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public string Name => "log4jxml";

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
        using var xmlReader = Log4JXmlFormat.CreateXmlReader(stringReader, ("log4j", log4JNamespace));
        var xml = XElement.Load(xmlReader);
        var messageXName = XName.Get("message", log4JNamespace);
        var propertiesXName = XName.Get("properties", log4JNamespace);
        var propertiesDataXName = XName.Get("data", log4JNamespace);

        var level = LogLevel.FromString(xml.Attribute("level")?.Value ?? throw new KeyNotFoundException("Attribute: level"));
        var logger = xml.Attribute("logger")?.Value ?? throw new KeyNotFoundException("Attribute: logger");
        var message = xml.Element(messageXName)?.Value ?? throw new KeyNotFoundException("Element: message");
        var properties = xml.Element(propertiesXName)?.Elements(propertiesDataXName)
                            .ToDictionary(
                                x => x.Attribute("name")?.Value ?? throw new KeyNotFoundException("Attribute: name"),
                                x => x.Attribute("value")?.Value)
                         ?? DictionaryHelper<string, string?>.Empty;

        DictionaryHelper<string, string?>.Rename(properties, "log4jmachinename", "machinename");
        DictionaryHelper<string, string?>.Rename(properties, "log4japp", "processname");

        var result = LogEventInfo.Create(level, logger, message);
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
}
