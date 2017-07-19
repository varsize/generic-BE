using System.Collections.Generic;

namespace BlockExplorerAPI.Services.Models
{
    public class AddressModel
    {
        public string Address { get; set; }
        public decimal TotalReceived { get; set; }
        public decimal TotalSent { get; set; }
        public decimal TotalBalance { get { return TotalReceived - TotalSent; } }
        public List<UnspentOutputModel> UnspentOutputs { get; set; }

        public List<AddressTransactionModel> Transactions { get; private set; }

        public AddressModel(string address)
        {
            Address = address;
            Transactions = new List<AddressTransactionModel>();
            UnspentOutputs = new List<UnspentOutputModel>();
        }

        public class UnspentOutputModel
        {
            public string txid { get; set; }
            public int vout { get; set; }
            public string address { get; set; }
            public string scriptPubKey { get; set; }
            public decimal amount { get; set; }
            public RawTransaction raw { get; set; }
        }
    }
}