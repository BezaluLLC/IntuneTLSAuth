using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntuneTLSDotNet.Services;
using System.Threading.Tasks;

namespace IntuneTLSDotNet
{
    public class Verify
    {
        private readonly ILogger<Verify> _logger;
        private readonly IUnifiService _unifiService;

        public Verify(ILogger<Verify> logger, IUnifiService unifiService)
        {
            _logger = logger;
            _unifiService = unifiService;
        }

        [Function("Verify")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Get the client's IP address
            string ipAddress = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation($"Request from IP: {ipAddress}");

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
    }
}
