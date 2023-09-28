using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace Logair;

public static class WebHostBuilderExtensions
{
    public static IWebHostBuilder UseInternalLogger(this IWebHostBuilder builder, NLogAspNetCoreOptions? options = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        options ??= NLogAspNetCoreOptions.Default;
        builder.UseNLog(options);
        /////////////////////////////////// Expected behavior of AspNetExtensions.UseNLog //////////////////////////////////////
        ////options = options ?? NLogAspNetCoreOptions.Default;
        ////builder.ConfigureServices(
        ////    services =>
        ////    {
        ////        ConfigurationItemFactory.Default.RegisterItemsFromAssembly(typeof(AspNetExtensions).GetTypeInfo().Assembly);
        ////        LogManager.AddHiddenAssembly(typeof(AspNetExtensions).GetTypeInfo().Assembly);
        ////        services.AddSingleton(
        ////            serviceProvider =>
        ////            {
        ////                ServiceLocator.ServiceProvider = serviceProvider;
        ////                return (ILoggerProvider) new NLogLoggerProvider(options);
        ////            });
        ////        if (!options.RegisterHttpContextAccessor)
        ////        {
        ////            return;
        ////        }
        ////        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        ////    });
        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<ILoggerProvider>();
                services.AddSingleton<ILoggerProvider>(new InternalLogger.Provider(options));
            });
        return builder;
    }
}
