using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using Microsoft.Owin;
using System;
//using CommonServiceLocator;
using WebApplicationCore.Extensions;
using System.Linq;

namespace Owin.WebSocket
{
    public class WebSocketConnectionMiddleware<T>
        //: OwinMiddleware 
        where T : WebSocketConnection
    {
        //private readonly Regex mMatchPattern;
        //private readonly IServiceLocator mServiceLocator;
        private static readonly Task completedTask = Task.FromResult(false);

        //public WebSocketConnectionMiddleware(OwinMiddleware next, IServiceLocator locator)
        //    : base(next)
        //{
        //    mServiceLocator = locator;
        //}

        private readonly IServiceProvider _serviceProvider;
        public WebSocketConnectionMiddleware(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WebSocketConnectionMiddleware(IServiceProvider serviceProvider,
            // OwinMiddleware next, IServiceLocator locator, 
            Regex matchPattern)
            : this(serviceProvider)
        //: this(next, locator)
        {
            //mMatchPattern = matchPattern;
        }

        public async Task InvokeAsync(IDictionary<string, object> context)
        {
            var matches = new Dictionary<string, string>();

            //if (mMatchPattern != null)
            //{
            var routeAttributes = typeof(T).GetCustomAttributes(typeof(WebSocketRouteAttribute), true);

            if (routeAttributes.Length == 0)
                return;
            //throw new InvalidOperationException(typeof(T).Name + " type must have attribute of WebSocketRouteAttribute for mapping");

            var requestPath = context["owin.RequestPath"].ToString();

            foreach (var routeAttribute in routeAttributes.Cast<WebSocketRouteAttribute>())
            {
                var matchPattern = new Regex(routeAttribute.Route);

                var match = matchPattern.Match(requestPath);
                if (!match.Success)
                    return;// Next?.Invoke(context) ?? completedTask;

                for (var i = 1; i <= match.Groups.Count; i++)
                {
                    var name = matchPattern.GroupNameFromNumber(i);
                    var value = match.Groups[i];
                    matches.Add(name, value.Value);
                }
            }
            //}

            T socketConnection;
            if (_serviceProvider == null)
                socketConnection = Activator.CreateInstance<T>();
            else
                socketConnection = _serviceProvider.GetService<T>();

            await socketConnection.AcceptSocketAsync(context, matches);
        }
    }
}