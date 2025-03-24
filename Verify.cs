using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntuneTLSDotNet.Services;

namespace IntuneTLSDotNet
{
    public class Verify(ILogger<Verify> logger, IUnifiService unifiService)
    {
        private readonly ILogger<Verify> _logger = logger;
        private readonly IUnifiService _unifiService = unifiService;

        [Function("Verify")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            // Try to get the best client IP from available sources
            string ipAddress = req.Headers["CLIENT-IP"];

            // X-Forwarded-For can contain multiple IPs - we want the first one (client's original IP)
            if (ipAddress.Contains(':'))
            {
                ipAddress = ipAddress.Split(':')[0].Trim();
            }

            _logger.LogInformation($"Using IP for authorization: {ipAddress}");

            // Check if the IP is authorized
            bool isAuthorized = await _unifiService.IsIpAddressAuthorized(ipAddress);

            if (isAuthorized)
            {
                _logger.LogInformation($"IP {ipAddress} is authorized");
                return new OkObjectResult($"Authorization successful for {ipAddress}");
            }
            else
            {
                _logger.LogWarning($"IP {ipAddress} is not authorized");
                return new StatusCodeResult(403);
            }
        }
    }
}
