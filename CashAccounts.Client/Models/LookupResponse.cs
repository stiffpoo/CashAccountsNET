using System;
using System.Collections.Generic;
using System.Text;

namespace CashAccountsNET.Client
{
    public class LookupResponse
    {
        public string RawTransactionString { get; internal set; }
        public string InclusionProofString { get; internal set; }
    }
}
