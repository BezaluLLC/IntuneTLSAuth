using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IntuneTLSDotNet.Services
{
    public interface IUnifiService
    {
        Task<bool> IsIpAddressAuthorized(string ipAddress);
    }

    public class UnifiService : IUnifiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UnifiService> _logger;
        private readonly string _apiKey;

        public UnifiService(HttpClient httpClient, IConfiguration configuration, ILogger<UnifiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["UNIFI_API_KEY"] ?? throw new InvalidOperationException("UNIFI_API_KEY not configured");
        }

        public async Task<bool> IsIpAddressAuthorized(string ipAddress)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

                _logger.LogInformation("Sending request to Unifi API");
                var response = await _httpClient.GetAsync("https://api.ui.com/ea/hosts");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                
                // Log the raw response to help with debugging
                _logger.LogDebug("Raw Unifi API response: {RawResponse}", content);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var unifiResponse = JsonSerializer.Deserialize<UnifiResponse>(content, options);

                if (unifiResponse?.Data == null)
                {
                    _logger.LogWarning("No data returned from Unifi API");
                    return false;
                }

                // Log the list of IP addresses returned from the Unifi API
                var ipAddresses = unifiResponse.Data.Select(host => host.IpAddress).ToList();
                _logger.LogInformation("Unifi API returned {Count} IP addresses: [{IpAddresses}]", 
                    ipAddresses.Count, 
                    string.Join(", ", ipAddresses));

                bool isAuthorized = unifiResponse.Data.Any(host => host.IpAddress == ipAddress);
                _logger.LogInformation("IP address {IpAddress} authorization check result: {IsAuthorized}", 
                    ipAddress, 
                    isAuthorized);
                
                return isAuthorized;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Unifi API response for IP address {IpAddress}", ipAddress);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Unifi API for IP address {IpAddress}", ipAddress);
                return false;
            }
        }
    }

    public class UnifiResponse
    {
        [JsonPropertyName("data")]
        public List<UnifiHost> Data { get; set; } = new();
    }

    public class UnifiHost
    {
        // Try the correct property name for IP address from Unifi API
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;
    }
}