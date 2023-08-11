using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public class JsonFormat : IFormat
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string Name => "json";

    public Range GetChunk(ExtractInput input)
    {
        if (!JsonFormat.StartsAsJson(input.Data.Span))
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

        var dataObjects = JsonSerializer.Deserialize<Dictionary<string, object?>>(slice, JsonFormat.JsonOptions) ?? throw new JsonException("Empty value");
        var data = dataObjects.ToDictionary(a => a.Key, a => a.Value?.ToString());

        var level = LogLevel.FromString(data["level"]);
        var message = data["message"];
        var logger = data.GetValueOrDefault("logger", "Unknown");

        var result = LogEventInfo.Create(level, logger, message);
        foreach (var property in data.Where(JsonFormat.TakeAsProperty))
        {
            result.Properties[property.Key] = property.Value;
        }

        return result;
    }

    private static bool TakeAsProperty(KeyValuePair<string, string?> entry)
        => entry.Key switch
        {
            "level" => false,
            "message" => false,
            "logger" => false,
            _ => true
        };

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
}
