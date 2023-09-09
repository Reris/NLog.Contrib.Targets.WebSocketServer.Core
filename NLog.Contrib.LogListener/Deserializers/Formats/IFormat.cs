using System;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public interface IFormat
{
    string GetDiscriminator();
    Range GetSlice(ExtractInput input);
    LogEventInfo Deserialize(ExtractInput input, Range slice);
}
