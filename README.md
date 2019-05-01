[![License][License-Image]][License-URL]
# CashAccountsNET
C#/.NET implementation for (Bitcoin) Cash Accounts @ https://cashaccount.info 
[[Specification]](https://gitlab.com/cash-accounts/specification/blob/master/SPECIFICATION.md)

#### Projects within this repository:
##### CashAccounts
A .NET Core class library, housing all the underlying logic for any .NET Core app
to interface with the CashAccounts protocol on Bitcoin Cash.
###### Dependencies
- [BitcoinNET](https://github.com/Lokad/BitcoinNET) (Bitcoin Cash fork of [NBitcoin](https://github.com/MetacoSA/NBitcoin))

##### CashAccounts.Client
Another .NET Core class library, focussed on client interactions with Cash Accounts lookup servers.

Providing:
- retrieval of registration transactions and SPV inclusion proofs served by lookup servers.
(recommended when attempting to retrieve payment information for an account)
- retrieval of information pertaining to an account at a glance 
(not recommended for retrieving payment information)
- submission of a new Cash Account registration to lookup servers that provide it
###### Dependencies
- [CashAccounts](https://github.com/stiffpoo/CashAccountsNET/tree/master/CashAccounts)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (High-performance JSON library)
