using System;

namespace WebApplicationCore.Extensions
{
    public static class ServiceProviderExtension
    {
        internal static T GetService<T>(this IServiceProvider serviceProvider)
            where T : class
        {
            return serviceProvider.GetService(typeof(T)) as T;
        }
    }
}
