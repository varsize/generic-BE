using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using BlockExplorerAPI.Models.Responses;
using BlockExplorerAPI.Services.Abstract;
using BlockExplorerAPI.Validation;
using System.Threading;

namespace BlockExplorerAPI.Services
{
    public class BlockService
    {
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly int RpcMethodsLimit = 100;

        private readonly Func<CryptoRpcClient> GetRpcClient;
        private readonly TransactionService transactionService;
        private readonly IMessageDeliveryService messageDeliveryService;

        public int LastScannedBlockNumber { get; private set; }
        private object lockObj = new object();
        
        public BlockService(Func<CryptoRpcClient> rpcClientFactory, TransactionService transactionService, IMessageDeliveryService messageDeliveryService)
        {
            if (rpcClientFactory == null)
                throw new ArgumentNullException("rpcClientFactory");
            if (transactionService == null)
                throw new ArgumentNullException("transactionService");
            if (messageDeliveryService == null)
                throw new ArgumentNullException("messageDeliveryService");

            this.GetRpcClient = rpcClientFactory;
            this.transactionService = transactionService;
            this.messageDeliveryService = messageDeliveryService;
            LastScannedBlockNumber = GetLastScannedBlockNumber();
        }

        public void NewBlock(string hash)
        {
            Logger.Info("NewBlock: {0}", hash);
            if (!HashValidator.Validate(hash))
            {
                Logger.Warn("Invalid blockhash provided: {0}", hash);
                return;
            }

            var block = GetRpcClient().GetBlock(hash);
            if (block == null)
            {
                Logger.Warn("Block was not found: {0}", hash);
                return;
            }
            
            int blockHeight = block["height"].Value<int>();

            if (Monitor.TryEnter(lockObj, TimeSpan.FromSeconds(3)))
            {
                try
                {
                    if (LastScannedBlockNumber + 1 > blockHeight)
                    {
                        return;
                    }

                    Logger.Debug("LastScannedBlockNumber: {0}", LastScannedBlockNumber);
                    ScanBlockchainNoLock(toBlockNumber: blockHeight - 1);
                    SaveBlockTransactions(block, true);
                    transactionService.MemPool.Clear();
                }
                catch(Exception e)
                {
                    Logger.Error(e, "NewBlock");
                }
                finally
                {
                    Monitor.Exit(lockObj);
                }
            }
        }

        private void SaveBlockTransactions(JToken block, bool checkMemPool)
        {
            int height = block["height"].Value<int>();
            Logger.Debug("Saving block: {0}. MemPool check: {1}", height, checkMemPool);

            if (transactionService.BlockExists(block["hash"].Value<string>()))
                return;

            var transactions = transactionService.SaveAllBlockTransactions(block);
            LastScannedBlockNumber = height;

            var blockModel = new BlockItemModel(block, transactions);
            messageDeliveryService.NewBlock(blockModel);
            foreach (var transaction in transactions)
            {
                if (!checkMemPool || !transactionService.MemPool.Contains(transaction.Txid))
                {
                    if (checkMemPool)
                        Logger.Debug("txid was not found in the mempool: {0}", transaction.Txid);
                    messageDeliveryService.NewTransaction(transaction);
                }
            }
        }

        public void ScanBlockchain(int? fromBlockNumber = null, int? toBlockNumber = null)
        {
            lock (lockObj)
            {
                ScanBlockchainNoLock(fromBlockNumber, toBlockNumber);
            }
        }

        private void ScanBlockchainNoLock(int? fromBlockNumber = null, int? toBlockNumber = null)
        {
            if (!fromBlockNumber.HasValue || LastScannedBlockNumber <= fromBlockNumber.Value)
                fromBlockNumber = LastScannedBlockNumber + 1;
            if (!toBlockNumber.HasValue)
                toBlockNumber = GetRpcClient().GetBlockCount();

            if (fromBlockNumber >= toBlockNumber/* || LastScannedBlockNumber == toBlockNumber*/)
            {
                return;
            }

            try
            {
                int from = fromBlockNumber.Value;
                int to = from + RpcMethodsLimit - 1;
                if (to > toBlockNumber.Value)
                    to = toBlockNumber.Value;
                List<JToken> blocks = GetBlocks(from, to);
                while (to < toBlockNumber.Value)
                {
                    from = to + 1;
                    to += RpcMethodsLimit;
                    if (to > toBlockNumber.Value)
                        to = toBlockNumber.Value;

                    Task<List<JToken>> rpcTask = Task.Run(() => GetBlocks(from, to));
                    SaveBlocks(blocks);
                    Task.WaitAll(rpcTask);
                    blocks = rpcTask.Result;
                }
                SaveBlocks(blocks);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private int GetLastScannedBlockNumber()
        {
            return transactionService.GetLastBlockNumber();
        }

        private List<JToken> GetBlocks(int from, int to)
        {
            var rpcClient = GetRpcClient();
            var blockHashes = rpcClient.GetBlockHashes(to, from);
            blockHashes.Reverse();
            var blocks = rpcClient.GetBlocks(blockHashes);
            return blocks;
        }

        private void SaveBlocks(List<JToken> blocks)
        {
            foreach (var block in blocks)
            {
                SaveBlockTransactions(block, false);
            }
        }
    }
}