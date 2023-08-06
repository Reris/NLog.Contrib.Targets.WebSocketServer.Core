using System;
using NLog.Contrib.LogListener.Tcp.Data;

namespace NLog.Contrib.LogListener.Tcp.Deserializers;

public class NLogDeserializer : INLogDeserializer
{
    private static readonly ILogger MyLogger = InternalLogger.Get<NLogDeserializer>();
    private readonly IFormatDeserializer[] _deserializers;

    public NLogDeserializer(IFormatDeserializer[] deserializers)
    {
        this._deserializers = deserializers;
    }

    public LogEventInfo Convert(NLogMessage message) => LogEventInfo.Create(LogLevel.FromOrdinal((int)message.Level), message.Logger, message.Message);

    public ExtractResult TryExtract(ExtractInput input)
        => this.TryExtract(input, true);

    public ExtractResult TryExtract(ExtractInput input, bool skipException)
    {
        foreach (var formatType in this._deserializers)
        {
            var chunk = formatType.GetChunk(input);
            if (chunk.Equals(default))
            {
                continue;
            }

            try
            {
                var result = formatType.Deserialize(input, chunk);
                var leftover = input.Data[chunk.End.Value..];
                return new ExtractResult(true, result, leftover);
            }
            catch (Exception e) when (skipException)
            {
                var leftover = input.Data[chunk.End.Value..];
                var value = input.DataString.Substring(chunk.Start.Value, chunk.End.Value);
                NLogDeserializer.MyLogger.Error(
                    "{Deserializer} failed with:\n{Exception}\n\nParsing:\n{Match}",
                    formatType.GetType().Name,
                    e.ToString(),
                    value);
                return new ExtractResult(false, null, leftover);
            }
        }

        return new ExtractResult(false, null, input.Data);
    }
}
