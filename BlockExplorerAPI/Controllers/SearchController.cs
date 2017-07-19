using System.Web.Http;
using Newtonsoft.Json.Linq;
using BlockExplorerAPI.Models.Responses;
using BlockExplorerAPI.Services;
using BlockExplorerAPI.Validation;

namespace BlockExplorerAPI.Controllers
{
    [RoutePrefix("search")]
    public class SearchController : BaseApiController
    {
        private readonly CryptoRpcClient rpcClient;
        private readonly TransactionService transactionService;
        private readonly AddressService addressService;

        public SearchController(CryptoRpcClient rpcClient, TransactionService transactionService, AddressService addressService)
        {
            this.rpcClient = rpcClient;
            this.transactionService = transactionService;
            this.addressService = addressService;
        }

        /// <summary>
        /// Search an transaction, block or address
        /// </summary>
        /// <param name="input">txid, or blockhash, or blocknumber, or address</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{input}")]
        public IHttpActionResult Search(string input)
        {
            if (HashValidator.Validate(input))
            {
                var transaction = transactionService.Find(input);
                if (transaction != null)
                    return Ok(new {type = "tx", result = transaction});

                var block = GetBlockWithTransactions(input);
                if (block != null)
                    return Ok(new { type = "block", result = block });
            }
            else if (AddressValidator.Validate(input))
            {
                var addressInfo = addressService.Find(input);
                if (addressInfo != null)
                    return Ok(new {type = "address", result = addressInfo});
            }
            else
            {
                int blockNumber;
                if (int.TryParse(input, out blockNumber))
                {
                    string blockHash = rpcClient.GetBlockHash(blockNumber);
                    if (blockHash != null)
                    {
                        var block = GetBlockWithTransactions(blockHash);
                        if (block != null)
                            return Ok(new { type = "block", result = block });
                    }
                }
            }
            return NotFound();
        }

        private JToken GetBlockWithTransactions(string hash)
        {
            var block = rpcClient.GetBlock(hash);
            if (block != null)
            {
                var transactions = transactionService.GetBlockTransactions(block);
                block["tx"] = JToken.FromObject(transactions, GlobalConfiguration.Configuration.Formatters.JsonFormatter.CreateJsonSerializer());
                block["type"] = BlockItemModel.ParseBlockType(block);
            }
            return block;
        }
    }
}
