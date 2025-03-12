# System Patterns

## How the system is built
1. The project is implemented as an Azure Function App using Python.
2. The `function_app.py` file defines the `/verify` endpoint.
3. The system uses the `azure.functions` library for HTTP triggers and responses.

## Key technical decisions
1. The `X-Forwarded-For` header is used to extract the requester's IP address.
2. A fallback value of "Unknown IP" is used if the header is not present.
3. Debug logging is implemented to capture request details, including headers and IP addresses.

## Architecture patterns
1. Serverless architecture using Azure Functions.
2. Stateless HTTP endpoint for IP verification.
3. Logging for observability and debugging.