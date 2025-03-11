# System Patterns

- How the system is built: The system should be built as a serverless Azure function using Python programming model v2.
- Key technical decisions: 
  - Credentials should be stored in Azure Key Vaults.
  - The Unifi API should be checked on every incoming request to ensure up-to-date validations.
  - The endpoint/http trigger must not require authentication.
- Architecture patterns: Serverless architecture leveraging Azure Functions and secure credential management via Key Vaults.