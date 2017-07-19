using System.Threading.Tasks;
using System.Web.Cors;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;

[assembly: OwinStartup(typeof(BlockExplorerAPI.Startup))]

namespace BlockExplorerAPI
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var corsOptions = CreateCorsOptions();
            app.UseCors(corsOptions);

            app.Map("/signalr", map =>
            {
                map.UseCors(corsOptions);
                var hubConfiguration = new HubConfiguration
                {
                    EnableDetailedErrors = true
                };
                map.RunSignalR(hubConfiguration);
            });
        }

        public CorsOptions CreateCorsOptions()
        {
            var corsPolicy = new CorsPolicy
            {
                AllowAnyMethod = true,
                AllowAnyHeader = true,
                SupportsCredentials = true,
                AllowAnyOrigin = true,
            };
            var corsOptions = new CorsOptions
            {
                PolicyProvider = new CorsPolicyProvider
                {
                    PolicyResolver = context => Task.FromResult(corsPolicy)
                }
            };
            return corsOptions;
        }
    }
}
