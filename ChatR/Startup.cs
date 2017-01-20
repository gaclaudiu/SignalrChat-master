using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using System;

[assembly: OwinStartup(typeof(ChatR.Startup))]

namespace ChatR
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HubConfiguration();
            config.EnableDetailedErrors = true;

            GlobalHost.Configuration.TransportConnectTimeout = TimeSpan.FromSeconds(10);
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(10);
            GlobalHost.DependencyResolver.UseRedis("[azure redis cache]", 6379, "[password]", "[app name]");
            //GlobalHost.DependencyResolver.UseSqlServer("your connection string");

            app.MapSignalR(config);
        }
    }
}