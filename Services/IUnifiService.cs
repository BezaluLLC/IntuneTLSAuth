using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UnifiService(HttpClient httpClient, IConfiguration configuration, ILogger<UnifiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Try to get the API key from configuration with more detailed error handling
            _apiKey = configuration["UNIFI_API_TOKEN"] ?? throw new InvalidOperationException("UNIFI_API_TOKEN not configured");

            // Initialize JsonSerializerOptions once and reuse it
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
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
                _logger.LogTrace("Raw Unifi API response: {RawResponse}", content);

                var unifiResponse = JsonSerializer.Deserialize<UnifiResponse>(content, _jsonSerializerOptions);

                if (unifiResponse?.Data == null)
                {
                    _logger.LogWarning("No data returned from Unifi API");
                    return false;
                }

                // Log the list of IP addresses returned from the Unifi API
                var ipAddresses = unifiResponse.Data.Select(host => host.IpAddress).ToList();
                _logger.LogDebug("Unifi API returned {Count} IP addresses: [{IpAddresses}]",
                    ipAddresses.Count,
                    string.Join(", ", ipAddresses));

                bool isAuthorized = unifiResponse.Data.Any(host => host.IpAddress == ipAddress);

                return isAuthorized;
            }
            catch (JsonException ex) {
                _logger.LogError(ex, "Error deserializing Unifi API response for IP address {IpAddress}", ipAddress);
                return false;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error calling Unifi API for IP address {IpAddress}", ipAddress);
                return false;
            }
        }
    }

    public class UnifiResponse
    {
        [JsonPropertyName("data")]
        public List<UnifiHost> Data { get; set; } = new List<UnifiHost>();
    }

    public class UnifiHost
    {
        // Try the correct property name for IP address from Unifi API
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;
    }
}