namespace NLog.Contrib.LogListener.Deserializers;

public interface IDeserializerFactory
{
    T Get<T>(ListenerOptions options)
        where T : IDeserializer;

    void Configure(ListenerOptions options);
}
