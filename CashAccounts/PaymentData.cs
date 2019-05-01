using System;
using System.Collections.Generic;
using System.Text;

namespace CashAccountsNET
{
    public class PaymentData
    {
        public PaymentType Type { get; private set; }
        public string Address { get; private set; }

        public PaymentData(string address)
        {
            if (address.Contains(':'))
            {
                var pieces = address.ToLower().Split(':');
                switch (pieces[0])
                {
                    case "bitcoincash":
                        {
                            if (pieces[1].StartsWith('q'))
                                this.Type = PaymentType.KeyHash;
                            else if (pieces[1].StartsWith('p'))
                                this.Type = PaymentType.ScriptHash;
                        }
                        break;
                    case "simpleledger":
                        {
                            if (pieces[1].StartsWith('q'))
                                this.Type = PaymentType.SlpKeyHash;
                            else if (pieces[1].StartsWith('p'))
                                this.Type = PaymentType.SlpScriptHash;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("address", "Address prefix was not valid/recognised");
                }
                if (CashAccounts.ValidateCashAddress(address.ToLower()))
                    this.Address = address;
                else
                    throw new ArgumentException("Address was not valid", "address");
            }
            else if (address[0].Equals('P'))
            {
                this.Type = PaymentType.PaymentCode;
                if (address.IndexOfAny(CashAccounts.BASE58_CHARSET.ToCharArray()) != -1 || address.Length != 81)
                    this.Address = address;
                else
                    throw new ArgumentException("Payment Code was not valid", "address");
            }
            else
                throw new ArgumentException("Address was not valid", "address");
        }
    }
}
