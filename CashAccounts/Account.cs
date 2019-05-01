using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinNet;

namespace CashAccountsNET
{
    public class Account
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
        public int Number { get => this.BlockHeight - CashAccounts.BLOCK_MODIFICATION; }
        private string _collisionId;
        public string CollisionId
        {
            get
            {
                if (_collisionId == null)
                {
                    if (string.IsNullOrEmpty(this.BlockHash))
                        throw new ArgumentNullException("Block Hash", "Can not calculate Collision ID without having defined a Block Hash");
                    if (string.IsNullOrEmpty(this.Txid))
                        throw new ArgumentNullException("TXID", "Can not calculate Collision ID without having defined a TXID");
                    _collisionId = CashAccounts.CalculateCollisionId(this.BlockHash, this.Txid);
                }
                return _collisionId;
            }
        }
        private string _emoji;
        public string Emoji
        {
            get
            {
                if (_emoji == null)
                {
                    if (string.IsNullOrEmpty(this.BlockHash))
                        throw new ArgumentNullException("BlockHash", "Can not calculate Collision ID without having defined a Block Hash");
                    if (string.IsNullOrEmpty(this.Txid))
                        throw new ArgumentNullException("Txid", "Can not calculate Collision ID without having defined a TXID");
                    _emoji = CashAccounts.CalculateEmoji(this.BlockHash, this.Txid);
                }
                return _emoji;
            }
        }
        public PaymentData[] PaymentData { get; private set; }
        public string Txid { get; private set; }
        public string RawTxHex { get; private set; }
        private int _blockHeight;
        public int BlockHeight
        {
            get => _blockHeight;
            set
            {
                if (value >= CashAccounts.ACTIVATION_HEIGHT)
                    _blockHeight = value;
                else
                    throw new ArgumentOutOfRangeException("Block Height", "Block Height can not be less than than 563720 (Protocol Activation Height)");
            }
        }
        public string BlockHash { get; private set; }
        public string InclusionProof { get; private set; }

        private Account()
        {

        }

        public static Account Parse(string rawTxHex, string inclusionProof, int blockHeight, string blockHash)
        {
            var registrationTx = Transaction.Parse(rawTxHex, Network.Main);
            var account = new Account()
            {
                Name = CashAccounts.ParseAccountName(registrationTx),
                PaymentData = CashAccounts.ProcessPaymentData(registrationTx),
                Txid = registrationTx.GetHash().ToString(),
                InclusionProof = inclusionProof,
                RawTxHex = rawTxHex,
                BlockHeight = blockHeight,
                BlockHash = blockHash
            };
            return account;
        }
    }
}
