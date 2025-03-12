## Purpose
The project exists to provide a secure and efficient way to verify if a requester's public IP address is trusted. This is achieved by integrating with the Unifi Site Manager API.  
Primarily meant to satisfy the [AllowedTlsAuthenticationEndpoints Intune CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-networklistmanager#allowedtlsauthenticationendpoints)

## Problem Solved
The system ensures that only trusted networks are flagged as 'domain' for Windows purposes.

## How It Works
1. The `UnifiService` interacts with the Unifi API to fetch a list of trusted IPs.
2. The `verify` function in `function_app.py` checks if the requester's IP is in the list of trusted IPs.
3. Logging is implemented to provide visibility into the process and aid debugging.
