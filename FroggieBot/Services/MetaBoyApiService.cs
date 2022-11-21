using FroggieBot.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroggieBot.Services
{
    public class MetaBoyApiService : IDisposable
    {
        const string _baseUrl = "https://metaboy.azurewebsites.net";

        readonly RestClient _client;

        public MetaBoyApiService()
        {
            _client = new RestClient(_baseUrl);
        }

        public async Task<List<Claimable>> GetClaimable()
        {
            var request = new RestRequest("api/nft/claimable");
            try
            {
                var response = await _client.GetAsync(request);
                var data = JsonConvert.DeserializeObject<List<Claimable>>(response.Content!);
                return data;
            }
            catch (HttpRequestException httpException)
            {
                Console.WriteLine($"Error getting claimable from metaboy api: {httpException.Message}");
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException jSex)
            {
                Console.WriteLine($"Error deserialising claimable json: {jSex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting claimable from metaboy api: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetRedeemable(string address, string nftData)
        {
            var request = new RestRequest("api/nft/redeemable");
            request.AddParameter("address", address);
            request.AddParameter("nftData", nftData);
            try
            {
                var response = await _client.GetAsync(request);
                var data = response.Content!;
                return data;
            }
            catch (HttpRequestException httpException)
            {
                Console.WriteLine($"Error getting redeemable from metaboy api: {httpException.Message}");
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException jSex)
            {
                Console.WriteLine($"Error deserialising redeemable json: {jSex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting redeemable from metaboy api: {ex.Message}");
                return null;
            }
        }

        public async Task<string> AddClaim(NftReciever nftReciever)
        {
            var request = new RestRequest("api/nft/claim");
            request.AddJsonBody(nftReciever);
            try
            {
                var response = await _client.PostAsync(request);
                var data = response.Content!;
                return data;
            }
            catch (HttpRequestException httpException)
            {
                Console.WriteLine($"Error adding claim from metaboy api: {httpException.Message}");
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException jSex)
            {
                Console.WriteLine($"Error adding claim json: {jSex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding claim from metaboy api: {ex.Message}");
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
