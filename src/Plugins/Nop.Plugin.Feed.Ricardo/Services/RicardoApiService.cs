using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Microsoft.Extensions.Logging;

namespace Nop.Plugin.Feed.Ricardo.Services
{
    public class RicardoApiService
    {
        private readonly RicardoSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RicardoApiService> _logger;

        public RicardoApiService(RicardoSettings settings, 
            IHttpClientFactory httpClientFactory,
            ILogger<RicardoApiService> logger)
        {
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private string BaseUrl => _settings.UseSandbox 
            ? "https://api.sandbox.ricardo.ch" 
            : "https://api.ricardo.ch";

        public async Task<string> GetAccessTokenAsync()
        {
            // Simplified OAuth flow implementation
            // In production, you would cache this token based on expiration
            
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.ricardo.ch/oauth/token"); // Example URL
            
            // Basic Auth header usually required or body params
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            // For now, assume we get a token or return a dummy one if credentials are empty to avoid crash
            if (string.IsNullOrEmpty(_settings.ClientId)) return string.Empty;

            try 
            {
                // Un-comment to enable real call when credentials are set
                // var response = await client.SendAsync(request);
                // response.EnsureSuccessStatusCode();
                // var content = await response.Content.ReadAsStringAsync();
                // return JsonConvert.DeserializeObject<dynamic>(content).access_token;
                
                return "dummy_token_" + Guid.NewGuid(); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ricardo: Failed to get access token");
                return string.Empty;
            }
        }

        public async Task<bool> CreateArticleAsync(Product product)
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning($"Ricardo: Skipped syncing product {product.Name} (No Token)");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Mapping Nop Product to Ricardo Article JSON structure
            var ricardoArticle = new
            {
                title = product.Name,
                description = product.ShortDescription ?? product.Name,
                price = product.Price,
                currency = "CHF",
                quantity = product.StockQuantity,
                condition = "new", // Simplified
                categoryId = 12345, // Ideally, you need a mapping table Nop Category -> Ricardo Category
                listingType = "buy_now",
                startDate = DateTime.UtcNow.ToString("O"),
                endDate = DateTime.UtcNow.AddDays(10).ToString("O")
            };

            var json = JsonConvert.SerializeObject(ricardoArticle);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Fake endpoint for now
                // var response = await client.PostAsync($"{BaseUrl}/articles", content);
                // return response.IsSuccessStatusCode;
                
                _logger.LogInformation($"Ricardo: Successfully synced product {product.Name} (Simulated). Payload: {json}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ricardo: Error syncing product {product.Name}");
                return false;
            }
        }
    }
}
