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
            _logger.LogInformation(req.ToString());
        }
    }
}
