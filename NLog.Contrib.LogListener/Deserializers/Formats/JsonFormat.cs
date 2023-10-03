using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JetBrains.Annotations;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

[FormatDiscriminator(JsonFormat.Discriminator, typeof(Options))]
public class JsonFormat : IFormat, IConfigurable
{
    public const string Discriminator = "json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private Options _currentOptions;

    public JsonFormat()
    {
        this._currentOptions = this.MapOptions(new Options());
    }

    public void Configure(FormatOptions options) => this._currentOptions = this.MapOptions((Options)options);

    public string GetDiscriminator() => JsonFormat.Discriminator;

    public bool HasValidStart(ExtractInput input)
    {
        foreach (var b in input.Data.Span)
        {
            switch ((char)b)
            {
                case '{':
                    return true;
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
        try
        {
            var reader = new Utf8JsonReader(input.Data.Span);
            if (reader.Read() && reader.TrySkip())
            {
                return new Range(0, (int)reader.BytesConsumed);
            }
        }
        catch
        {
            // munch
        }

        return default;
    }

    public LogEventInfo Deserialize(ExtractInput input, Range slice)
    {
        var span = input.Data.Span.Slice(slice.Start.Value, slice.End.Value);

        var dataObjects = JsonSerializer.Deserialize<Dictionary<string, object?>>(span, JsonFormat.JsonOptions) ?? throw new JsonException("Empty value");
        var data = dataObjects.ToDictionary(a => a.Key, a => a.Value?.ToString());

        var level = FormatHelper.ParseLogLevel(data.GetValueOrDefault(this._currentOptions.LevelNames, "info" /* compact doesnt send @l on info */));
        var logger = data.GetValueOrDefault(this._currentOptions.SourceContextNames, "Unknown");
        var message = data.GetValueOrDefault(this._currentOptions.MessageNames);
        this._currentOptions.ProcessNames.ForEach(a => data.RenameKey(a, "@pn"));
        this._currentOptions.MachineNames.ForEach(a => data.RenameKey(a, "@mn"));

        var result = LogEventInfo.Create(level, logger, message);
        if (DateTime.TryParse(data.GetValueOrDefault(this._currentOptions.TimestampNames), out var timestamp))
        {
            result.TimeStamp = timestamp;
        }

        foreach (var property in data.Where(JsonFormat.TakeAsProperty))
        {
            result.Properties[property.Key] = property.Value;
        }

        return result;
    }

    private Options MapOptions(Options options)
    {
        var o = new Options
        {
            Schemes = options.Schemes.Any() ? options.Schemes.ToArray() : new[] { "logstash" },
            TimestampNames = new List<string>(options.TimestampNames),
            LevelNames = new List<string>(options.LevelNames),
            SourceContextNames = new List<string>(options.SourceContextNames),
            MessageNames = new List<string>(options.MessageNames),
            ProcessNames = new List<string>(options.ProcessNames),
            MachineNames = new List<string>(options.MachineNames)
        };

        foreach (var schema in o.Schemes)
        {
            if (schema == "logstash")
            {
                o.TimestampNames.Add("timestamp");
                o.LevelNames.Add("level");
                o.SourceContextNames.Add("sourceContext");
                o.MessageNames.Add("message");
                o.ProcessNames.Add("processName");
                o.MachineNames.Add("machineName");
            }
            else if (schema == "compact")
            {
                o.TimestampNames.Add("@t");
                o.LevelNames.Add("@l");
                o.SourceContextNames.Add("@s");
                o.SourceContextNames.Add("SourceContext");
                o.MessageNames.Add("@mt");
                o.MessageNames.Add("@m");
                o.ProcessNames.Add("@pn");
                o.ProcessNames.Add("ProcessName");
                o.MachineNames.Add("@mn");
                o.MachineNames.Add("MachineName");
            }
        }

        o.TimestampNames = o.TimestampNames.Distinct().ToList();
        o.LevelNames = o.LevelNames.Distinct().ToList();
        o.SourceContextNames = o.SourceContextNames.Distinct().ToList();
        o.MessageNames = o.MessageNames.Distinct().ToList();
        o.ProcessNames = o.ProcessNames.Distinct().ToList();
        o.MachineNames = o.MachineNames.Distinct().ToList();

        return o;
    }

    private static bool TakeAsProperty(KeyValuePair<string, string?> entry)
        => entry.Key switch
        {
            "level" => false,
            "message" => false,
            "logger" => false,
            _ => true
        };

    [PublicAPI]
    public record Options : FormatOptions
    {
        public string[] Schemes { get; set; } = Array.Empty<string>();
        public List<string> TimestampNames { get; set; } = new();
        public List<string> LevelNames { get; set; } = new();
        public List<string> SourceContextNames { get; set; } = new();
        public List<string> MessageNames { get; set; } = new();
        public List<string> ProcessNames { get; set; } = new();
        public List<string> MachineNames { get; set; } = new();
        public override string GetDiscriminator() => JsonFormat.Discriminator;
    }
}
