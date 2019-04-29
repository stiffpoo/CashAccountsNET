using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace CashAccountsNET.Client
{
    public class AccountClient
    {
        public Uri ApiServer { get; set; }
        private RestClient RestClient { get; set; }

        public static readonly string[] API_SERVERS =
            { "http://api.cashaccount.info:8585/", "https://calus.stiffp.ooo/api/", "https://cashacct.imaginary.cash/" };

        public AccountClient(Uri apiServer = null)
        {
            if (apiServer == null)
            {
                Random random = new Random();
                int r = random.Next(0, API_SERVERS.Length - 1);
                this.ApiServer = new Uri(API_SERVERS[r]);
            }
            else
                this.ApiServer = apiServer;
            this.RestClient = new RestClient(this.ApiServer);
        }

        public LookupResponse[] GetLookupResponses(string name, int number)
        {
            var request = new RestRequest("lookup/{number}/{name}", Method.GET);
            request.AddUrlSegment("name", name);
            request.AddUrlSegment("number", number);

            var response = this.RestClient.Execute(request);
            return ParseLookupJSON(response.Content);
        }

        public async Task<LookupResponse[]> GetLookupResponsesAsync(string name, int number)
        {
            var request = new RestRequest("lookup/{number}/{name}", Method.GET);
            request.AddUrlSegment("name", name);
            request.AddUrlSegment("number", number);

            var response = await this.RestClient.ExecuteTaskAsync(request);
            return ParseLookupJSON(response.Content);
        }

        public AccountMetadata GetAccountMetadata(string name, int number, string hash = "")
        {
            var request = new RestRequest("account/{number}/{name}/{hash}", Method.GET);
            request.AddUrlSegment("name", name);
            request.AddUrlSegment("number", number);
            request.AddUrlSegment("hash", hash);

            var response = this.RestClient.Execute<AccountMetadata>(request);
            return response.Data;
        }

        public async Task<AccountMetadata> GetAccountMetadataAsync(string name, int number, string hash = "")
        {
            var request = new RestRequest("account/{number}/{name}/{hash}", Method.GET);
            request.AddUrlSegment("name", name);
            request.AddUrlSegment("number", number);
            request.AddUrlSegment("hash", hash);

            var response = await this.RestClient.ExecuteTaskAsync<AccountMetadata>(request);
            return response.Data;
        }

        public RegistrationResponse PostAccountRegistration(AccountRegistration registration)
        {
            if (!registration.PaymentData.Any())
                throw new ArgumentException("Account Registration is not complete, no payment data present", "registration");
            else if (string.IsNullOrEmpty(registration.Name))
                throw new ArgumentException("Account Registration is not complete, no account name present", "registration");

            var request = new RestRequest("register/", Method.POST);
            request.AddJsonBody(registration.ToJson());

            var response = this.RestClient.Execute(request);
            var jResponse = JObject.Parse(response.Content);

            if (response.IsSuccessful)
            {
                var registrationResponse = new RegistrationResponse()
                {
                    Txid = (string)jResponse["txid"],
                    RawTxHex = (string)jResponse["hex"]
                };
                return registrationResponse;
            }
            else
            {
                var errorMessage = (string)jResponse["error"];
                throw new Exception(errorMessage);
            }
        }

        public async Task<RegistrationResponse> PostAccountRegistrationAsync(AccountRegistration registration)
        {
            if (!registration.PaymentData.Any())
                throw new ArgumentException("Account Registration is not complete, no payment data present", "registration");
            else if (string.IsNullOrEmpty(registration.Name))
                throw new ArgumentException("Account Registration is not complete, no account name present", "registration");

            var request = new RestRequest("register/", Method.POST);
            request.AddJsonBody(registration.ToJson());

            var response = await this.RestClient.ExecuteTaskAsync(request);
            var jResponse = JObject.Parse(response.Content);

            if (response.IsSuccessful)
            {
                var registrationResponse = new RegistrationResponse()
                {
                    Txid = (string)jResponse["txid"],
                    RawTxHex = (string)jResponse["hex"]
                };
                return registrationResponse;
            }
            else
            {
                var errorMessage = (string)jResponse["error"];
                throw new Exception(errorMessage);
            }
        }

        private LookupResponse[] ParseLookupJSON(string json)
        {
            JObject jLookupResponse = JObject.Parse(json);
            JArray jLookupResponses = (JArray)jLookupResponse["results"];
            var lookupResponses = new LookupResponse[jLookupResponses.Count];
            for (int i = 0; i < lookupResponses.Length; i++)
            {
                lookupResponses[i] = new LookupResponse
                {
                    RawTransactionString = (string)jLookupResponses[i]["transaction"],
                    InclusionProofString = (string)jLookupResponses[i]["inclusion_proof"]
                };
            }
            return lookupResponses;
        }
    }
}
