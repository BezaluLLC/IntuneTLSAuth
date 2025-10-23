using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;

namespace IntuneTLSDotNet.Services
{
    public interface IUnifiService
    {
        Task<bool> IsIpAddressAuthorized(string ipAddress);
    }

    // Converted to primary constructor (C#13 / .NET9)
    public class UnifiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<UnifiService> logger,
        IDistributedCache cache) : IUnifiService
    {
        private readonly string _apiKey = configuration["UNIFI_API_TOKEN"] ?? throw new InvalidOperationException("UNIFI_API_TOKEN not configured");
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly TimeSpan _cacheDuration =
            TimeSpan.FromMinutes(configuration.GetValue<int?>("UNIFI_CACHE_DURATION_MINUTES") ?? 5);
        private const string CacheKey = "UnifiIpAddressList";

        public async Task<bool> IsIpAddressAuthorized(string ipAddress)
        {
            try
            {
                // Try to get the cached IP address list from distributed cache
                var cachedData = await cache.GetStringAsync(CacheKey);
                List<string>? cachedIpAddresses;

                if (cachedData == null)
                {
                    logger.LogInformation("Cache miss - fetching IP addresses from Unifi API");

                    // Fetch from API
                    cachedIpAddresses = await FetchIpAddressesFromApi();

                    if (cachedIpAddresses.Count > 0)
                    {
                        // Serialize and store in distributed cache
                        var serializedData = JsonSerializer.Serialize(cachedIpAddresses, _jsonSerializerOptions);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _cacheDuration
                        };

                        await cache.SetStringAsync(CacheKey, serializedData, cacheOptions);
                        logger.LogInformation("Cached {Count} IP addresses for {Duration} minutes",
                            cachedIpAddresses.Count, _cacheDuration.TotalMinutes);
                    }
                }
                else
                {
                    // Deserialize cached data
                    cachedIpAddresses = JsonSerializer.Deserialize<List<string>>(cachedData, _jsonSerializerOptions);
                    logger.LogInformation("Cache hit - using cached IP addresses ({Count} addresses)",
                        cachedIpAddresses?.Count ?? 0);
                }

                if (cachedIpAddresses == null || cachedIpAddresses.Count == 0)
                {
                    logger.LogWarning("No IP addresses available for authorization check");
                    return false;
                }

                var isAuthorized = cachedIpAddresses.Contains(ipAddress);
                return isAuthorized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking IP address authorization");
                return false;
            }
        }

        private async Task<List<string>> FetchIpAddressesFromApi()
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

            logger.LogInformation("Sending request to Unifi API");
            var response = await httpClient.GetAsync("https://api.ui.com/ea/hosts");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // Log the raw response to help with debugging
            logger.LogTrace("Raw Unifi API response: {RawResponse}", content);

            var unifiResponse = JsonSerializer.Deserialize<UnifiResponse>(content, _jsonSerializerOptions);

            if (unifiResponse?.Data == null)
            {
                logger.LogWarning("No data returned from Unifi API");
                return [];
            }

            // Prefer reportedState.wans[].ipv4 values, fallback to ipAddress if none present
            var wanIpAddresses = unifiResponse.Data
                .SelectMany(host => host.ReportedState?.Wans?
                    .Select(w => w.Ipv4)
                    .Where(ip => !string.IsNullOrWhiteSpace(ip))
                    ?? [])
                .Distinct()
                .ToList();

            var fallbackIpAddresses = unifiResponse.Data
                .Select(host => host.IpAddress)
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .Distinct()
                .ToList();

            var finalIpList = wanIpAddresses.Count != 0 ? wanIpAddresses : fallbackIpAddresses;

            logger.LogInformation("Unifi API returned {WanCount} WAN IPs and {FallbackCount} fallback IPs. Using {FinalCount} IPs: [{IpAddresses}]",
                wanIpAddresses.Count,
                fallbackIpAddresses.Count,
                finalIpList.Count,
                string.Join(", ", finalIpList));

            return finalIpList;
        }
    }

    public class UnifiResponse
    {
        [JsonPropertyName("data")]
        public List<UnifiHost> Data { get; } = [];
    }

    public class UnifiHost
    {
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;

        [JsonPropertyName("reportedState")]
        public ReportedState? ReportedState { get; set; }
    }

    public class ReportedState
    {
        [JsonPropertyName("wans")]
        public List<Wan> Wans { get; } = [];
    }

    public class Wan
    {
        [JsonPropertyName("ipv4")]
        public string Ipv4 { get; set; } = string.Empty;
    }
}