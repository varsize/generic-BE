using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlockExplorerAPI.Services.Models
{
    public class RawTransaction
    {
        public string hex { get; set; }
        public string txid { get; set; }
        public string version { get; set; }
        public string locktime { get; set; }
        public string time { get; set; }
        public List<RawTransactionVin> vin { get; set; }
        public List<RawTransactionVout> vout { get; set; }
        public string blockhash { get; set; }
        public int confirmations { get; set; }
        public int blocktime { get; set; }

    }

    public class RawTransactionVin
    {
        public string txid { get; set; }
        public int vout { get; set; }
        public RawTransactionVinScriptSig scriptSig { get; set; }
        public long sequence { get; set; }
    }

    public class RawTransactionVinScriptSig
    {
        public string asm { get; set; }
        public string hex { get; set; }
    }
    
    public class RawTransactionVout
    {
        public decimal value { get; set; }
        public int n { get; set; }
        public RawTransactionVoutScriptPubKey scriptPubKey { get; set; }
    }

    public class RawTransactionVoutScriptPubKey
    {
        public string asm { get; set; }
        public string hex { get; set; }
        public int reqSigs { get; set; }
        public string type { get; set; }
        public List<string> addresses { get; set; }

    }
}