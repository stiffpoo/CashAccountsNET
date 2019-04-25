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
                PaymentData = paymentData.ToList(),
                OutputScript = null
            };

            var payloads = new List<byte[]>();
            foreach (var payment in registration.PaymentData)
            {
                switch (payment.Type)
                {
                    case PaymentType.KeyHash:
                        {
                            var addr = BitcoinAddress.CreateFromAny(payment.Address, Network.Main);
                            var payload = new byte[] { (byte)payment.Type }.Concat(addr.ScriptPubKey.GetDestination().ToBytes());
                            if (CashAccounts.VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.ScriptHash:
                        {
                            var addr = BitcoinAddress.CreateFromAny(payment.Address, Network.Main);
                            var payload = new byte[] { (byte)payment.Type }.Concat(addr.ScriptPubKey.GetDestination().ToBytes());
                            if (CashAccounts.VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.PaymentCode:
                        {
                            var paymentCodePayload = CashAccounts.base58CheckEncoder.DecodeData(payment.Address).ToList();
                            paymentCodePayload.RemoveAt(0);
                            var payload = new byte[] { (byte)payment.Type }.Concat(paymentCodePayload.ToArray());
                            if (CashAccounts.VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Payment Code was out of Range!");
                        }
                        break;
                    case PaymentType.StealthKey:
                        throw new NotImplementedException();
                        // TODO
                        break;
                    case PaymentType.TokenKeyHash:
                        {
                            var payload = new byte[] { (byte)payment.Type }.Concat(CashAccounts.DecodeCashAddress(payment.Address)).ToArray();
                            if (CashAccounts.VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.TokenScriptHash:
                        {
                            var payload = new byte[] { (byte)payment.Type }.Concat(CashAccounts.DecodeCashAddress(payment.Address)).ToArray();
                            if (CashAccounts.VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.TokenPaymentCode:
                        throw new NotImplementedException();
                        // TODO
                        break;
                    case PaymentType.TokenStealthKey:
                        throw new NotImplementedException();
                        // TODO
                        break;
                    default:
                        break;
                }
            }

            var ops = new List<Op>
            {
                OpcodeType.OP_RETURN,
                Op.GetPushOp(CashAccounts.PROTOCOL_PREFIX),
                Op.GetPushOp(Encoding.UTF8.GetBytes(registration.Name))
            };

            foreach (var payload in payloads)
            {
                ops.Add(Op.GetPushOp(payload));
            }
            registration.OutputScript = new Script(ops);

            return registration;
        }
    }
}
