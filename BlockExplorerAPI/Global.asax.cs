using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;
using BlockExplorerAPI.Services;

namespace BlockExplorerAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            UnityConfig.RegisterComponents();

            RunBlockchainSync();
        }

        protected void Application_Stop()
        {
            StopTxMemPoolChecker();
        }


        private void RunBlockchainSync()
        {
            var blockService = ResolveService<BlockService>();
            Task.Run(() =>
            {
                blockService.ScanBlockchain();
                StartTxMemPoolChecker();
            });
        }

        #region TxMemPool

        private Timer txMemPoolUpdateTimer;

        private void StartTxMemPoolChecker()
        {
            txMemPoolUpdateTimer = new Timer(ApplicationSettings.TxMemPoolUpdateInterval * 1000);
            txMemPoolUpdateTimer.Elapsed += (o, a) =>
            {
                txMemPoolUpdateTimer.Stop();
                var txMemPoolChecker = ResolveService<TxMemPoolChecker>();
                if (txMemPoolChecker != null)
                {
                    txMemPoolChecker.Check();
                }
                txMemPoolUpdateTimer.Start();
            };
            txMemPoolUpdateTimer.Start();
        }

        private void StopTxMemPoolChecker()
        {
            txMemPoolUpdateTimer.Stop();
            txMemPoolUpdateTimer.Dispose();
        }

        #endregion
        
        private static T ResolveService<T>() where T : class
        {
            return GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof (T)) as T;
        }
    }
}
