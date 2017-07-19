using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlockExplorerAPI.Services.Models.CreateTransaction
{
    public class SignTransactionRpcResponse
    {
        public string hex { get; set; }
        public bool complete { get; set; }
    }
}