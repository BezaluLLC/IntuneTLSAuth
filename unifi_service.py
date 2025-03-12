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

    def get_trusted_ips(self, page_size: int = 10) -> list:
        """
        Retrieve the list of trusted public IPs from the Unifi API.
        Supports pagination.
        """
        try:
            trusted_ips = []
            next_token = None
    
            while True:
                params = {"pageSize": page_size}
                if next_token:
                    params["nextToken"] = next_token
    
                response = self.session.get(
                    f"{self.BASE_URL}/ea/hosts",
                    headers={"X-API-KEY": self.api_token},
                    params=params,
                )
                response.raise_for_status()
                data = response.json().get("data", [])
                next_token = response.json().get("nextToken")
    
                for host in data:
                    ip = host.get("ip")
                    if ip:
                        trusted_ips.append(ip)
    
                if not next_token:
                    break
    
            logging.info(f"Trusted IPs retrieved: {trusted_ips}")
            return trusted_ips
        except requests.RequestException as e:
            logging.error(f"Failed to retrieve trusted IPs: {e}")
            raise