using System.Configuration;
using System.Globalization;

namespace BlockExplorerAPI
{
    public static class ApplicationSettings
    {
        public static string RpcHost
        {
            get { return ConfigurationManager.AppSettings["RpcHost"]; }
        }
        public static int RpcPort
        {
            get { return int.Parse(ConfigurationManager.AppSettings["RpcPort"]); }
        }
        public static string RpcUser
        {
            get { return ConfigurationManager.AppSettings["RpcUser"]; }
        }
        public static string RpcPassword
        {
            get { return ConfigurationManager.AppSettings["RpcPassword"]; }
        }

        public static double TxMemPoolUpdateInterval 
        { 
            get { return double.Parse(ConfigurationManager.AppSettings["TxMemPoolUpdateInterval"], CultureInfo.InvariantCulture); } 
        }

        public static int DefaultLastTransactionsCount
        {
            get { return int.Parse(ConfigurationManager.AppSettings["DefaultLastTransactionsCount"]); }
        }
        public static int MaxLastTransactionsCount
        {
            get { return int.Parse(ConfigurationManager.AppSettings["MaxLastTransactionsCount"]); }
        }
    }
}