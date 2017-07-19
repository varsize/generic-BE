using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using BlockExplorerAPI.Services.Models;

namespace BlockExplorerAPI.Models.Responses
{
    public class BlockItemModel
    {
        public string Hash { get; private set; }
        public int Size { get; private set; }
        public int Height { get; private set; }
        public long Time { get; private set; }
        public decimal Mint { get; private set; }
        public string Type { get; private set; }
        public int TxCount { get; private set; }
        public decimal CoinsSent { get; private set; }

        public BlockItemModel(JToken block, List<TransactionModel> transactions)
        {
            Hash = block["hash"].Value<string>();
            Size = block["size"].Value<int>();
            Height = block["height"].Value<int>();
            Time = block["time"].Value<long>();
            Mint = block["mint"].Value<decimal>();
            Type = ParseBlockType(block);
            TxCount = transactions.Count;
            CoinsSent = transactions.Sum(tx => (decimal?) tx.OutputsValue) ?? 0;
        }

        public static string ParseBlockType(JToken block)
        {
            try
            {
                string flags = block["flags"].Value<string>();
                if (!string.IsNullOrEmpty(flags))
                {
                    if (flags.Contains("proof-of-stake"))
                        return "PoS";
                    if (flags.Contains("proof-of-work"))
                        return "PoW";
                }
            }
            catch
            {
            }
            return "";
        }
    }
}