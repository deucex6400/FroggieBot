using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroggieBot
{
    public class EtherscanService : IEtherscanService, IDisposable
    {
        const string _baseUrl = "https://api.etherscan.io";
        readonly RestClient _client;

        public EtherscanService()
        {
            _client = new RestClient(_baseUrl);
        }

        public async Task<EtherscanGas> GetGas(string apiKey)
        {
            var request = new RestRequest($"/api?module=gastracker&action=gasoracle&apikey={apiKey}");
            try
            {
                var response = await _client.GetAsync(request);
                var data = JsonConvert.DeserializeObject<EtherscanGas>(response.Content!);
                return data;
            }
            catch (HttpRequestException httpException)
            {
                Console.WriteLine($"Error getting gas: {httpException.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
