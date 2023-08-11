using System;
using NLog.Contrib.LogListener.Deserializers.Formats;

namespace NLog.Contrib.LogListener.Deserializers;

public class NLogDeserializer : INLogDeserializer
{
    private static readonly ILogger MyLogger = InternalLogger.Get<NLogDeserializer>();
    private readonly IFormat[] _formats;

    public NLogDeserializer(IFormat[] formats)
    {
        this._formats = formats;
    }

    public ExtractResult TryExtract(ExtractInput input)
        => this.TryExtract(input, true);

    public ExtractResult TryExtract(ExtractInput input, bool skipException)
    {
        foreach (var format in this._formats)
        {
            var chunk = format.GetChunk(input);
            if (chunk.Equals(default))
            {
                continue;
            }

            try
            {
                var result = format.Deserialize(input, chunk);
                var leftover = input.Data[chunk.End.Value..];
                return new ExtractResult(true, result, leftover);
            }
            catch (Exception e) when (skipException)
            {
                var leftover = input.Data[chunk.End.Value..];
                var value = input.DataString.Substring(chunk.Start.Value, chunk.End.Value);
                NLogDeserializer.MyLogger.Error(
                    "{Deserializer} failed with:\n{Exception}\n\nParsing:\n{Match}",
                    format.GetType().Name,
                    e.ToString(),
                    value);
                return new ExtractResult(false, null, leftover);
            }
        }

        return new ExtractResult(false, null, input.Data);
    }
}
