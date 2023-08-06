using System;

namespace NLog.Contrib.LogListener.Tcp.Deserializers;

public interface IFormatDeserializer
{
    Range GetChunk(ExtractInput input);
    LogEventInfo Deserialize(ExtractInput input, Range chunk);
}
