import azure.functions as func
import datetime
import json
import logging

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

@app.route(route="verify", methods=["GET"])
def verify(req: func.HttpRequest) -> func.HttpResponse:
    """
    HTTP trigger function to verify if the requester's public IP is trusted.
    """
    logging.info("Processing verification request.")

    # Placeholder logic for IP verification
    # Replace this with actual Unifi API integration
    trusted_ips = ["192.168.1.1", "192.168.1.2"]  # Example trusted IPs
    requester_ip = req.headers.get("client-ip", "Unknown IP")

    logging.info(f"Requester IP: {requester_ip}")
    logging.info(f"Request headers: {dict(req.headers)}")
    
    if requester_ip in trusted_ips:
        return func.HttpResponse(f"IP {requester_ip} is trusted.", status_code=200)
    else:
        return func.HttpResponse(f"IP {requester_ip} is not trusted.", status_code=403)