using System;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class FormatDiscriminatorAttribute : Attribute
{
    public string Discriminator { get; }
    public Type OptionsType { get; }

    public FormatDiscriminatorAttribute(string discriminator, Type optionsType)
    {
        this.Discriminator = discriminator;
        this.OptionsType = optionsType;
    }
}
