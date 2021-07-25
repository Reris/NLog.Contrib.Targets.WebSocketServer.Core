using System;

namespace NLog.Contrib.Targets.WebSocket.Web.AspNetCore
{
    internal static class ServiceProviderExtension
    {
        internal static T GetService<T>(this IServiceProvider serviceProvider)
            where T : class
        {
            return serviceProvider.GetService(typeof(T)) as T;
        }
    }
}
