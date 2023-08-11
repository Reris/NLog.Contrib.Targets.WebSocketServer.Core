using System;
using JetBrains.Annotations;

namespace NLog.Contrib.LogListener;

[PublicAPI]
public record DeserializerOptions
{
    public string[] Formats { get; set; } = Array.Empty<string>();
}
