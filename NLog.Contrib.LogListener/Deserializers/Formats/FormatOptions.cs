namespace NLog.Contrib.LogListener.Deserializers.Formats;

public abstract record FormatOptions
{
    public abstract string GetDiscriminator();
}
