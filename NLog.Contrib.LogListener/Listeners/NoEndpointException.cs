using System;
using System.Runtime.Serialization;

namespace NLog.Contrib.LogListener.Listeners;

[Serializable]
public class NoEndpointException : InvalidOperationException
{
    public NoEndpointException()
        : this("No Endpoint found.")
    {
    }

    public NoEndpointException(string? message)
        : base(message)
    {
    }

    public NoEndpointException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected NoEndpointException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
