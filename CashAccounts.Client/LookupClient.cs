using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace CashAccountsNET.Client
{
    public class LookupClient
    {
        public Uri LookupServer { get; set; }
        private RestClient RestClient { get; set; }

        public readonly string[] LOOKUP_SERVERS = 
            { "http://api.cashaccount.info:8585/", "https://calus.stiffp.ooo/api/", "http://89.163.204.53:8585/", "https://cashacct.imaginary.cash/", "http://lookup.cashaccounts.bigkesh.com/" };

        public LookupClient(Uri lookupServer = null)
        {
            if (lookupServer == null)
            {
                Random random = new Random();
                int r = random.Next(0, LOOKUP_SERVERS.Length - 1);
                this.LookupServer = new Uri(LOOKUP_SERVERS[r]);
            }
            else
                this.LookupServer = lookupServer;
            this.RestClient = new RestClient(this.LookupServer);
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
