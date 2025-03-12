# Technical Context

## Technologies Used
- **Azure Functions**: For serverless execution of the `verify` function.
- **Python**: The primary programming language for the project.
- **Unifi API**: Used to fetch trusted IPs.

## Development Setup
1. Ensure Python and Azure Functions Core Tools are installed.
2. Set the `UNIFI_API_TOKEN` environment variable with a valid API token.
3. Use `local.settings.json` for local development configuration.

## Technical Constraints
- The Unifi API requires a valid API token for authentication.
- The system relies on the `/ea/hosts` endpoint to fetch trusted IPs.