## CashAccounts.Client

###### Dependencies
- [CashAccounts](https://github.com/stiffpoo/CashAccountsNET/tree/master/CashAccounts)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (High-performance JSON library)

#### Supports
- Lookup Services (Retrieval of Registrations & Inclusion Proofs to trustlessly parse account data)
- Metadata Services (Retrieval of Human-Readable account information, reliant on server)
- Registration Services (Submission of a new account with payment data to be broadcast by a supporting server)

### Usage
Using statements:
```c#
using CashAccountsNET;
using CashAccountsNET.Client;
```
Retrieving Payment Data for zzasdfcollision#406.71;
```c#
var client = new AccountClient(); // Picks a server at random
client = new AccountClient(new Uri("http://api.cashaccount.info/")); // Specify your own lookup server

var accounts = new List<Account>();
var lookup = client.LookupAccount("zzasdfcollision", 406);

int blockHeight = 406 + CashAccountsNET.CashAccounts.BLOCK_MODIFICATION; // Block Height = Account number + 563620
string blockHash = "000000000000000003b66524cda7cd6ccbd25add9d9551b7367e9ceaa85b8c20";

foreach (var response in lookup)
{
    var account = Account.Parse(
        response.RawTxHex,
        response.InclusionProof,
        blockHeight,
        blockHash);
    accounts.Add(account);
}

Account zzasdfcollision;
foreach (var account in accounts)
{
    if (account.CollisionId.StartsWith("71"))
        zzasdfcollision = account;
}

// It would probably be wise to verify the registration transaction was confirmed
// in the blockchain using SPV validation. Subject to your implementation.

Console.WriteLine(zzasdfcollision.PaymentData[0].Address) // bitcoincash:qqrrclrtx62ce3dhkqd5hp94djawfwclus6v0dxy5p
```
