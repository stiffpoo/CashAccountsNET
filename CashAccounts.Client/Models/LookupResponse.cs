using System;
using System.Collections.Generic;
using System.Text;

namespace CashAccountsNET.Client.Models
{
    public class LookupResponse
    {
        public string RawTxHex { get; internal set; }
        public string InclusionProof { get; internal set; }

        internal LookupResponse()
        {

        }
    }
}
