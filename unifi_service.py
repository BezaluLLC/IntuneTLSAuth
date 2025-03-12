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
        Filters out private IP addresses and supports pagination.
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
                    ip = host.get("IP")
                    if ip and not self._is_private_ip(ip):
                        trusted_ips.append(ip)
    
                if not next_token:
                    break
    
            logging.debug(f"Trusted IPs retrieved: {trusted_ips}")
            return trusted_ips
        except requests.RequestException as e:
            logging.error(f"Failed to retrieve trusted IPs: {e}")
            raise

    @staticmethod
    def _is_private_ip(ip: str) -> bool:
        """
        Check if an IP address is private.
        """
        private_networks = [
            ip_network("10.0.0.0/8"),
            ip_network("172.16.0.0/12"),
            ip_network("192.168.0.0/16"),
        ]
        ip_obj = ip_address(ip)
        return any(ip_obj in network for network in private_networks)