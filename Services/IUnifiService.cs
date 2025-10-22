using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private const string CacheKey = "UnifiIpAddressList";
        private readonly TimeSpan _cacheDuration;

        public UnifiService(HttpClient httpClient, IConfiguration configuration, ILogger<UnifiService> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;

            // Try to get the API key from configuration with more detailed error handling
            _apiKey = configuration["UNIFI_API_TOKEN"] ?? throw new InvalidOperationException("UNIFI_API_TOKEN not configured");

            // Get cache duration from configuration, default to 5 minutes
            var cacheDurationMinutes = configuration.GetValue<int?>("UNIFI_CACHE_DURATION_MINUTES") ?? 5;
            _cacheDuration = TimeSpan.FromMinutes(cacheDurationMinutes);

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
                // Try to get the cached IP address list
                if (!_cache.TryGetValue(CacheKey, out List<string>? cachedIpAddresses))
                {
                    _logger.LogInformation("Cache miss - fetching IP addresses from Unifi API");
                    
                    // Fetch from API
                    cachedIpAddresses = await FetchIpAddressesFromApi();
                    
                    if (cachedIpAddresses != null && cachedIpAddresses.Count > 0)
                    {
                        // Store in cache
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(_cacheDuration);
                        
                        _cache.Set(CacheKey, cachedIpAddresses, cacheEntryOptions);
                        _logger.LogInformation("Cached {Count} IP addresses for {Duration} minutes", 
                            cachedIpAddresses.Count, _cacheDuration.TotalMinutes);
                    }
                }
                else
                {
                    _logger.LogInformation("Cache hit - using cached IP addresses ({Count} addresses)", 
                        cachedIpAddresses?.Count ?? 0);
                }

                if (cachedIpAddresses == null || cachedIpAddresses.Count == 0)
                {
                    _logger.LogWarning("No IP addresses available for authorization check");
                    return false;
                }

                bool isAuthorized = cachedIpAddresses.Contains(ipAddress);
                return isAuthorized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking IP address authorization");
                return false;
            }
        }

        private async Task<List<string>> FetchIpAddressesFromApi()
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
                return new List<string>();
            }

            // Extract and log the list of IP addresses returned from the Unifi API
            var ipAddresses = unifiResponse.Data.Select(host => host.IpAddress).ToList();
            _logger.LogInformation("Unifi API returned {Count} IP addresses: [{IpAddresses}]",
                ipAddresses.Count,
                string.Join(", ", ipAddresses));

            return ipAddresses;
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