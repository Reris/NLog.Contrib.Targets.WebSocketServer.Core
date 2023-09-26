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
        var anyValidStart = false;
        foreach (var format in this._formats)
        {
            if (!format.HasValidStart(input))
            {
                continue;
            }

            anyValidStart = true;
            var slice = format.GetSlice(input);
            if (slice.Equals(default))
            {
                continue;
            }

            try
            {
                var result = format.Deserialize(input, slice);
                var leftover = input.Data[slice.End.Value..];
                return new ExtractResult(true, result, leftover);
            }
            catch (Exception e) when (skipException)
            {
                var leftover = input.Data[slice.End.Value..];
                var value = input.DataString.Substring(slice.Start.Value, slice.End.Value);
                NLogDeserializer.MyLogger.Error(
                    "{Deserializer} failed with:\n{Exception}\n\nParsing:\n{Match}",
                    format.GetType().Name,
                    e.ToString(),
                    value);
                return new ExtractResult(false, null, leftover);
            }
        }

        return new ExtractResult(false, null, anyValidStart ? input.Data : ReadOnlyMemory<byte>.Empty);
    }
}
