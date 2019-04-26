using BitcoinNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CashAccountsNET
{
    public class AccountRegistration
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (CashAccounts.PROTOCOL_RX.IsMatch(value))
                    _name = value;
                else
                    throw new ArgumentOutOfRangeException("Account Name", "Account Name does not match Protocol Regular Expression");
            }
        }
        public List<PaymentData> PaymentData { get; set; }
        public Script OutputScript { get; private set; }

        private AccountRegistration()
        {

        }

        public static AccountRegistration Create(string accountName, IEnumerable<PaymentData> paymentData)
        {
            var registration = new AccountRegistration()
            {
                Name = accountName,
                PaymentData = new List<PaymentData>(),
                OutputScript = new Script()
            };

            paymentData.Where(p => p.Type == PaymentType.StealthKey).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.PaymentCode).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.KeyHash).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.ScriptHash).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.SlpStealthKey).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.SlpPaymentCode).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.SlpKeyHash).ToList().ForEach(p => registration.PaymentData.Add(p));
            paymentData.Where(p => p.Type == PaymentType.SlpScriptHash).ToList().ForEach(p => registration.PaymentData.Add(p));

            registration.OutputScript = CashAccounts.GetRegistrationScript(registration.Name, registration.PaymentData);

            return registration;
        }
    }
}
