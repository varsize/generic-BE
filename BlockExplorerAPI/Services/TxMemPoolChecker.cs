using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.Services.Abstract;

namespace BlockExplorerAPI.Services
{
    public class TxMemPoolChecker
    {
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly CryptoRpcClient rpcClient;
        //private readonly IExtendedTransactionRepository memPoolTransactionRepository;
        private readonly TransactionService transactionService;
        //private readonly IMessageDeliveryService messageDeliveryService;

        public TxMemPoolChecker(CryptoRpcClient rpcClient, /*IExtendedTransactionRepository memPoolTransactionRepository, */TransactionService transactionService/*, IMessageDeliveryService messageDeliveryService*/)
        {
            if (rpcClient == null)
                throw new ArgumentNullException("rpcClient");
            //if (memPoolTransactionRepository == null)
            //    throw new ArgumentNullException("memPoolTransactionRepository");
            if (transactionService == null)
                throw new ArgumentNullException("transactionService");
            //if (messageDeliveryService == null)
            //    throw new ArgumentNullException("messageDeliveryService");

            this.rpcClient = rpcClient;
            //this.memPoolTransactionRepository = memPoolTransactionRepository;
            this.transactionService = transactionService;
            //this.messageDeliveryService = messageDeliveryService;
        }
        
        public void Check()
        {
            try
            {
                Logger.Debug("RawMemPool Check");
                
                var txpool = rpcClient.GetRawMemPool();
                if (txpool == null)
                {
                    Logger.Error("RawMemPool is null");
                    return;
                }

                var txids = txpool.Values<string>().ToList();
                if (txids.Count == 0)
                {
                    Logger.Debug("RawMemPool is empty");
                    return;
                }

                List<JToken> transactions = rpcClient.GetRawTransactions(txids);
                foreach (var transaction in transactions)
                {
                    transactionService.SaveMemPoolTx(transaction);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}