using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using IntuneTLSDotNet.Services;
using Azure.Identity;
using StackExchange.Redis;
using Microsoft.Azure.StackExchangeRedis;

// Create the function app builder
var builder = FunctionsApplication.CreateBuilder(args);

// Ensure configuration is properly loaded from all sources
builder.Configuration.AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

// Configure Azure Redis with Managed Identity
var redisConnectionString = builder.Configuration["REDIS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
    var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
    
    // Use Azure Managed Identity for authentication
    await configurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());
    
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.ConfigurationOptions = configurationOptions;
    });
}
else
{
    throw new InvalidOperationException("REDIS_CONNECTION_STRING is required for distributed caching");
}

// Register HttpClient and Unifi service with simplified logging
builder.Services
    .AddHttpClient()
    .AddSingleton<IConfiguration>(builder.Configuration)
    .AddSingleton<IUnifiService, UnifiService>()
    .AddApplicationInsightsTelemetryWorkerService(); // Traditional App Insights integration

// Remove OpenTelemetry completely
// builder.Services.AddOpenTelemetry().UseAzureMonitor().UseFunctionsWorkerDefaults();

builder.Build().Run();