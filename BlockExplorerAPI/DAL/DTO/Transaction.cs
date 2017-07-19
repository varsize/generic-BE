using System;
using System.Collections.Generic;

namespace BlockExplorerAPI.DAL.DTO
{
    [Serializable]
    public class Transaction
    {
        public string Id { get; set; }
        public int? BlockNumber { get; set; }
        public string BlockHash { get; set; }
        public long Time { get; set; }
        public int Size { get; set; }
        public ICollection<Input> Inputs { get; set; }
        public ICollection<Output> Outputs { get; set; }
    }
}