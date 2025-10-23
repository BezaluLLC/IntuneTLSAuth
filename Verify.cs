using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntuneTLSDotNet.Services;
using System.Threading;

namespace IntuneTLSDotNet
{
    public class Verify(IUnifiService _unifiService)
    {
        [Function("Verify")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext executionContext)
        {
            var _logger = executionContext.GetLogger("Verify");
            // Try to get the best client IP from available sources
            string ipAddress = req.Headers["CLIENT-IP"];

            // Ensure no ports are present
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
