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
        #region Blockchain Constants
        public const int BLOCK_MODIFICATION = 563620;
        public const int ACTIVATION_HEIGHT = 563720;
        public const string ACTIVATION_BLOCKHASH = "000000000000000002abbeff5f6fb22a0b3b5c2685c6ef4ed2d2257ed54e9dcb";
        #endregion

        #region Transaction Constants
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

        #region Utils
        public static readonly Base58CheckEncoder base58CheckEncoder = new Base58CheckEncoder();
        public static readonly HexEncoder hexEncoder = new HexEncoder();
        #endregion

        internal static List<PaymentData> ProcessPaymentData(Transaction tx)
        {
            List<byte[]> rawPaymentDataList = new List<byte[]>();
            foreach (var output in tx.Outputs)
            {
                var ops = output.ScriptPubKey.ToOps().ToArray(); // Break-up raw output into easily manipulable IEnumerable<Op>
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
                }
            }

            List<PaymentData> paymentDataList = new List<PaymentData>(); // list of parsed payment data that will be populated and returned

            foreach (var rawPaymentBytes in rawPaymentDataList)
            {
                switch (rawPaymentBytes.First())
                {
                    case (byte)PaymentType.KeyHash:
                        {
                            var bytesList = rawPaymentBytes.ToList();
                            bytesList.RemoveAt(0);
                            var keyHash = new KeyId(bytesList.ToArray());
                            var address = keyHash.GetAddress(Network.Main);
                            var paymentData = new PaymentData()
                            {
                                Type = PaymentType.KeyHash,
                                Address = address.ToString()
                            };
                            paymentDataList.Add(paymentData);
                            break;
                        }
                    case (byte)PaymentType.ScriptHash:
                        {
                            var bytesList = rawPaymentBytes.ToList();
                            bytesList.RemoveAt(0);
                            var scriptHash = new ScriptId(bytesList.ToArray());
                            var address = scriptHash.GetAddress(Network.Main);
                            var paymentData = new PaymentData()
                            {
                                Type = PaymentType.ScriptHash,
                                Address = address.ToString()
                            };
                            paymentDataList.Add(paymentData);
                            break;
                        }
                    case (byte)PaymentType.PaymentCode:
                        {
                            var bytesList = rawPaymentBytes.ToList();
                            bytesList.RemoveAt(0);
                            bytesList.Insert(0, 0x47);
                            var address = base58CheckEncoder.EncodeData(bytesList.ToArray());
                            var paymentData = new PaymentData()
                            {
                                Type = PaymentType.PaymentCode,
                                Address = address
                            };
                            paymentDataList.Add(paymentData);
                            break;
                        }
                    case (byte)PaymentType.StealthKey:
                        {
                            throw new NotImplementedException();
                            // TODO
                            break;
                        }
                    case (byte)PaymentType.TokenKeyHash:
                        {
                            throw new NotImplementedException();
                            // TODO
                            break;
                        }
                    case (byte)PaymentType.TokenScriptHash:
                        {
                            throw new NotImplementedException();
                            // TODO
                            break;
                        }
                    case (byte)PaymentType.TokenPaymentCode:
                        {
                            throw new NotImplementedException();
                            // TODO
                            break;
                        }
                    case (byte)PaymentType.TokenStealthKey:
                        {
                            throw new NotImplementedException();
                            // TODO
                            break;
                        }
                    default:
                        break;
                }
            }
            var paymentDataTmpList = paymentDataList.ToList();
            paymentDataList.Clear();
            paymentDataTmpList.Where(p => p.Type == PaymentType.StealthKey).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.PaymentCode).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.KeyHash).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.ScriptHash).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.TokenStealthKey).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.TokenPaymentCode).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.TokenKeyHash).ToList().ForEach(p => paymentDataList.Add(p));
            paymentDataTmpList.Where(p => p.Type == PaymentType.TokenScriptHash).ToList().ForEach(p => paymentDataList.Add(p));

            return paymentDataList;
        }

        internal static string ParseAccountName(Transaction tx)
        {
            string name = null;
            foreach (var output in tx.Outputs)
            {
                var ops = output.ScriptPubKey.ToOps().ToArray();
                if (ops.ElementAt(0).Code == OpcodeType.OP_RETURN && ops.ElementAt(1).PushData.SequenceEqual(PROTOCOL_PREFIX))
                    name = Encoding.UTF8.GetString(ops.ElementAt(2).PushData);
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
    }
}
