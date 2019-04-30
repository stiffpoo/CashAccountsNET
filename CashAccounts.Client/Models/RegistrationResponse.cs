using System;
using System.Collections.Generic;
using System.Text;

namespace CashAccountsNET.Client.Models
{
    public class RegistrationResponse
    {
        public string Txid { get; internal set; }
        public string RawTxHex { get; internal set; }

        internal RegistrationResponse()
        {

        }
    }
}
