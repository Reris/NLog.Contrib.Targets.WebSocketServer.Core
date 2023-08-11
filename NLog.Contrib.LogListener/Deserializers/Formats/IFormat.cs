using System;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public interface IFormat
{
    string Name { get; }
    Range GetChunk(ExtractInput input);
    LogEventInfo Deserialize(ExtractInput input, Range chunk);
}
