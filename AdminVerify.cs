using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using IntuneTLSDotNet.Services;

namespace IntuneTLSDotNet
{
 // Admin-only function endpoints secured by function key.
 public class AdminVerify(IUnifiService unifiService)
 {
 [Function("AdminstuffAddIp")] // POST with body raw IP string
 public async Task<IActionResult> AddIp([
 HttpTrigger(AuthorizationLevel.Function, "post", Route = "ip")] HttpRequest req,
 FunctionContext ctx)
 {
 var logger = ctx.GetLogger("AdminstuffAddIp");
 var body = await new StreamReader(req.Body).ReadToEndAsync();
 if (string.IsNullOrWhiteSpace(body)) return new BadRequestObjectResult("Body must contain IP");
 var ip = body.Trim();
 var success = await unifiService.AppendManualIpAsync(ip);
 return success ? new OkObjectResult($"Added {ip}") : new BadRequestObjectResult($"Invalid or duplicate {ip}");
 }

 [Function("AdminstuffListIps")] // GET returns JSON array
 public async Task<IActionResult> ListIps([
 HttpTrigger(AuthorizationLevel.Function, "get", Route = "ips")] HttpRequest req,
 FunctionContext ctx)
 {
 var logger = ctx.GetLogger("AdminstuffListIps");
 var list = await unifiService.GetAuthorizedIpListAsync();
 return new OkObjectResult(list);
 }

 [Function("AdminstuffRefreshIps")] // POST triggers a forced refresh from Unifi API then returns list
 public async Task<IActionResult> RefreshIps([
 HttpTrigger(AuthorizationLevel.Function, "post", Route = "ips/refresh")] HttpRequest req,
 FunctionContext ctx)
 {
 var logger = ctx.GetLogger("AdminstuffRefreshIps");
 if (unifiService is UnifiService concrete)
 {
 var list = await concrete.RefreshAuthorizedIpCacheAsync();
 logger.LogInformation("Cache refresh complete. {Count} IPs now in list.", list.Count);
 return new OkObjectResult(list);
 }
 logger.LogError("Unable to refresh IP cache: service instance is not concrete UnifiService");
 return new StatusCodeResult(500);
 }
 }
}
