using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BitcoinNet;
using BitcoinNet.Crypto;
using BitcoinNet.DataEncoders;

namespace CashAccountsNET
{
    public abstract class CashAccounts
    {
        #region Blockchain Constants (Specific to Cash Accounts)
        public const int BLOCK_MODIFICATION = 563620;
        public const int ACTIVATION_HEIGHT = 563720;
        public const string ACTIVATION_BLOCKHASH = "000000000000000002abbeff5f6fb22a0b3b5c2685c6ef4ed2d2257ed54e9dcb";
        #endregion

        #region Transaction Constants (Specific to Cash Accounts)
        public static readonly byte[] PROTOCOL_PREFIX = { 0x01, 0x01, 0x01, 0x01 };
        public static readonly int[] VALID_PAYMENT_LENGTHS = { 21, 67, 81 };
        public static readonly Regex PROTOCOL_RX = new Regex(@"^[a-zA-Z0-9_]{1,99}$", RegexOptions.Compiled);
        public static readonly int[] EMOJI_CODES = { 128123, 128018, 128021, 128008, 128014, 128004, 128022, 128016, 128042, 128024, 128000, 128007, 128063, 129415,
                128019, 128039, 129414, 129417, 128034, 128013, 128031, 128025, 128012, 129419, 128029, 128030, 128375, 127803, 127794, 127796, 127797,
                127809, 127808, 127815, 127817, 127819, 127820, 127822, 127826, 127827, 129373, 129381, 129365, 127805, 127798, 127812, 129472, 129370,
                129408, 127850, 127874, 127853, 127968, 128663, 128690, 9973, 9992, 128641, 128640, 8986, 9728, 11088, 127752, 9730, 127880, 127872,
                9917, 9824, 9829, 9830, 9827, 128083, 128081, 127913, 128276, 127925, 127908, 127911, 127928, 127930, 129345, 128269, 128367, 128161,
                128214, 9993, 128230, 9999, 128188, 128203, 9986, 128273, 128274, 128296, 128295, 9878, 9775, 128681, 128099, 127838 };
        #endregion

        #region Utils (Address manipulation, encoders etc...)
        internal static readonly Base58CheckEncoder base58CheckEncoder = new Base58CheckEncoder();
        internal static readonly HexEncoder hexEncoder = new HexEncoder();

        internal const string CASH_ADDR_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        internal const string BASE58_CHARSET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        internal const byte p2pkhCashAddrByte = 0b_0_0000_000; // 160-bit hash
        internal const byte p2shCashAddrByte = 0b_0_0001_000; // 160-bit hash

        // Values calculated @ https://play.golang.org/p/o4-mMftR44D (uses PolyMod method)
        internal const ulong BCH_POLYMOD_VALUE = 1058337025301; // Main-net
        internal const ulong SLP_POLYMOD_VALUE = 982016419000; // Main-net

        internal static string EncodeCashAddress(byte[] payloadWithVersionByte, ulong polyModValue)
        {
            byte[] cashAddrBase5 = ConvertBits(payloadWithVersionByte, 8, 5);
            cashAddrBase5 = cashAddrBase5.Concat(new byte[8]);

            ulong mod = PolyMod(cashAddrBase5, polyModValue);
            for (int i = 0; i < 8; i++)
            {
                cashAddrBase5[i + 34] = (byte)((mod >> (5 * (7 - i))) & 0x1f);
            }

            var prefix = "";
            if (polyModValue == BCH_POLYMOD_VALUE)
                prefix = "bitcoincash:";
            else if (polyModValue == SLP_POLYMOD_VALUE)
                prefix = "simpleledger:";

            var cashAddr = new StringBuilder(prefix);
            for (int i = 0; i < cashAddrBase5.Length; i++)
            {
                cashAddr.Append(CASH_ADDR_CHARSET[cashAddrBase5[i]]);
            }
            return cashAddr.ToString();
        }

        internal static byte[] DecodeCashAddress(string address) // Courtesy ProtocolCash @ https://github.com/ProtocolCash/SharpBCH/blob/master/SharpBCH/CashAddress/CashAddress.cs
        {
            // split at separator colon, ensure lowercase
            var addressPieces = address.ToLower().Split(':');
            // prefix string
            var prefixStr = addressPieces[0];
            // payload string (hash + checksum)
            var payloadStr = addressPieces[1];
            // payload (hash + checksum) as byte array
            var payload = new byte[payloadStr.Length];
            for (int i = 0; i < payload.Length; i++) // decode bech32 encoded payload string
            {
                payload[i] = (byte)CASH_ADDR_CHARSET.IndexOf(payloadStr[i]);
            }
            // Drop the checksum from the payload to extract hash (+ version byte)
            var data = payload.Take(payload.Length - 8).ToArray();
            // convert to standard 8-bit from 5-bit
            data = ConvertBits(data, 5, 8, true);
            // cashAddr version byte
            var versionByte = data[0];
            // pubkey hash or script hash
            var hash = data.Skip(1).ToArray();
            return hash;
        }

        internal static bool ValidateCashAddress(string address)
        {
            var pieces = address.ToLower().Split(':');
            var prefix = pieces[0];
            var payload = new byte[pieces[1].Length];
            for (int i = 0; i < payload.Length; i++) // decode bech32 encoded payload string
            {
                payload[i] = (byte)CASH_ADDR_CHARSET.IndexOf(pieces[1][i]);
            }
            var prefixPayload = MapPrefixToPolymodInput(prefix);
            var startValue = PolyMod(prefixPayload, 1, true);
            return PolyMod(payload, startValue) == 0;
        }

        internal static byte[] MapPrefixToPolymodInput(string prefix)
        {
            var alphabet = "abcdefghijklmnopqrstuvwxyz";
            var input = new byte[prefix.Length + 1];
            for (int i = 0; i < prefix.Length; i++)
            {
                input[i] = (byte)(alphabet.IndexOf(prefix[i]) + 1);
            }
            input[prefix.Length] = 0x00; // 0 byte for ':' separator
            return input;
        }

        internal static ulong PolyMod(byte[] input, ulong startValue = 1, bool getStartValue = false) // from https://github.com/cashaddress/SharpCashAddr/blob/master/SharpCashAddr/SharpCashAddr.cs
        {
            for (uint i = 0; i < input.Length; i++)
            {
                ulong c0 = startValue >> 35;
                startValue = ((startValue & 0x07ffffffff) << 5) ^ input[i];
                if ((c0 & 0x01) > 0)
                {
                    startValue ^= 0x98f2bc8e61;
                }
                if ((c0 & 0x02) > 0)
                {
                    startValue ^= 0x79b76d99e2;
                }
                if ((c0 & 0x04) > 0)
                {
                    startValue ^= 0xf33e5fb3c4;
                }
                if ((c0 & 0x08) > 0)
                {
                    startValue ^= 0xae2eabe2a8;
                }
                if ((c0 & 0x10) > 0)
                {
                    startValue ^= 0x1e4f43e470;
                }
            }
            return getStartValue ? startValue : startValue ^ 1;
        }

        internal static byte[] ConvertBits(byte[] data, int from, int to, bool strictMode = false) // Courtesy ProtocolCash @ https://github.com/ProtocolCash/SharpBCH/blob/master/SharpBCH/CashAddress/CashAddress.cs
        {
            var d = data.Length * from / (double)to;
            var length = strictMode ? (int)Math.Floor(d) : (int)Math.Ceiling(d);
            var mask = (1 << to) - 1;
            var result = new byte[length];
            var index = 0;
            var accumulator = 0;
            var bits = 0;
            foreach (var value in data)
            {
                accumulator = (accumulator << from) | value;
                bits += from;
                while (bits >= to)
                {
                    bits -= to;
                    result[index] = (byte)((accumulator >> bits) & mask);
                    ++index;
                }
            }

            if (strictMode) return result;
            if (bits <= 0) return result;

            result[index] = (byte)((accumulator << (to - bits)) & mask);
            ++index;

            return result;
        }
        #endregion

        #region CashAccounts Specific Methods
        internal static PaymentData[] ProcessPaymentData(Transaction tx)
        {
            List<byte[]> rawPaymentDataList = new List<byte[]>();
            foreach (var output in tx.Outputs)
            {
                var ops = output.ScriptPubKey.ToOps().ToArray(); // Present raw output as and array of Ops
                if (ops.ElementAt(0).Code == OpcodeType.OP_RETURN && ops.ElementAt(1).PushData.SequenceEqual(PROTOCOL_PREFIX)) // check if output adheres to the protocol specification. ignores non-OP_RETURN outputs
                {
                    bool isExhausted = false;
                    int pushDataIndex = 3; // begins the search for raw payment data in the output, skipping OP_RETURN, PROTOCOL_PREFIX and Account Name
                    while (!isExhausted) // adds raw payment data to a list until there is no specified payment left in output
                    {
                        try
                        {
                            rawPaymentDataList.Add(ops.ElementAt(pushDataIndex).PushData);
                            pushDataIndex++;
                        }
                        catch
                        {
                            isExhausted = true;
                        }
                    }
                    break;
                }
            }

            List<PaymentData> paymentDataList = new List<PaymentData>(); // list of parsed payment data that will be populated and returned

            foreach (var rawPaymentBytes in rawPaymentDataList)
            {
                var bytesList = rawPaymentBytes.ToList(); // easier to work with a list :)
                switch (rawPaymentBytes.First()) // parse raw payment data based on what payment type is specified in registration
                {
                    case (byte)PaymentType.KeyHash:
                        {
                            bytesList.RemoveAt(0);
                            bytesList.Insert(0, p2pkhCashAddrByte);
                            var address = EncodeCashAddress(bytesList.ToArray(), BCH_POLYMOD_VALUE);
                            var paymentData = new PaymentData(address);
                            paymentDataList.Add(paymentData);
                        }
                        break;
                    case (byte)PaymentType.ScriptHash:
                        {
                            bytesList.RemoveAt(0);
                            bytesList.Insert(0, p2shCashAddrByte);
                            var address = EncodeCashAddress(bytesList.ToArray(), BCH_POLYMOD_VALUE);
                            var paymentData = new PaymentData(address);
                            paymentDataList.Add(paymentData);
                        }
                        break;
                    case (byte)PaymentType.PaymentCode:
                        {
                            bytesList.RemoveAt(0);
                            bytesList.Insert(0, 0x47);
                            var address = base58CheckEncoder.EncodeData(bytesList.ToArray());
                            var paymentData = new PaymentData(address);
                            paymentDataList.Add(paymentData);
                        }
                        break;
                    case (byte)PaymentType.StealthKey:
                        {
                            throw new NotImplementedException();
                            // TODO
                        }
                        break;
                    case (byte)PaymentType.SlpKeyHash:
                        {
                            bytesList.RemoveAt(0);
                            bytesList.Insert(0, p2pkhCashAddrByte);
                            var address = EncodeCashAddress(bytesList.ToArray(), SLP_POLYMOD_VALUE);
                            var paymentData = new PaymentData(address);
                            paymentDataList.Add(paymentData);
                        }
                        break;
                    case (byte)PaymentType.SlpScriptHash:
                        {
                            bytesList.RemoveAt(0);
                            bytesList.Insert(0, p2shCashAddrByte);
                            var address = EncodeCashAddress(bytesList.ToArray(), SLP_POLYMOD_VALUE);
                            var paymentData = new PaymentData(address);
                            paymentDataList.Add(paymentData);
                        }
                        break;
                    case (byte)PaymentType.SlpPaymentCode:
                        {
                            throw new NotImplementedException();
                            // TODO
                        }
                        break;
                    case (byte)PaymentType.SlpStealthKey:
                        {
                            throw new NotImplementedException();
                            // TODO
                        }
                        break;
                    default:
                        throw new Exception("Payment Type declaration in registration transaction not recognised");
                }
            }

            var paymentDataTmpList = paymentDataList.ToList();
            paymentDataList.Clear();

            // really hacky way of ordering payment data, prioritising first bch over slp, then reusable addresses over static addresses
            paymentDataTmpList.Where(p => p.Type == PaymentType.StealthKey).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.PaymentCode).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.KeyHash).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.ScriptHash).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.SlpStealthKey).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.SlpPaymentCode).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.SlpKeyHash).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.SlpScriptHash).ToList().ForEach(p => paymentDataList.Add(p));

            return paymentDataList.ToArray();
        }

        internal static Script GetRegistrationScript(string accountName, IEnumerable<PaymentData> paymentData)
        {
            var payloads = new List<byte[]>();
            foreach (var payment in paymentData)
            {
                switch (payment.Type)
                {
                    case PaymentType.KeyHash:
                        {
                            var payload = new byte[] { (byte)payment.Type }.Concat(DecodeCashAddress(payment.Address)).ToArray();
                            if (VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.ScriptHash:
                        {
                            var payload = new byte[] { (byte)payment.Type }.Concat(DecodeCashAddress(payment.Address)).ToArray();
                            if (VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.PaymentCode:
                        {
                            var paymentCodePayload = base58CheckEncoder.DecodeData(payment.Address).ToList();
                            paymentCodePayload.RemoveAt(0);
                            var payload = new byte[] { (byte)payment.Type }.Concat(paymentCodePayload.ToArray());
                            if (VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Payment Code was out of Range!");
                        }
                        break;
                    case PaymentType.StealthKey:
                        throw new NotImplementedException();
                        // TODO
                        break;
                    case PaymentType.SlpKeyHash:
                        {
                            var payload = new byte[] { (byte)payment.Type }.Concat(DecodeCashAddress(payment.Address)).ToArray();
                            if (VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.SlpScriptHash:
                        {
                            var payload = new byte[] { (byte)payment.Type }.Concat(DecodeCashAddress(payment.Address)).ToArray();
                            if (VALID_PAYMENT_LENGTHS.Contains(payload.Length))
                                payloads.Add(payload);
                            else
                                throw new ArgumentOutOfRangeException("Address", "Address was out of Range!");
                        }
                        break;
                    case PaymentType.SlpPaymentCode:
                        throw new NotImplementedException();
                        // TODO
                        break;
                    case PaymentType.SlpStealthKey:
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
                Op.GetPushOp(PROTOCOL_PREFIX),
                Op.GetPushOp(Encoding.UTF8.GetBytes(accountName))
            };

            foreach (var payload in payloads)
            {
                ops.Add(Op.GetPushOp(payload));
            }

            return new Script(ops);
        }

        internal static string ParseAccountName(Transaction tx)
        {
            string name = null;
            foreach (var output in tx.Outputs)
            {
                var ops = output.ScriptPubKey.ToOps().ToArray();
                if (ops.ElementAt(0).Code == OpcodeType.OP_RETURN && ops.ElementAt(1).PushData.SequenceEqual(PROTOCOL_PREFIX))
                {
                    name = Encoding.UTF8.GetString(ops.ElementAt(2).PushData);
                    break;
                }
            }
            if (PROTOCOL_RX.IsMatch(name))
                return name;
            else
                throw new Exception("Parsed Name does not adhere to Regular Expression");
        }

        internal static string CalculateCollisionId(string blockHash, string txid)
        {
            // If 2 or more registrations with same account name made in same block (hence same account number), unique account must be resolved with the Collision ID (Collision Hash)
            // Documentation here: https://gitlab.com/cash-accounts/specification/blob/master/SPECIFICATION.md#collision-hash

            string concatResultString = blockHash + txid; // Step 1: Concatenate the block hash with the transaction hash
            var hexEnconder = new HexEncoder();
            byte[] concatResultBytes = hexEnconder.DecodeData(concatResultString);

            byte[] hash = Hashes.SHA256(concatResultBytes); // Step 2: Hash the results of the concatenation with sha256
            var hashBytesList = hash.ToList();
            hashBytesList.RemoveRange(4, hashBytesList.Count - 4); // Step 3: Take the first four bytes and discard the rest
            byte[] hashBytes = hashBytesList.ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(hashBytes);
            uint number = BitConverter.ToUInt32(hashBytes, 0);
            string numberString = number.ToString(); // Step 4: Convert to decimal notation and store as a string
            char[] charArray = numberString.ToCharArray();
            Array.Reverse(charArray); // Step 5: Reverse the the string so the last number is first
            string collisionId = new string(charArray);
            while (collisionId.Length < 10)
                collisionId = collisionId.Insert(collisionId.Length, "0"); // Step 6: Right pad the string with zeroes up to a string length of 10.
            return collisionId;
        }

        internal static string CalculateEmoji(string blockHash, string txid)
        {
            string concatResultString = blockHash + txid; // Step 1: Concatenate the block hash with the transaction hash
            byte[] concatResultBytes = hexEncoder.DecodeData(concatResultString);
            byte[] hash = Hashes.SHA256(concatResultBytes); // Step 2: Hash the results of the concatenation with sha256
            var hashBytesList = hash.ToList();
            hashBytesList.RemoveRange(0, hashBytesList.Count - 4); // Step 3: Take the last four bytes and discard the rest
            byte[] hashBytes = hashBytesList.ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(hashBytes);
            uint number = BitConverter.ToUInt32(hashBytes, 0); // Step 4: Convert to decimal notation
            long emojiIndex = Convert.ToInt64(number) % 100; // Step 5: Modulus by 100.
            int emojiCode = EMOJI_CODES.ElementAt(Convert.ToInt32(emojiIndex)); // Step 6: Take the emoji at the given position in the emoji list.
            string emoji = char.ConvertFromUtf32(emojiCode);
            return emoji;
        }
        #endregion
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
