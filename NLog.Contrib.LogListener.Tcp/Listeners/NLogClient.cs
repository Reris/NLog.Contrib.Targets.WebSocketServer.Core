using System;
using System.Text;
using NLog.Contrib.LogListener.Tcp.Deserializers;

namespace NLog.Contrib.LogListener.Tcp.Listeners;

public class NLogClient : ILogClient
{
    private static readonly ILogger Logger = InternalLogger.Get<NLogClient>();

    public NLogClient(INetworkChannel channel, ILogger clientLogger, INLogDeserializer deserializer)
    {
        this.Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        this.ClientLogger = clientLogger ?? throw new ArgumentNullException(nameof(clientLogger));
        this.Deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));

        NLogClient.Logger.Info("Client connection to '{RemoteEndPoint}' has been started", this.Channel.RemoteEndPoint.ToString());
        this.Channel.DataReceived += this.ChannelDataReceived;
        this.Channel.Disconnected += this.ChannelDisconnected;
    }

    public Memory<byte> CurrentData { get; private set; } = Array.Empty<byte>();
    public string CurrentDataString => Encoding.UTF8.GetString(this.CurrentData.Span);
    public INetworkChannel Channel { get; }
    public ILogger ClientLogger { get; }
    public INLogDeserializer Deserializer { get; }

    public void ChannelDisconnected(object? sender, EventArgs e)
        => NLogClient.Logger.Info("Client connection to '{RemoteEndPoint}' has been closed", this.Channel.RemoteEndPoint.ToString());

    public void ChannelDataReceived(object? _, ReceivedEventArgs e)
    {
        this.AppendCurrentData(e.Data);
        ExtractResult extracted;
        while ((extracted = this.Deserializer.TryExtract(new ExtractInput(this.CurrentData))).Succeeded)
        {
            this.CurrentData = extracted.Leftover.ToArray();
            this.ClientLogger.Log(extracted.Result);
        }
    }

    private void AppendCurrentData(ReadOnlyMemory<byte> data)
    {
        if (this.CurrentData.Length > 0)
        {
            var combine = new byte[this.CurrentData.Length + data.Length];
            this.CurrentData.CopyTo(combine.AsMemory());
            data.CopyTo(combine.AsMemory(this.CurrentData.Length));
            this.CurrentData = combine;
        }
        else
        {
            this.CurrentData = data.ToArray();
        }
    }
}
