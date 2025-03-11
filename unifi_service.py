import requests
import logging
from ipaddress import ip_address, ip_network

class UnifiService:
    """
    Service for interacting with the Unifi Site Manager API.
    """

    BASE_URL = "https://api.ui.com"

    def __init__(self, api_token: str):
        self.api_token = api_token
        self.session = requests.Session()
        self.session.headers.update({"Authorization": f"Bearer {self.api_token}"})

    def get_trusted_ips(self) -> list:
        """
        Retrieve the list of trusted public IPs from the Unifi API.
        Filters out private IP addresses.
        """
        try:
            response = self.session.get(f"{self.BASE_URL}/ea/sites")
            response.raise_for_status()
            data = response.json().get("data", [])

            trusted_ips = []
            for site in data:
                ip_addrs = site.get("ipAddrs", [])
                for ip in ip_addrs:
                    if not self._is_private_ip(ip):
                        trusted_ips.append(ip)

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