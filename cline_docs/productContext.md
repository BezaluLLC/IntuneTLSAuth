# Product Context

- Why this project exists: Intune does not provide a simplistic way to identify a network as 'trusted' or domain-type.
- What problems it solves: Provides a mechanism to identify trusted networks by validating the requester's public IP against a Unifi Dream Machine Pro using the Unifi Site Manager API.
- How it should work: The system should receive a plain, unauthenticated GET request and return a 200 response if the requester's public IP is present on the Unifi Dream Machine Pro.