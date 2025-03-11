# Active Context

- What you're working on now: Scaffolding and debugging an Azure Functions project for network verification.
- Recent changes:
  - Created the `verify` HTTP trigger in `function_app.py`.
  - Implemented `UnifiService` in `unifi_service.py` for interacting with the Unifi API.
  - Updated `requirements.txt` to include the `requests` library.
  - Configured `.vscode/launch.json` for Azure Functions Core Tools debugging.
- Next steps:
  - Ensure Azure Functions Core Tools are properly installed and configured.
  - Test the `verify` endpoint with dynamic IP validation using the Unifi API.