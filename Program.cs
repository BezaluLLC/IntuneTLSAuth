using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using IntuneTLSDotNet.Services;
using Azure.Monitor.OpenTelemetry.AspNetCore;

// Create the function app builder
var builder = FunctionsApplication.CreateBuilder(args);

// Ensure configuration is properly loaded from all sources
builder.Configuration.AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

// Register HttpClient and Unifi service
builder.Services
    .AddHttpClient()
    .AddSingleton<IConfiguration>(builder.Configuration) // Explicitly register IConfiguration
    .AddSingleton<IUnifiService, UnifiService>()
    .AddOpenTelemetry()
    .UseAzureMonitor();

builder.Build().Run();