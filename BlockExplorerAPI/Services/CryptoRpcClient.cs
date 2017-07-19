using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using BlockExplorerAPI.Utils;
using BlockExplorerAPI.Services.Models;

namespace BlockExplorerAPI.Services
{
    public class CryptoRpcClient : JsonRpcClient
    {
        public CryptoRpcClient(string user, string password, string host, int port) : base(user, password, host, port, "2.0")
        {
        }

        protected T InvokeMethod<T>(string method, params object[] methodParams)
        {
            var response = base.InvokeMethod(method, methodParams);
            var error = response["error"];
            if (!error.IsEmpty())
            {
                //TODO: write a log
                return default(T);
            }
            return response.Value<T>("result");
        }

        protected List<T> InvokeMethods<T>(IEnumerable<Tuple<string, object[]>> methods)
        {
            var response = base.InvokeMethods(methods);
            var results = new List<T>();
            foreach (var result in response)
            {
                if (!result["error"].IsEmpty())
                {
                    //TODO: write a log
                }
                else
                {
                    results.Add(result.Value<T>("result"));
                }
            }
            return results;
        }

        public JToken GetInfo()
        {
            return InvokeMethod<JToken>("getinfo");
        }

        public long GetNetworkHashps(int blocks = -1, int height = -1)
        {
            return InvokeMethod<long>("getnetworkhashps", blocks, height);
        }

        public int GetBlockCount()
        {
            return InvokeMethod<int>("getblockcount");
        }

        public JToken GetBlock(string hash)
        {
            return InvokeMethod<JToken>("getblock", hash);
        }

        public string GetBlockHash(int number)
        {
            return InvokeMethod<string>("getblockhash", number);
        }

        public string GetBestBlockHash()
        {
            return InvokeMethod<string>("getbestblockhash");
        }

        /// <summary>
        /// Batch request of block hashes (getblockhash)
        /// </summary>
        /// <param name="from">High block index</param>
        /// <param name="to">Low block index</param>
        /// <returns></returns>
        public List<string> GetBlockHashes(int from, int to)
        {
            var methods = new List<Tuple<string, object[]>>();
            for(int blockNumber = from; blockNumber >= to; blockNumber--)
                methods.Add(new Tuple<string, object[]>("getblockhash", new object[] { blockNumber }));
            return InvokeMethods<string>(methods);
        }

        /// <summary>
        /// Batch request of block hashes (getblockhash) by list of block numbers
        /// </summary>
        /// <param name="blockNumbers"></param>
        /// <returns></returns>
        public List<string> GetBlockHashes(IEnumerable<int> blockNumbers)
        {
            return InvokeMethods<string>(blockNumbers.Select(blockNumber => new Tuple<string, object[]>("getblockhash", new object[] { blockNumber })));
        }

        /// <summary>
        /// Batch request of blocks (getblock)
        /// </summary>
        /// <param name="hashes">Block hashes</param>
        /// <returns></returns>
        public List<JToken> GetBlocks(IEnumerable<string> hashes)
        {
            return InvokeMethods<JToken>(hashes.Select(hash => new Tuple<string, object[]>("getblock", new object[] { hash })));
        }


        public JToken GetRawMemPool(bool format = false)
        {
            return InvokeMethod<JToken>("getrawmempool", format);
        }

        public JToken GetTransaction(string txid)
        {
            return InvokeMethod<JToken>("gettransaction", txid);
        }

        public JToken GetRawTransaction(string txid)
        {
            return InvokeMethod<JToken>("getrawtransaction", txid, 1);
        }

        public List<JToken> GetRawTransactions(IEnumerable<string> txids)
        {
            return InvokeMethods<JToken>(txids.Select(txid => new Tuple<string, object[]>("getrawtransaction", new object[] { txid, 1 })));
        }

        public string CreateTransaction(object[] parameters)
        {
            return InvokeMethod<string>("createrawtransaction", parameters);
        }
        public JToken SignTransaction(object[] parameters)
        {
            return InvokeMethod<JToken>("signrawtransaction", parameters);
        }

        public string SendTransaction(string hex)
        {
            return InvokeMethod<string>("sendrawtransaction", hex);
        }
    }
}