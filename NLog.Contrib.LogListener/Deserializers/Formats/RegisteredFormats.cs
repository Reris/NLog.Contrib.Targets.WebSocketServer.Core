using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public class RegisteredFormats
{
    public RegisteredFormats(IReadOnlyList<RegisteredFormat> formats)
    {
        this.Formats = formats;
    }

    public IReadOnlyList<RegisteredFormat> Formats { get; }

    public static RegisteredFormats GetRegisteredFormats(IServiceCollection serviceCollection)
    {
        var formats = serviceCollection.Where(a => typeof(IFormat).IsAssignableFrom(a.ServiceType))
                                       .Select(a => (a.ServiceType.GetCustomAttribute<FormatDiscriminatorAttribute>() ?? null, a.ServiceType))
                                       .Where(a => a.Item1 is not null)
                                       .Select(a => new RegisteredFormat(a.Item1!.Discriminator, a.Item1.OptionsType, a.ServiceType))
                                       .ToArray();
        return new RegisteredFormats(formats);
    }

    public record struct RegisteredFormat(string Discriminator, Type OptionsType, Type FormatType);
}
