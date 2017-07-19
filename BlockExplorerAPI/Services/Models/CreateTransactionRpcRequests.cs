using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlockExplorerAPI.Services.Models.CreateTransaction
{
    public class CreateTransactionRpcRequests
    {
        public List<VoutPair> pairs { get; set; }
        public Dictionary<string, decimal> destination { get; set;}

        public class VoutPair
        {
            public string txid { get; set; }
            public int vout { get; set; }
        }

        public CreateTransactionRpcRequests()
        {
            pairs = new List<VoutPair>();
            destination = new Dictionary<string, decimal>();
        }

        public object[] toJson()
        {
            return new object[] { JToken.FromObject(pairs), JToken.FromObject(destination) };
        }
    }

    public class SignTransactionRpcRequests
    {
        public string hash { get; set; }

        public List<VoutKeyTriple> vout { get; set; }

        public List<string> keySet { get; set; }

        public class VoutKeyTriple
        {
            public string txid { get; set; }
            public int vout { get; set; }
            public string scriptPubKey { get; set; }
        }

        public SignTransactionRpcRequests()
        {
            vout = new List<VoutKeyTriple>();
            keySet = new List<string>();
        }

        public object[] toJson()
        {
            return new object[] { hash, JToken.FromObject(vout), JToken.FromObject(keySet) };
        }
    }

    public class SendTransactionRpcRequests
    {
        public string hash { get; set; }
        public object[] toJson()
        {
            return new object[] { hash };
        }
    }
}