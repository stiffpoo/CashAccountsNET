using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace CashAccountsNET.Client
{
    public class AccountClient
    {
        public Uri ApiServer { get; set; }
        private RestClient RestClient { get; set; }

        public readonly string[] API_SERVERS = 
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
