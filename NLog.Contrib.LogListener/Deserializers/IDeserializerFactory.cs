namespace NLog.Contrib.LogListener.Deserializers;

public interface IDeserializerFactory
{
    T Get<T>(DeserializerOptions options)
        where T : IDeserializer;
}
