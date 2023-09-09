using System;
using System.Text;

namespace NLog.Contrib.LogListener.Deserializers;

public class ExtractInput
{
    private string? _dataString;

    public ExtractInput(string data, ListenerOptions options)
        : this(Encoding.UTF8.GetBytes(data), options)
    {
        this._dataString = data;
    }

    public ExtractInput(ReadOnlyMemory<byte> data, ListenerOptions options)
    {
        this.Data = data;
        this.Options = options;
    }

    public ReadOnlyMemory<byte> Data { get; }
    public ListenerOptions Options { get; }
    public string DataString => this._dataString ??= Encoding.UTF8.GetString(this.Data.Span);

    public override bool Equals(object? obj) => obj is ExtractInput other && this.DataString.Equals(other.DataString);
    public override int GetHashCode() => this.Data.GetHashCode();
}
