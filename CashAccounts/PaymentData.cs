using System;
using System.Collections.Generic;
using System.Text;

namespace CashAccountsNET
{
    public class PaymentData
    {
        public PaymentType Type { get; set; }
        public string Address { get; set; }

        public PaymentData()
        {

        }

        public PaymentData(string address, PaymentType paymentType)
        {
            this.Address = address;
            this.Type = paymentType;
        }
    }

    public enum PaymentType : byte
    {
        KeyHash = 0x01,
        ScriptHash = 0x02,
        PaymentCode = 0x03,
        StealthKey = 0x04,
        TokenKeyHash = 0x81,
        TokenScriptHash = 0x82,
        TokenPaymentCode = 0x83,
        TokenStealthKey = 0x84
    }
}
