using System;
using System.Collections.Generic;
using System.Text;

namespace CashAccountsNET.Client.Models
{
    public class AccountMetadata
    {
        public string Identifier { get; set; }
        public AccountInformation Information { get; set; }

        internal AccountMetadata()
        {

        }
    }

    public class AccountInformation
    {
        public string Emoji { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public CollisionInformation Collision { get; set; }
        public List<PaymentInformation> Payment { get; set; }
    }

    public class CollisionInformation
    {
        public string Hash { get; set; }
        public int Count { get; set; }
        public int Length { get; set; }
    }

    public class PaymentInformation
    {
        public string Type { get; set; }
        public string Address { get; set; }
    }
}
