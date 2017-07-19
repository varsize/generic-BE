using System.Collections.Generic;
using System.Web.Http;
using BlockExplorerAPI.Models.Requests;
using BlockExplorerAPI.Services;
using BlockExplorerAPI.Services.Models;
using BlockExplorerAPI.Validation;
using System;
using BlockExplorerAPI.Services.Models.CreateTransaction;
using NLog;

namespace BlockExplorerAPI.Controllers
{
    [RoutePrefix("tx")]
    public class TransactionController : BaseApiController
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly CryptoRpcClient rpcClient;
        private readonly TransactionService transactionService;

        public TransactionController(CryptoRpcClient rpcClient, TransactionService transactionService)
        {
            this.rpcClient = rpcClient;
            this.transactionService = transactionService;
        }

        /// <summary>
        /// Get detailed info about transaction(s)
        /// </summary>
        /// <param name="search">One or several txids separated by comma</param>
        /// <returns>Detailed info about each transaction</returns>
        [HttpGet]
        [Route("info/{search}")]
        public IHttpActionResult Info(string search)
        {
            string[] searchParams = search.Trim().Split(',');

            var transactions = new List<TransactionModel>();
            foreach (var searchParam in searchParams)
            {
                if (HashValidator.Validate(searchParam))
                {
                    var transaction = transactionService.Find(searchParam);
                    if (transaction != null) transactions.Add(transaction);
                }
            }

            if (transactions.Count == 0)
                return NotFound();
            return Ok(transactions);
        }

        /// <summary>
        /// Get last n ("count") transactions
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Transactions list ordered by descending</returns>
        [HttpGet]
        [Route("last")]
        public IHttpActionResult Last([FromUri] LastTransactionsRequest request)
        {
            if (request == null) request = new LastTransactionsRequest();
            var transactions = transactionService.GetLast(request.Count);
            return Ok(transactions);
        }

        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create([FromBody] CreateTransactionRequest request)
        {
            if (request == null)
                return BadRequest(Infrastructure.ApiStatusCode.CreateTransactionNoData);
            try
            {
                var tx = transactionService.CreateTransaction(request);
                return Ok(tx);
            }
            catch (Exception ex) {
                Logger.Error(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("createunsigned")]
        public IHttpActionResult CreateUnsigned([FromBody] CreateTransactionRequest request)
        {
            if (request == null)
                return BadRequest(Infrastructure.ApiStatusCode.CreateTransactionNoData);
            try
            {
                var tx = transactionService.CreateUnsignedTransaction(request);
                return Ok(tx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("send")]
        public IHttpActionResult Send([FromBody] SignTransactionRpcResponse request)
        {
            if (request == null)
                return BadRequest(Infrastructure.ApiStatusCode.CreateTransactionNoData);
            try
            {
                var tx = transactionService.SendRawTransaction(request);
                return Ok(tx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("raw/{input}")]
        public IHttpActionResult Raw(string input)
        {
            string[] inputParams = input.Trim().Split(',');
            var transactions = rpcClient.GetRawTransactions(inputParams);
            if (transactions.Count == 0)
                return NotFound();
            return Ok(transactions);
        }
    }
}
