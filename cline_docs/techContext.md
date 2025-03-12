# Technical Context

## Technologies used
1. Azure Functions for serverless execution.
2. Python 3.11 for implementing the function logic.
3. `azure.functions` library for HTTP triggers and responses.

## Development setup
1. Install Azure Functions Core Tools for local development.
2. Use Python 3.11 environment managed by `pyenv`.
3. Define function routes and logic in `function_app.py`.
4. Use `local.settings.json` for local configuration.

## Technical constraints
1. The `HttpRequest` object in Azure Functions does not include `remote_addr`.
2. The system relies on the `X-Forwarded-For` header for IP extraction.
3. Debugging and logging are critical for monitoring and troubleshooting.