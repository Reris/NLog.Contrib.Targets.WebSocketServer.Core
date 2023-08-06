using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NLog.Contrib.LogListener.Tcp.Deserializers;

public class JsonFormatDeserializer : IFormatDeserializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Range GetChunk(ExtractInput input)
    {
        if (!JsonFormatDeserializer.StartsAsJson(input.Data.Span))
        {
            return default;
        }

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

    public LogEventInfo Deserialize(ExtractInput input, Range chunk)
    {
        var slice = input.Data.Span.Slice(chunk.Start.Value, chunk.End.Value);
        var data = JsonSerializer.Deserialize<JsonLogEventInfo>(slice, JsonFormatDeserializer.JsonOptions) ?? throw new JsonException("Empty value");
        data.Properties ??= new Dictionary<string, string?>();
        data.Properties.TryGetValue("app", out var app);
        app ??= "Unknown";
        data.Properties.TryGetValue("machinename", out var machinename);
        machinename ??= "Unknown";
        var transform = new
        {
            Level = LogLevel.FromString(data.Level ?? throw new KeyNotFoundException("level")),
            Logger = $"App={app.Replace('.', ':')}.Machine={machinename}.{data.Logger ?? throw new KeyNotFoundException("logger")}",
            Message = data.Message ?? throw new KeyNotFoundException("Element: message"),
            Properties = data.Properties ?? new Dictionary<string, string?>()
        };

        var result = LogEventInfo.Create(transform.Level, transform.Logger, transform.Message);
        foreach (var property in transform.Properties)
        {
            result.Properties[property.Key] = property.Value;
        }

        return result;
    }

    private static bool StartsAsJson(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
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

    private class JsonLogEventInfo
    {
        public string? Level { get; set; }
        public string? Logger { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string?>? Properties { get; set; }
    }
}
