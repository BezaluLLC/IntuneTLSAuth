# Active Context

## Current Task
Integrating the Unifi service into the function and refining its implementation.

## Recent Changes
1. Integrated the Unifi service into the `verify` function in `function_app.py`.
2. Updated the `UnifiService.get_trusted_ips` method to:
   - Use the `/ea/hosts` endpoint.
   - Remove private IP evaluation logic.
   - Remove pagination logic.
   - Add debug logging for the raw API response.
3. Updated the `verify` function to:
   - Strip the port from the `requester_ip` for accurate comparison.

## Next Steps
- Test the implementation to ensure it works as expected.
- Address any further feedback or issues that arise during testing.