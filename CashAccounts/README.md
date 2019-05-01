## CashAccounts

###### Dependencies
- [BitcoinNET](https://github.com/Lokad/BitcoinNET) (Bitcoin Cash fork of [NBitcoin](https://github.com/MetacoSA/NBitcoin))

#### Supports
- Cash Addresses (Bech32)
    - P2PKH (bitcoincash:q....)
    - P2SH (bitcoincash:p...)
- Simple Ledger Addresses
    - P2PKH (simpleledger:q...)
    - P2SH (simpleledger:p...)
- [BIP47](https://github.com/bitcoin/bips/blob/master/bip-0047.mediawiki) Payment Codes

### Usage
Using statement:
```c#
using CashAccountsNET;
```
Parsing an account from a confirmed transaction:
```c#
string rawTxHex = "01000000017cc04d29109cb43a0bfade3993b5840b6f68f22e09d4806f26fe9e7d772fc72f010000006a47304402207b0da3150bf9a44a8fae7333f4d5b03ba1297dd21c8641880efea9eaa9e1e89d022028d2a8d840771d4a87c84f0b217d02f17c3f57832fe0c37ac27b5afc27004fbe41210355f64f0ed04944eb477b33dcb46bb45453b8988bba1862698abe7343c6f0e2c6ffffffff020000000000000000256a0401010101084a6f6e617468616e1501ebdeb6430f3d16a9c6758d6c0d7a400c8e6bbee4c00c1600000000001976a914efd03e75f2aedb19261b39a6c8361c7bccd9f4f088ac00000000";
string inclusionProof = "0000c0204895ef83b69cd64a7901fee54858461bfc0c09cb74a6b0000000000000000000cdd0c434d6ee0aee331016d1f9d2bef0bbcd26e003b784a39f392f2f2a50bfda6f532e5c6ea304184467d54bd300000009c33356693bc1c8928b96621d832c510e6e239ae52f0ee27ab28a44643313e0c65eb8a4982058bb5e86cf84653c797d51741a90ca6c1a2518c1ba871402cb76e31faf65208ce05d15d4720cc209c02cea0270107c59af586add632f128fde00e16951e442a7aa1fafe6464ae63cd1797a9c6c0b24128d0ec61ec856b7ef04672036715eae4e2df35b3d30295df5cbbd71198d9ebb94918fe00eaf047edf1f0d5912b92bd2afd8911d303d691a7e5b873dab49e4f7b09b74d83a9025c18ac11f5926aa34a21c258cf49dd10909bec8c3d603648c2d86f90938ddc39470ee4ca98b1fb673951453599354f1d56d8f710c89d2bde5835a66b565df382d04c3e6eac3f98b52bb82f830a80d718e25f6adfcb93ec2b89cf2d56d610ce9238373b2cfab03bb1a00";
int blockHeight = 563720;
string blockHash = "000000000000000002abbeff5f6fb22a0b3b5c2685c6ef4ed2d2257ed54e9dcb";

var account = Account.Parse(rawTxHex, inclusionProof, blockHeight, BlockHash);

Console.WriteLine(account.Name); // Jonathan
Console.WriteLine(account.CollisionId); // 5876958390
Console.WriteLine(account.Emoji); // ☯
Console.WriteLine(account.PaymentData[0].Address); // bitcoincash:qr4aadjrpu73d2wxwkxkcrt6gqxgu6a7usxfm96fst
```
Creating a new Account Registration:
```c#
string name = "Timothy";
var payment = new PaymentData[]
{
    new PaymentData("bitcoincash:qzj8jyvyqvhvr0gxhy7uhqcnyc99mv77asxn9sekqu"),
    new PaymentData("simpleledger:qqra6zy6c2k3x86whvpcr4we4e8xlnayhcajhv0nch"),
    new PaymentData("PM8TJa84G8yD81hRCF7kWoDcXDEgSdYNXWWe26HRGzwszeC1uc7JwUyZkqkTSQ2sjRXPEqVAST9aN4wrmXfvf59vzsDxCHbVuwB1oe5gKnR2nfkVvhcc");
}

var registration = AccountRegistration.Create(name, payment);

Console.WriteLine(registration.OutputScript) // OP_RETURN 01010101 54696d6f746879 0301000345765a84a03c288708eb35e53e4cecfd0f113db8968235a4c3887f97dbceada0e20064dab0909bcad5ecd7388cddf3ae0cc57fa57135418230974002f9504d1e00000000000000000000000000 01a4791184032ec1bd06b93dcb8313260a5db3deec 8107dd089ac2ad131f4ebb0381d5d9ae4e6fcfa4be
```
