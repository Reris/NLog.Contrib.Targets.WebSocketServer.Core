using System;

namespace NLog.Contrib.LogListener.Listeners;

public class ReceivedEventArgs : EventArgs
{
    public ReceivedEventArgs(ReadOnlyMemory<byte> data)
    {
        this.Data = data;
    }

    public ReadOnlyMemory<byte> Data { get; }
}
