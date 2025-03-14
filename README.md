# Intune TLS Authentication Endpoint

## Overview
This project is an Azure Function application designed to verify if a requester's public IP is trusted. It interacts with the Unifi Site Manager API to fetch trusted IPs and performs verification.

## Logging Implementation
The project uses the Azure OpenTelemetry SDK for logging to Azure Application Insights, ensuring efficient and consistent logging without duplicate entries.

### Why OpenTelemetry for Azure Functions?
OpenTelemetry provides a standardized way to collect and export telemetry data, including logs, metrics, and traces. For Azure Functions, it integrates seamlessly with Azure Application Insights, enabling robust observability.

## Setup Instructions

### Prerequisites
- Python 3.8 or higher
- Azure Application Insights account
- Unifi API token (set as an environment variable `UNIFI_API_TOKEN`)

### Dependencies
Add the following dependencies to your `requirements.txt` file:
```
opentelemetry-api
opentelemetry-sdk
opentelemetry-exporter-azuremonitor
```

### Installation
Run the following command to install the dependencies:
```bash
pip install -r requirements.txt
```

### Configuration for Azure Functions
1. Import OpenTelemetry modules in your Azure Function files:
   ```python
   from opentelemetry import trace
   from opentelemetry.sdk.trace import TracerProvider
   from opentelemetry.sdk.trace.export import BatchSpanProcessor
   from opentelemetry.exporter.azuremonitor import AzureMonitorTraceExporter
   from opentelemetry.sdk.resources import Resource
   from opentelemetry.sdk.logs import LoggingHandler
   from opentelemetry.exporter.azuremonitor import AzureMonitorLogExporter
   import logging
   ```

2. Set up the OpenTelemetry SDK in your `function_app.py`:
   ```python
   # Configure OpenTelemetry for Azure Functions
   resource = Resource.create({"service.name": "IntuneTLSAuthEndpoint"})
   trace_provider = TracerProvider(resource=resource)
   trace.set_tracer_provider(trace_provider)

   # Configure Azure Monitor Exporter
   trace_exporter = AzureMonitorTraceExporter(connection_string="InstrumentationKey=<Your_Instrumentation_Key>")
   span_processor = BatchSpanProcessor(trace_exporter)
   trace_provider.add_span_processor(span_processor)

   # Configure Logging for Azure Functions
   log_exporter = AzureMonitorLogExporter(connection_string="InstrumentationKey=<Your_Instrumentation_Key>")
   logging_handler = LoggingHandler(log_exporter)
   logging.basicConfig(level=logging.INFO, handlers=[logging_handler])
   ```

3. Replace native `logging` calls with OpenTelemetry logging:
   ```python
   logging.info("Your log message")
   logging.error("Your error message")
   ```

### Environment Variables for Azure Functions
Set the following environment variables in your Azure Function App settings:
- `UNIFI_API_TOKEN`: Your Unifi API token.
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Connection string for Azure Application Insights.

### Testing in Azure Functions
1. Deploy the Azure Function to your Azure account.
2. Verify that logs and traces are being sent to Azure Application Insights.
3. Check the Azure Application Insights portal for incoming telemetry data.

## Troubleshooting
- Ensure the `APPLICATIONINSIGHTS_CONNECTION_STRING` is correctly set in the Azure Function App settings.
- Check the Azure Application Insights portal for logs and traces to confirm successful integration.

## License
This project is licensed under the MIT License.