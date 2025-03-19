using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntuneTLSDotNet.Services;
using System.Threading.Tasks;

namespace IntuneTLSDotNet
{
    public class Verify(ILogger<Verify> logger, IUnifiService unifiService)
    {
        private readonly ILogger<Verify> _logger = logger;
        private readonly IUnifiService _unifiService = unifiService;

        [Function("Verify")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            // Log all headers for debugging
            LogRequest(req);

            // Get the client's IP address - try multiple sources
            string forwardedIp = req.Headers["X-Forwarded-For"].FirstOrDefault() ?? "null";
            string remoteIp = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "null";
            string realIp = req.Headers["X-Real-IP"].FirstOrDefault() ?? "null";

            _logger.LogInformation($"X-Forwarded-For: {forwardedIp}");
            _logger.LogInformation($"RemoteIpAddress: {remoteIp}");
            _logger.LogInformation($"X-Real-IP: {realIp}");

            // Try to get the best client IP from available sources
            string ipAddress = forwardedIp ?? realIp ?? remoteIp ?? "unknown";

            // X-Forwarded-For can contain multiple IPs - we want the first one (client's original IP)
            if (ipAddress.Contains(','))
            {
                ipAddress = ipAddress.Split(',')[0].Trim();
            }

            _logger.LogInformation($"Using IP for authorization: {ipAddress}");

            // Check if the IP is authorized
            bool isAuthorized = await _unifiService.IsIpAddressAuthorized(ipAddress);

            if (isAuthorized)
            {
                _logger.LogInformation($"IP {ipAddress} is authorized");
                return new OkObjectResult("Authorization successful");
            }
            else
            {
                _logger.LogWarning($"IP {ipAddress} is not authorized");
                return new StatusCodeResult(403);
            }
        }

        private void LogRequest(HttpRequest req)
        {
            // Create a single, structured log entry with all request details
            var requestInfo = new
            {
                // Basic request info
                Method = req.Method,
                Protocol = req.Protocol,
                Scheme = $"{req.Scheme} (IsHttps: {req.IsHttps})",
                Host = req.Host.ToString(),
                Path = req.Path.ToString(),
                PathBase = req.PathBase.ToString(),
                QueryString = req.QueryString.ToString(),

                // Headers (including the ones we're particularly interested in)
                Headers = req.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray())),

                // Query parameters
                QueryParams = req.Query.ToDictionary(q => q.Key, q => string.Join(", ", q.Value.ToArray())),

                // Cookies
                Cookies = req.Cookies.ToDictionary(c => c.Key, c => c.Value),

                // Content details
                ContentType = req.ContentType,
                ContentLength = req.ContentLength,
                HasFormContentType = req.HasFormContentType,

                // Connection info
                Connection = new
                {
                    RemoteIpAddress = req.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    RemotePort = req.HttpContext.Connection.RemotePort,
                    LocalIpAddress = req.HttpContext.Connection.LocalIpAddress?.ToString(),
                    LocalPort = req.HttpContext.Connection.LocalPort,
                    ClientCertAvailable = req.HttpContext.Connection.ClientCertificate != null
                },

                // Route values
                RouteValues = req.RouteValues?.ToDictionary(r => r.Key, r => r.Value?.ToString()),

                // Additional context
                TraceIdentifier = req.HttpContext.TraceIdentifier
            };

            // Log the entire request info as a single structured log entry
            _logger.LogInformation("HTTP Request: {@RequestDetails}", requestInfo);
        }
    }
}
