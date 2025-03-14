import requests
import logging
import aiohttp
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

    async def get_trusted_ips_async(self) -> list:
        """
        Asynchronously retrieve the list of trusted public IPs from the Unifi API.
        """
        try:
            async with aiohttp.ClientSession(headers={"Authorization": f"Bearer {self.api_token}"}) as session:
                async with session.get(f"{self.BASE_URL}/ea/hosts", headers={"X-API-KEY": self.api_token}) as response:
                    response.raise_for_status()
                    data = await response.json()
            
            trusted_ips = [host.get("ipAddress") for host in data.get("data", []) if host.get("ipAddress")]
    
            logging.info(f"Trusted IPs retrieved: {trusted_ips}")
            return trusted_ips
        except aiohttp.ClientError as e:
            logging.error(f"Failed to retrieve trusted IPs: {e}")
            raise