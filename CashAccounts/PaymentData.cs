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
                if (CashAccounts.ValidateBech32Checksum(address.ToLower()))
                    this.Address = address;
                else
                    throw new ArgumentException("Address was not valid", "address");
            }
            else if (address[0].Equals('P'))
            {
                this.Type = PaymentType.PaymentCode;
                try
                {
                    CashAccounts.base58CheckEncoder.DecodeData(address);
                    this.Address = address;
                }
                catch
                {
                    throw new ArgumentException("Address was not valid", "address");
                }
            }
        }
    }

    public enum PaymentType : byte
    {
        KeyHash = 0x01,
        ScriptHash = 0x02,
        PaymentCode = 0x03,
        StealthKey = 0x04,
        SlpKeyHash = 0x81,
        SlpScriptHash = 0x82,
        SlpPaymentCode = 0x83,
        SlpStealthKey = 0x84
    }
}
