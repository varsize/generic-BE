using System;

namespace BlockExplorerAPI.DAL.DTO
{
    [Serializable]
    public class Input
    {
        public string TxId { get; set; }
        public Transaction Transaction { get; set; }
        public string Address { get; set; }
        public decimal Value { get; set; }
        public string ParentTxId { get; set; }
        public int Vout { get; set; }
    }
}