using NLog.Contrib.LogListener.Tcp.Data;

namespace NLog.Contrib.LogListener.Tcp.Deserializers;

public interface INLogDeserializer
{
    LogEventInfo Convert(NLogMessage message);
    ExtractResult TryExtract(ExtractInput input);
}
