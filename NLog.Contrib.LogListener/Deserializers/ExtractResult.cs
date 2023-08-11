using System;
using System.Text;

namespace NLog.Contrib.LogListener.Deserializers;

public readonly record struct ExtractResult(bool Succeeded, LogEventInfo? Result, ReadOnlyMemory<byte> Leftover)
{
    public ExtractResult(bool Succeeded, LogEventInfo? Result, string Leftover)
        : this(Succeeded, Result, Encoding.UTF8.GetBytes(Leftover))
    {
    }

    public string LeftoverString => Encoding.UTF8.GetString(this.Leftover.Span);
}
