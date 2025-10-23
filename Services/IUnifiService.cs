using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IntuneTLSDotNet.Services
{
    public interface IUnifiService
    {
        Task<bool> IsIpAddressAuthorized(string ipAddress);
        Task<IReadOnlyList<string>> GetAuthorizedIpListAsync();
        Task<bool> AppendManualIpAsync(string ipAddress);
    }

    public class UnifiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<UnifiService> logger,
        IDistributedCache cache) : IUnifiService
    {
        private readonly string _apiKey = configuration["UNIFI_API_TOKEN"] ?? throw new InvalidOperationException("UNIFI_API_TOKEN not configured");
        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(configuration.GetValue<int?>("UNIFI_CACHE_DURATION_MINUTES") ?? 5);
        private const string CacheKey = "UnifiIpAddressList";
        private const string ManualCacheKey = "UnifiManualIpAddressList";

        public async Task<bool> IsIpAddressAuthorized(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            var list = await GetAuthorizedIpListAsync();
            return list.Contains(ipAddress.Trim());
        }

        public async Task<IReadOnlyList<string>> GetAuthorizedIpListAsync()
        {
            var apiIps = await GetOrFetchApiIpsAsync();
            var manualData = await cache.GetStringAsync(ManualCacheKey);
            var manualIps = manualData != null ? JsonSerializer.Deserialize<List<string>>(manualData, _json) ?? [] : [];

            return apiIps
                .Concat(manualIps)
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(ip => ip)
                .ToList();
        }

        public async Task<bool> AppendManualIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            ipAddress = ipAddress.Trim();
            if (!IPAddress.TryParse(ipAddress, out _)) return false;

            var manualData = await cache.GetStringAsync(ManualCacheKey);
            var manualIps = manualData != null ? JsonSerializer.Deserialize<List<string>>(manualData, _json) ?? [] : [];
            if (manualIps.Contains(ipAddress)) return true; // already present
            manualIps.Add(ipAddress);

            var serialized = JsonSerializer.Serialize(manualIps, _json);
            await cache.SetStringAsync(ManualCacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            });
            var sanitizedIp = ipAddress.Replace("\r", "").Replace("\n", "");
            logger.LogInformation("Manual IP added {Ip}. Manual list count={Count}", sanitizedIp, manualIps.Count);
            return true;
        }

        public async Task<IReadOnlyList<string>> RefreshAuthorizedIpCacheAsync()
        {
            logger.LogInformation("Refreshing Unifi IP cache");
            var apiIps = await FetchIpAddressesFromApi();
            var serialized = JsonSerializer.Serialize(apiIps, _json);
            await cache.SetStringAsync(CacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            });
            logger.LogInformation("Cache repopulated with {Count} IPs", apiIps.Count);
            return await GetAuthorizedIpListAsync();
        }

        private async Task<List<string>> GetOrFetchApiIpsAsync()
        {
            var cachedData = await cache.GetStringAsync(CacheKey);
            if (cachedData != null)
            {
                var list = JsonSerializer.Deserialize<List<string>>(cachedData, _json) ?? [];
                if (list.Count > 0) return list;
            }

            logger.LogInformation("Cache miss -> fetching from API");
            var apiIps = await FetchIpAddressesFromApi();
            if (apiIps.Count > 0)
            {
                var serialized = JsonSerializer.Serialize(apiIps, _json);
                await cache.SetStringAsync(CacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                });
                logger.LogInformation("Cached {Count} IPs for {Minutes} minutes", apiIps.Count, _cacheDuration.TotalMinutes);
            }
            return apiIps;
        }

        private async Task<List<string>> FetchIpAddressesFromApi()
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

            var response = await httpClient.GetAsync("https://api.ui.com/ea/hosts");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var root = JsonSerializer.Deserialize<UnifiResponse>(content, _json);
            if (root?.Data == null)
            {
                logger.LogWarning("Unifi API returned no data");
                return [];
            }

            logger.LogInformation("Parsed {Count} hosts", root.Data.Count);
            var publicIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rawIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var host in root.Data)
            {
                Collect(host.IpAddress);
                var rs = host.ReportedState;
                if (rs != null)
                {
                    Collect(rs.Ip);
                    if (rs.Wans?.Count > 0) foreach (var w in rs.Wans) Collect(w.Ipv4);
                }
            }
            void Collect(string? ip)
            {
                if (string.IsNullOrWhiteSpace(ip)) return;
                ip = ip.Trim();
                if (!ip.Contains('.')) return; // skip IPv6
                rawIps.Add(ip);
                if (IsPublicIpv4(ip)) publicIps.Add(ip);
            }
            var final = publicIps.Count > 0 ? publicIps : rawIps;
            logger.LogInformation("Collected {Public} public IPv4s (raw={Raw}) using {Final}", publicIps.Count, rawIps.Count, final.Count);
            logger.LogDebug("Sample: {Sample}", string.Join(", ", final.Take(15)));
            return final.OrderBy(ip => ip).ToList();
        }

        private static bool IsPublicIpv4(string ip)
        {
            if (!IPAddress.TryParse(ip, out var parsed)) return false;
            if (parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
            var b = parsed.GetAddressBytes();
            if (b[0] == 10) return false;
            if (b[0] == 192 && b[1] == 168) return false;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;
            if (b[0] == 127) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            if (b[0] >= 224) return false;
            return true;
        }
    }

    public class UnifiResponse
    {
        [JsonPropertyName("data")] public List<UnifiHost> Data { get; set; } = [];
    }

    public class UnifiHost
    {
        [JsonPropertyName("ipAddress")] public string IpAddress { get; set; } = string.Empty;
        [JsonPropertyName("reportedState")] public ReportedState? ReportedState { get; set; }
    }

    public class ReportedState
    {
        [JsonPropertyName("wans")] public List<Wan> Wans { get; set; } = [];
        [JsonPropertyName("ip")] public string? Ip { get; set; }
    }

    public class Wan
    {
        [JsonPropertyName("ipv4")] public string Ipv4 { get; set; } = string.Empty;
    }
}