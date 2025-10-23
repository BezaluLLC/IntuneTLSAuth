using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntuneTLSDotNet.Services;

namespace IntuneTLSDotNet
{
    public class Verify(IUnifiService unifiService)
    {
        [Function("Verify")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext executionContext)
        {
            const string testIp = "1.1.1.1";
            var logger = executionContext.GetLogger("Verify");
            // Try to get the best client IP from available sources
            var ipAddress = !string.IsNullOrEmpty(req.Headers["CLIENT-IP"])
                ? req.Headers["CLIENT-IP"].ToString()
                : testIp;

            // Remove any newline characters from ipAddress before logging (prevents log injection)
            ipAddress = ipAddress.Replace("\r", "").Replace("\n", "");

            // Log output for testing fallback IP
            if (ipAddress == testIp)
                logger.LogWarning("Testing IP is in use. This is likely being run in Local Dev. If not, abort immediately.");

            // Ensure no ports are present
            if (ipAddress.Contains(':'))
            {
                ipAddress = ipAddress.Split(':')[0].Trim();
            }

            logger.LogInformation("Using IP for authorization: {IpAddress}", ipAddress);

            // Check if the IP is authorized
            var isAuthorized = await unifiService.IsIpAddressAuthorized(ipAddress);

            if (isAuthorized)
            {
                logger.LogInformation("IP {IpAddress} is authorized", ipAddress);
                return new OkObjectResult($"Authorization successful for {ipAddress}");
            }
            else
            {
                logger.LogWarning("IP {IpAddress} is not authorized", ipAddress);
                return new StatusCodeResult(403);
            }
        }
    }
}
