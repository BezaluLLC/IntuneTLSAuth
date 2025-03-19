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
            // Convert collections to dictionaries with explicit types for deeper properties
            Dictionary<string, string> headers = req.Headers.ToDictionary(
                h => h.Key,
                h => string.Join(", ", h.Value.ToArray()));

            Dictionary<string, string> queryParams = req.Query.ToDictionary(
                q => q.Key,
                q => string.Join(", ", q.Value.ToArray()));

            Dictionary<string, string> cookies = req.Cookies.ToDictionary(
                c => c.Key,
                c => c.Value);

            Dictionary<string, string?> routeValues = req.RouteValues?.ToDictionary(
                r => r.Key,
                r => r.Value?.ToString()) ?? new();

            // Connection info
            string remoteIp = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            int remotePort = req.HttpContext.Connection.RemotePort;
            string localIp = req.HttpContext.Connection.LocalIpAddress?.ToString() ?? "";
            int localPort = req.HttpContext.Connection.LocalPort;
            bool clientCertAvailable = req.HttpContext.Connection.ClientCertificate != null;

            // Log all information in a single log entry with named properties
            _logger.LogInformation(
                "HTTP Request: Method={Method}, Protocol={Protocol}, Scheme={Scheme}, " +
                "Host={Host}, Path={Path}, PathBase={PathBase}, QueryString={QueryString}, " +
                "ContentType={ContentType}, ContentLength={ContentLength}, HasFormContentType={HasFormContentType}, " +
                "RemoteIpAddress={RemoteIpAddress}, RemotePort={RemotePort}, " +
                "LocalIpAddress={LocalIpAddress}, LocalPort={LocalPort}, ClientCertAvailable={ClientCertAvailable}, " +
                "TraceIdentifier={TraceIdentifier}, Headers={@Headers}, QueryParams={@QueryParams}, " +
                "Cookies={@Cookies}, RouteValues={@RouteValues}",
                req.Method,
                req.Protocol,
                $"{req.Scheme} (IsHttps: {req.IsHttps})",
                req.Host.ToString(),
                req.Path.ToString(),
                req.PathBase.ToString(),
                req.QueryString.ToString(),
                req.ContentType,
                req.ContentLength,
                req.HasFormContentType,
                remoteIp,
                remotePort,
                localIp,
                localPort,
                clientCertAvailable,
                req.HttpContext.TraceIdentifier,
                headers,
                queryParams,
                cookies,
                routeValues);
        }
    }
}
