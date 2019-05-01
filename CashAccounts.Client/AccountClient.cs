using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CashAccountsNET.Client.Models;
using Newtonsoft.Json.Linq;

namespace CashAccountsNET.Client
{
    public class AccountClient
    {
        public Uri ApiServer { get; set; }
        public Uri LookupServer { get; set; }
        public HttpClient HttpClient { get; private set; } = new HttpClient();

        public static readonly string[] API_SERVERS =
            { "http://api.cashaccount.info:8585/", "https://cashacct.imaginary.cash/" };

        public static readonly string[] LOOKUP_SERVERS = API_SERVERS.Concat(new string[] 
            { "https://calus.stiffp.ooo/api/", "http://89.163.204.53:8585/" }).ToArray();

        public AccountClient(Uri lookupServer = null, Uri apiServer = null)
        {
            if (apiServer == null && lookupServer == null)
            {
                Random random = new Random();
                int r = random.Next(0, API_SERVERS.Length - 1);
                this.ApiServer = new Uri(API_SERVERS[r]);
                this.LookupServer = this.ApiServer;
            }
            else if (apiServer == null && lookupServer != null)
            {
                Random random = new Random();
                int r = random.Next(0, API_SERVERS.Length - 1);
                this.ApiServer = new Uri(API_SERVERS[r]);
                this.LookupServer = lookupServer;
            }
            else if (apiServer != null && lookupServer == null)
            {
                this.ApiServer = apiServer;
                this.LookupServer = this.ApiServer;
            }
            else
            {
                this.ApiServer = apiServer;
                this.LookupServer = lookupServer;
            }
        }

        public LookupResponse[] AccountLookup(string name, int number)
        {
            var requestUri = new Uri(this.LookupServer + string.Format("lookup/{0}/{1}", number, name));

            var httpResponse = this.HttpClient.GetAsync(requestUri).Result;
            var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;

            if (httpResponse.IsSuccessStatusCode)
            {
                return ParseLookupJson(jsonResponse);
            }
            else
            {
                var errorMessage = ParseErrorMessage(jsonResponse);
                throw new HttpRequestException(errorMessage);
            }
        }

        public async Task<LookupResponse[]> AccountLookupAsync(string name, int number)
        {
            var requestUri = new Uri(this.LookupServer + string.Format("lookup/{0}/{1}", number, name));

            var httpResponse = await this.HttpClient.GetAsync(requestUri);
            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                return ParseLookupJson(jsonResponse);
            }
            else
            {
                var errorMessage = ParseErrorMessage(jsonResponse);
                throw new HttpRequestException(errorMessage);
            }
        }

        public AccountMetadata GetMetadata(string name, int number, string hash = "")
        {
            var requestUri = new Uri(this.ApiServer + string.Format("account/{0}/{1}/{2}", number, name, hash));

            var httpResponse = this.HttpClient.GetAsync(requestUri).Result;
            var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;

            if (httpResponse.IsSuccessStatusCode)
            {
                return ParseMetadataJson(jsonResponse);
            }
            else
            {
                var errorMessage = ParseErrorMessage(jsonResponse);
                throw new HttpRequestException(errorMessage);
            }
        }

        public async Task<AccountMetadata> GetMetadataAsync(string name, int number, string hash = "")
        {
            var requestUri = new Uri(this.ApiServer + string.Format("account/{0}/{1}/{2}", number, name, hash));

            var httpResponse = await this.HttpClient.GetAsync(requestUri);
            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                return ParseMetadataJson(jsonResponse);
            }
            else
            {
                var errorMessage = ParseErrorMessage(jsonResponse);
                throw new HttpRequestException(errorMessage);
            }
        }

        public RegistrationResponse PostRegistration(AccountRegistration registration)
        {
            if (!registration.PaymentData.Any())
                throw new ArgumentException("Account Registration is not complete, no payment data present", "registration");
            else if (string.IsNullOrEmpty(registration.Name))
                throw new ArgumentException("Account Registration is not complete, no account name present", "registration");

            var requestUri = new Uri(this.ApiServer + "register/");

            var jsonRequestString = AccountRegistrationToJson(registration);
            var jsonContent = new StringContent(jsonRequestString, Encoding.UTF8, "application/json");

            var httpResponse = this.HttpClient.PostAsync(requestUri, jsonContent).Result;
            var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;

            if (httpResponse.IsSuccessStatusCode)
            {
                return ParseRegoResponse(jsonResponse);
            }
            else
            {
                var errorMessage = ParseErrorMessage(jsonResponse);
                throw new HttpRequestException(errorMessage);
            }
        }

        public async Task<RegistrationResponse> PostRegistrationAsync(AccountRegistration registration)
        {
            if (!registration.PaymentData.Any())
                throw new ArgumentException("Account Registration is not complete, no payment data present", "registration");
            else if (string.IsNullOrEmpty(registration.Name))
                throw new ArgumentException("Account Registration is not complete, no account name present", "registration");

            var requestUri = new Uri(this.ApiServer + "register/");

            var jsonRequestString = AccountRegistrationToJson(registration);
            var jsonContent = new StringContent(jsonRequestString, Encoding.UTF8, "application/json");

            var httpResponse = await this.HttpClient.PostAsync(requestUri, jsonContent);
            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                return ParseRegoResponse(jsonResponse);
            }
            else
            {
                var errorMessage = ParseErrorMessage(jsonResponse);
                throw new HttpRequestException(errorMessage);
            }
        }

        private string ParseErrorMessage(string json)
        {
            var jError = JObject.Parse(json);
            return (string)jError["error"];
        }

        private LookupResponse[] ParseLookupJson(string json)
        {
            var jLookup = JObject.Parse(json);
            var jLookups = (JArray)jLookup["results"];
            var lookups = new LookupResponse[jLookups.Count];
            for (int i = 0; i < lookups.Length; i++)
            {
                lookups[i] = new LookupResponse
                {
                    RawTxHex = ((string)jLookups[i]["transaction"]).ToLower(),
                    InclusionProof = ((string)jLookups[i]["inclusion_proof"]).ToLower()
                };
            }
            return lookups;
        }

        private AccountMetadata ParseMetadataJson(string json)
        {
            var jMetadata = JObject.Parse(json);
            var jPaymentInformation = (JArray)jMetadata["information"]["payment"];
            var metadata = new AccountMetadata()
            {
                Identifier = (string)jMetadata["identifier"],
                Information = new AccountInformation()
                {
                    Emoji = (string)jMetadata["information"]["emoji"],
                    Name = (string)jMetadata["information"]["name"],
                    Number = (int)jMetadata["information"]["number"],
                    Collision = new CollisionInformation()
                    {
                        Hash = (string)jMetadata["information"]["collision"]["hash"],
                        Count = (int)jMetadata["information"]["collision"]["count"],
                        Length = (int)jMetadata["information"]["collision"]["length"],
                    },
                    Payment = new List<PaymentInformation>()
                }
            };
            for (int i = 0; i < jPaymentInformation.Count; i++)
            {
                var paymentInformation = new PaymentInformation()
                {
                    Type = (string)jPaymentInformation[i]["type"],
                    Address = (string)jPaymentInformation[i]["address"]
                };
                metadata.Information.Payment.Add(paymentInformation);
            }

            return metadata;
        }

        private RegistrationResponse ParseRegoResponse(string json)
        {
            var jResponse = JObject.Parse(json);
            var regoResponse = new RegistrationResponse()
            {
                Txid = (string)jResponse["txid"],
                RawTxHex = (string)jResponse["hex"]
            };
            return regoResponse;
        }

        private string AccountRegistrationToJson(AccountRegistration registration)
        {
            var addresses = new string[registration.PaymentData.Length];
            for (int i = 0; i < addresses.Length; i++)
            {
                addresses[i] = registration.PaymentData.ElementAt(i).Address;
            }

            var jObject = new JObject()
            {
                new JProperty("name", registration.Name),
                new JProperty("payments", new JArray(addresses))
            };

            return jObject.ToString();
        }
    }
}
