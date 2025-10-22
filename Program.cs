using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using IntuneTLSDotNet.Services;

// Create the function app builder
var builder = FunctionsApplication.CreateBuilder(args);

// Ensure configuration is properly loaded from all sources
builder.Configuration.AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

// Register HttpClient and Unifi service with simplified logging
builder.Services
    .AddHttpClient()
    .AddMemoryCache()
    .AddSingleton<IConfiguration>(builder.Configuration)
    .AddSingleton<IUnifiService, UnifiService>()
    .AddApplicationInsightsTelemetryWorkerService(); // Traditional App Insights integration

// Remove OpenTelemetry completely
// builder.Services.AddOpenTelemetry().UseAzureMonitor().UseFunctionsWorkerDefaults();

builder.Build().Run();