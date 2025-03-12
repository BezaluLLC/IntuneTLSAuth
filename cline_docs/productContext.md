# Product Context

## Why this project exists
This project provides an endpoint for verifying if a requester's public IP address is trusted. It is designed to integrate with the Unifi API for IP verification.

## What problems it solves
1. Ensures only trusted IPs can access certain resources or services.
2. Provides a mechanism to log and debug IP verification requests.

## How it should work
1. The `/verify` endpoint receives an HTTP GET request.
2. The requester's IP address is extracted from the `X-Forwarded-For` header.
3. The IP is checked against a list of trusted IPs.
4. Logs are generated for debugging and monitoring purposes.
5. A response is returned indicating whether the IP is trusted or not.