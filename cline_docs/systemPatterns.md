# System Patterns

## Architecture
- The system is built using Azure Functions for serverless execution.
- The `UnifiService` class encapsulates all interactions with the Unifi API.

## Key Technical Decisions
1. The Unifi API is used to fetch trusted IPs via the `/ea/hosts` endpoint.
2. The `verify` function processes HTTP requests to validate the requester's IP against the trusted IPs.
3. Logging is implemented for both debugging (raw API responses) and information (trusted IPs and match results).

## Patterns
- Encapsulation: The `UnifiService` class handles all API interactions, keeping the logic modular and reusable.
- Logging: Debug and information logs are used to provide visibility into the system's behavior.