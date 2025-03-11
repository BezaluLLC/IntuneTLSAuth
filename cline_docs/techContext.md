# Technical Context

- Technologies used: Python programming model v2, Azure Functions, Unifi Site Manager API.
- Development setup: Serverless Azure function with secure credential management via Azure Key Vaults.
- Technical constraints: The endpoint must not require authentication, and the Unifi API must be checked on every incoming request for up-to-date validations.