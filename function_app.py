import azure.functions as func
import logging
from unifi_service import UnifiService

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

@app.route(route="verify", methods=["GET"])
def verify(req: func.HttpRequest) -> func.HttpResponse:
    """
    HTTP trigger function to verify if the requester's public IP is trusted.
    """
    logging.info("Processing verification request.")

    # Initialize UnifiService
    try:
        unifi_service = UnifiService()
        # Fetch trusted IPs from Unifi API
        trusted_ips = unifi_service.get_trusted_ips()
    except Exception as e:
        logging.error(f"Error initializing UnifiService or fetching trusted IPs: {e}")
        return func.HttpResponse("Failed to process request.", status_code=500)

    logging.info(f"Trusted IPs: {trusted_ips}")

    requester_ip = req.headers.get("client-ip", "Unknown IP").split(":")[0]

    if requester_ip in trusted_ips:
        logging.info(f"Match found: IP {requester_ip} is trusted.")
        return func.HttpResponse(f"IP {requester_ip} is trusted.", status_code=200)
    else:
        logging.info(f"No match: IP {requester_ip} is not trusted.")
        return func.HttpResponse(f"IP {requester_ip} is not trusted.", status_code=403)