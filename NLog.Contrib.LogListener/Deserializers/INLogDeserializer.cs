namespace NLog.Contrib.LogListener.Deserializers;

public interface INLogDeserializer : IDeserializer
{
    ExtractResult TryExtract(ExtractInput input);
}
