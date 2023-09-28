using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Contrib.LogListener;
using NLog.Contrib.LogListener.Deserializers.Formats;
using NLog.Contrib.Targets.WebSocketServer.Core;

namespace Logair;

[ExcludeFromCodeCoverage]
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        this.Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILogger>(LogManager.GetLogger("redistribute"));
        services.AddLogListener(this.Configuration.GetSection("LogListener"));
        services.AddNLogTargetWebSocket(o => o.RedirectEmptyToViewer = true);
        services.AddControllers()
                .AddControllersAsServices();
        
        services.Configure<HttpListenerOptions>(BindHttpListenerOptions);

        void BindHttpListenerOptions(HttpListenerOptions options)
        {
            var optionsSection = this.Configuration.GetSection("HttpListener");
            optionsSection.Bind(options);
            var registeredFormats = RegisteredFormats.GetRegisteredFormats(services);
            var formatsSection = optionsSection.GetSection($"Formats");
            LogListeners.BindFormatOptions(formatsSection, registeredFormats, options.Formats);
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        app.UseNLogWebSockets();
        app.ApplicationServices.StartLogListeners();
    }
}
