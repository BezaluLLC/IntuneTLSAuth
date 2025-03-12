import requests
import logging
from ipaddress import ip_address, ip_network

class UnifiService:
    """
    Service for interacting with the Unifi Site Manager API.
    """

    BASE_URL = "https://api.ui.com"

    def __init__(self):
        import os
        self.api_token = os.getenv("UNIFI_API_TOKEN")
        if not self.api_token:
            raise ValueError("Unifi API token is not set in environment variables.")
        self.session = requests.Session()
        self.session.headers.update({"Authorization": f"Bearer {self.api_token}"})

    def get_trusted_ips(self) -> list:
        """
        Retrieve the list of trusted public IPs from the Unifi API.
        """
        try:
            response = self.session.get(
                f"{self.BASE_URL}/ea/hosts",
                headers={"X-API-KEY": self.api_token},
            )
            response.raise_for_status()
        
            data = response.json().get("data", [])
    
            trusted_ips = [host.get("ipAddress") for host in data if host.get("ipAddress")]
    
            logging.info(f"Trusted IPs retrieved: {trusted_ips}")
            return trusted_ips
        except requests.RequestException as e:
            logging.error(f"Failed to retrieve trusted IPs: {e}")
            raise