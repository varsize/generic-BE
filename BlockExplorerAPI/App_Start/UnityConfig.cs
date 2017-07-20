using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Practices.Unity;
using Newtonsoft.Json.Serialization;
using BlockExplorerAPI.DAL;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.Hubs;
using BlockExplorerAPI.Services;
using BlockExplorerAPI.Services.Abstract;

namespace BlockExplorerAPI
{
    public static class UnityConfig
    {
        public static IUnityContainer Configure()
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

            container.RegisterType<ITransactionRepository, TwoLevelTransactionRepository>(
                new InjectionFactory(c => new TwoLevelTransactionRepository(
                        c.Resolve<InMemoryTransactionRepository>("mempooltx"), c.Resolve<ISimpleTransactionRepository>()
                    )));

            container.RegisterType<TransactionService>(new InjectionFactory(c => 
                new TransactionService(
                    new TwoLevelTransactionRepository(c.Resolve<InMemoryTransactionRepository>("mempooltx"), c.Resolve<ISimpleTransactionRepository>()), 
                        c.Resolve<CryptoRpcClient>(), 
                        c.Resolve<AddressService>(),
                        c.Resolve<IMessageDeliveryService>()
                    )));

            container.RegisterType<TxMemPoolChecker>(new InjectionFactory(c => 
                new TxMemPoolChecker(
                        c.Resolve<CryptoRpcClient>(), 
                        c.Resolve<TransactionService>()
                    )));

            container.RegisterType<AddressService>();
            container.RegisterType<BlockService>(new ContainerControlledLifetimeManager(), 
                new InjectionFactory(c => 
                    new BlockService(
                            () => c.Resolve<CryptoRpcClient>(), 
                            c.Resolve<TransactionService>(),
                            c.Resolve<IMessageDeliveryService>()
                        )));
            
            return container;
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

        public class SignalRJsonContractResolver : IContractResolver
        {
            private readonly Assembly assembly;
            private readonly IContractResolver customContractResolver;
            private readonly IContractResolver defaultContractSerializer;

            public SignalRJsonContractResolver(IContractResolver customContractResolver)
            {
                defaultContractSerializer = new DefaultContractResolver();
                this.customContractResolver = customContractResolver;
                assembly = typeof(Connection).Assembly;
            }

            public JsonContract ResolveContract(Type type)
            {
                if (type.Assembly.Equals(assembly))
                    return defaultContractSerializer.ResolveContract(type);
                return customContractResolver.ResolveContract(type);
            }
        }
    }
}