using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlockExplorerAPI.Models.Requests
{
    public class CreateTransactionRequest
    {
        private decimal _amount;

        public string PrivateKey { get; set; }
        public string Address { get; set; }
        public string DestinationAddress { get; set; }
        public decimal Value
        {
            get { return _amount; }
            set
            {
                if (value > 0) this._amount = value;
            }
        }
    }
}