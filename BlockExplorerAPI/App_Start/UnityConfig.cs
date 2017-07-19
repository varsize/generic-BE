using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Practices.Unity;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using BlockExplorerAPI.DAL;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.Hubs;
using BlockExplorerAPI.Services;
using BlockExplorerAPI.Services.Abstract;
using Unity.WebApi;

namespace BlockExplorerAPI
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            container.RegisterType<CryptoRpcClient>(new InjectionFactory(c => 
                new CryptoRpcClient(ApplicationSettings.RpcUser, 
                                        ApplicationSettings.RpcPassword, 
                                        ApplicationSettings.RpcHost, 
                                        ApplicationSettings.RpcPort
                                        )));
            
            container.RegisterType<InMemoryTransactionRepository>("mempooltx", new ContainerControlledLifetimeManager());
            container.RegisterType<ISimpleTransactionRepository, DatabaseTransactionRepository>(new HierarchicalLifetimeManager(),
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["default"].ToString()));
            /*container.RegisterType<InMemoryTransactionRepository>("blocktx", new ContainerControlledLifetimeManager(), new InjectionFactory(
                c =>
                {
                    if (File.Exists(ApplicationSettings.TxIndexFilePath))
                    {
                        var formatter = new BinaryFormatter();
                        var stream = new FileStream(ApplicationSettings.TxIndexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        try
                        {
                            var obj = formatter.Deserialize(stream);
                            return obj as InMemoryTransactionRepository;
                        }
                        catch (Exception)
                        {
                            return new InMemoryTransactionRepository();
                        }
                        finally
                        {
                            stream.Close();
                        }
                    }
                    return new InMemoryTransactionRepository();
                }));
            container.RegisterType<ISimpleTransactionRepository, InMemoryTransactionRepository>(
                new InjectionFactory(c => c.Resolve<InMemoryTransactionRepository>("blocktx")));*/
            container.RegisterType<ITransactionRepository, TwoLevelTransactionRepository>(
                new InjectionFactory(c => new TwoLevelTransactionRepository(
                        c.Resolve<InMemoryTransactionRepository>("mempooltx"), c.Resolve<ISimpleTransactionRepository>()
                        //c.Resolve<InMemoryTransactionRepository>("blocktx")
                    )));

            container.RegisterType<TransactionService>(new InjectionFactory(c => 
                new TransactionService(
                    new TwoLevelTransactionRepository(c.Resolve<InMemoryTransactionRepository>("mempooltx"), c.Resolve<ISimpleTransactionRepository>()), 
                    c.Resolve<CryptoRpcClient>(), 
                    c.Resolve<AddressService>(),
                    c.Resolve<IMessageDeliveryService>())));

            container.RegisterType<TxMemPoolChecker>(new InjectionFactory(c => 
                new TxMemPoolChecker(
                    c.Resolve<CryptoRpcClient>(), 
                    c.Resolve<TransactionService>())));
            container.RegisterType<AddressService>();
            container.RegisterType<BlockService>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => 
                new BlockService(
                    () => c.Resolve<CryptoRpcClient>(), 
                    c.Resolve<TransactionService>(),
                    c.Resolve<IMessageDeliveryService>())));

            container.RegisterType<IMessageDeliveryService, SignalRNotificationService>(
                new InjectionFactory(c => new SignalRNotificationService(GlobalHost.ConnectionManager.GetHubContext<NotificationHub>())));
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);

            GlobalHost.DependencyResolver = new SignalRUnityDependencyResolver(container);
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () =>
                JsonSerializer.Create(new JsonSerializerSettings()
                {
                    ContractResolver = new SignalRContractResolver(),
                    Formatting = Formatting.Indented, 
                }));
        }

        public class SignalRUnityDependencyResolver : DefaultDependencyResolver
        {
            private IUnityContainer container;
            public SignalRUnityDependencyResolver(IUnityContainer container)
            {
                this.container = container;
            }

            public override object GetService(Type serviceType)
            {
                if (container.IsRegistered(serviceType))
                    return container.Resolve(serviceType);
                else
                    return base.GetService(serviceType);
            }

            public override IEnumerable<object> GetServices(Type serviceType)
            {
                if (container.IsRegistered(serviceType))
                    return container.ResolveAll(serviceType);
                else
                    return base.GetServices(serviceType);
            }
        }

        public class SignalRContractResolver : IContractResolver
        {
            private readonly Assembly assembly;
            private readonly IContractResolver camelCaseContractResolver;
            private readonly IContractResolver defaultContractSerializer;

            public SignalRContractResolver()
            {
                defaultContractSerializer = new DefaultContractResolver();
                camelCaseContractResolver = new CamelCasePropertyNamesContractResolver();
                assembly = typeof(Connection).Assembly;
            }

            public JsonContract ResolveContract(Type type)
            {
                if (type.Assembly.Equals(assembly))
                    return defaultContractSerializer.ResolveContract(type);
                return camelCaseContractResolver.ResolveContract(type);
            }
        }
    }
}