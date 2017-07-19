using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using BlockExplorerAPI.DAL;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.Models.Requests;
using BlockExplorerAPI.Models.Responses;
using BlockExplorerAPI.Services;
using BlockExplorerAPI.Services.Abstract;
using BlockExplorerAPI.Validation;

namespace BlockExplorerAPI.Controllers
{
    [RoutePrefix("block")]
    public class BlockController : BaseApiController
    {
        private readonly CryptoRpcClient rpcClient;
        private readonly BlockService blockService;
        private readonly ISimpleTransactionRepository transactionRepository;
        private readonly TransactionService transactionService;
        private readonly IMessageDeliveryService messageDeliveryService;

        public BlockController(CryptoRpcClient rpcClient, BlockService blockService, ISimpleTransactionRepository transactionRepository, TransactionService transactionService, IMessageDeliveryService messageDeliveryService)
        {
            this.rpcClient = rpcClient;
            this.blockService = blockService;
            this.transactionRepository = transactionRepository;
            this.transactionService = transactionService;
            this.messageDeliveryService = messageDeliveryService;
        }


        [HttpGet]
        public IHttpActionResult Info()
        {
            var info = rpcClient.GetInfo();
            long networkhashps = rpcClient.GetNetworkHashps();
            return Ok(new { blocks = info["blocks"], difficulty = info["difficulty"], networkhashps});
        }

        /// <summary>
        /// Get detailed info about a block/blocks
        /// </summary>
        /// <param name="search">String that can contain blocknumber, blockhash or keywords "first" and "last" separated by comma</param>
        /// <returns>Detailed info about blocks that matching search conditions</returns>
        [HttpGet]
        [Route("info/{search}")]
        public IHttpActionResult Info(string search)
        {
            string[] searchParams = search.Trim().Split(',');
            
            //prepare batch requests
            var blockNumbers = new HashSet<int>();
            var blockHashes = new HashSet<string>();
            foreach (var searchParam in searchParams)
            {
                int blockNumber;
                if (int.TryParse(searchParam, out blockNumber))
                    blockNumbers.Add(blockNumber);
                else if (searchParam == "first")
                    blockNumbers.Add(0);
                else if (searchParam == "last")
                {
                    string blockHash = rpcClient.GetBestBlockHash();
                    if (blockHash != null) blockHashes.Add(blockHash);
                }
                else if (HashValidator.Validate(searchParam))
                    blockHashes.Add(searchParam);
            }

            var blockHashesByNumbers = rpcClient.GetBlockHashes(blockNumbers);
            foreach (var blockHash in blockHashesByNumbers)
                blockHashes.Add(blockHash);

            var blocks = rpcClient.GetBlocks(blockHashes);
            if (blocks.Count == 0)
                return NotFound();

            foreach (var block in blocks)
            {
                var transactions = transactionService.GetBlockTransactions(block);
                block["tx"] = JToken.FromObject(transactions, GlobalConfiguration.Configuration.Formatters.JsonFormatter.CreateJsonSerializer());
                block["type"] = BlockItemModel.ParseBlockType(block);
            }
            return Ok(blocks);
        }
        
        /// <summary>
        /// Get blocks within given offset and limit from the last block
        /// </summary>
        /// <param name="request"></param>
        /// <returns>General info about each block</returns>
        [HttpGet]
        [Route("list")]
        public IHttpActionResult List([FromUri]PaginationRequest request)
        {
            if (request == null) request = new PaginationRequest();
            int blockCount = rpcClient.GetBlockCount();

            int startBlockNumber = blockCount - request.Offset;
            if (startBlockNumber <= 0)
                return NotFound();
            int endBlockNumber = startBlockNumber - request.Limit + 1;

            var blockHashes = rpcClient.GetBlockHashes(startBlockNumber, endBlockNumber);
            var blocks = rpcClient.GetBlocks(blockHashes);

            var result = new List<BlockItemModel>();
            foreach (var block in blocks)
            {
                var transactions = transactionService.GetBlockTransactions(block);
                var blockModel = new BlockItemModel(block, transactions);
                result.Add(blockModel);
            }

            if (result.Count == 0)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Accepts a standard blocknotify message from wallet
        /// </summary>
        /// <param name="blockhash"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("notify/{blockhash}")]
        public IHttpActionResult BlockNotify(string blockhash)
        {
            blockService.NewBlock(blockhash);
            return Ok();
        }

        #region Temporary Methods

        [HttpGet]
        public IHttpActionResult DebugInfo()
        {
            return Ok(new { blockService.LastScannedBlockNumber });
        }
        /*
        [HttpPost]
        [Route("backup")]
        public IHttpActionResult BackupTransactionRepository()
        {
            var repo = transactionRepository as InMemoryTransactionRepository;
            if (repo == null)
                return BadRequest();

            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(ApplicationSettings.TxIndexFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, repo);
            }
            return Ok();
        }

        [HttpPost]
        [Route("message/block")]
        public IHttpActionResult ImitateNewBlockMessage()
        {
            string blockhash = rpcClient.GetBlockHash(rpcClient.GetBlockCount() - 1000);
            var block = rpcClient.GetBlock(blockhash);

            var blockModel = new BlockItemModel()
            {
                Hash = block["hash"].Value<string>(),
                Size = block["size"].Value<int>(),
                Height = block["height"].Value<int>(),
                Time = block["time"].Value<long>(),
            };
            var txids = block["tx"].Values<string>();
            foreach (var txid in txids)
            {
                var transaction = transactionService.Find(txid);
                if (transaction != null) blockModel.CoinsSent += transaction.OutputsValue;
                blockModel.TxCount++;
            }

            messageDeliveryService.NewBlock(blockModel);
            return Ok();
        }

        [HttpPost]
        [Route("message/tx")]
        public IHttpActionResult ImitateNewTxMessage()
        {
            var tx = transactionService.GetLast(1).First();
            messageDeliveryService.NewTransaction(transactionService.Find(tx.Txid));
            return Ok();
        }
        */
        #endregion
    }
}
