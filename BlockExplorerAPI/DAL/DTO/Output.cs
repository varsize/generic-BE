using System;

namespace BlockExplorerAPI.DAL.DTO
{
    [Serializable]
    public class Output
    {
        public string TxId { get; set; }
        public Transaction Transaction { get; set; }
        public string Address { get; set; }
        public decimal Value { get; set; }
        public int Vout { get; set; }
    }
}