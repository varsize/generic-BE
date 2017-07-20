using System.Threading.Tasks;
using System.Web.Cors;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using System.Web.Http;
using Unity.WebApi;
using Newtonsoft.Json;
using static BlockExplorerAPI.UnityConfig;
using BlockExplorerAPI.Services.Abstract;
using BlockExplorerAPI.Services;
using Microsoft.Practices.Unity;
using Microsoft.AspNet.SignalR.Infrastructure;
using BlockExplorerAPI.Hubs;

[assembly: OwinStartup(typeof(BlockExplorerAPI.Startup))]

namespace BlockExplorerAPI
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var corsOptions = CreateCorsOptions();
            app.UseCors(corsOptions);

            var dependencyContainer = UnityConfig.Configure();
            var httpConfig = CreateHttpConfiguration();
            var hubConfig = new HubConfiguration();
            
            dependencyContainer.RegisterType<IMessageDeliveryService, SignalRNotificationService>(
                new InjectionFactory(c => new SignalRNotificationService(hubConfig.Resolver.Resolve<IConnectionManager>().GetHubContext<NotificationHub>())));

            var signalrDependencyResolver = new SignalRUnityDependencyResolver(dependencyContainer);
            signalrDependencyResolver.Register(typeof(JsonSerializer), () =>
                    JsonSerializer.Create(new JsonSerializerSettings()
                    {
                        ContractResolver = new SignalRJsonContractResolver(httpConfig.Formatters.JsonFormatter.SerializerSettings.ContractResolver),
                        Formatting = httpConfig.Formatters.JsonFormatter.SerializerSettings.Formatting
                    }));
            hubConfig.Resolver = signalrDependencyResolver;

            app.Map("/signalr", map =>
            {
                map.UseCors(corsOptions);
                map.RunSignalR(hubConfig);
            });

            httpConfig.DependencyResolver = new UnityDependencyResolver(dependencyContainer);
            app.UseWebApi(httpConfig);
        }

        private static CorsOptions CreateCorsOptions()
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

        public static HttpConfiguration CreateHttpConfiguration()
        {
            var config = new HttpConfiguration();
            WebApiConfig.Configure(config);
            return config;
        }
    }
}
