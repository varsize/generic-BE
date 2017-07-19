using System.Collections.Generic;
using BlockExplorerAPI.DAL.DTO;
using Newtonsoft.Json;

namespace BlockExplorerAPI.Services.Models
{
    public class AddressTransactionModel
    {
        public string Txid { get; protected set; }
        public long Time { get; protected set; }
        public int Size { get; set; }
        private int _confirmations;
        public int Confirmations
        {
            get { return _confirmations; }
            set
            {
                if (value < 0)
                {
                    _confirmations = 0;
                    DoubleSpend = true;
                }
                else
                {
                    _confirmations = value;
                }
            }
        }

        [JsonIgnore]
        public string BlockHash { get; set; }
        public bool DoubleSpend { get; private set; }
        public ICollection<string> Inputs { get; protected set; }
        public List<OutputModel> Outputs { get; protected set; }
        public decimal OutputsValue { get; set; }

        public AddressTransactionModel()
        {
            Inputs = new HashSet<string>();
            Outputs = new List<OutputModel>();
        }

        public AddressTransactionModel(Transaction transaction)
            : this()
        {
            Txid = transaction.Id;
            Time = transaction.Time;
            Size = transaction.Size;
        }

        public AddressTransactionModel(Transaction transaction, int blockCount)
            : this()
        {
            Txid = transaction.Id;
            Time = transaction.Time;
            Size = transaction.Size;
            Confirmations = transaction.BlockNumber.HasValue ? (blockCount - transaction.BlockNumber.Value + 1) : 0;
        }
    }

    public class OutputModel
    {
        public string Address { get; private set; }
        public decimal Value { get; set; }

        public OutputModel(string address, decimal value)
        {
            Address = address;
            Value = value;
        }
    }
}