using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IntuneTLSDotNet.Services;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register HttpClient and Unifi service
builder.Services
    .AddHttpClient()
    .AddSingleton<IUnifiService, UnifiService>()
    .AddApplicationInsightsTelemetryWorkerService()
    .AddOpenTelemetry()
    .UseAzureMonitor();

builder.Build().Run();