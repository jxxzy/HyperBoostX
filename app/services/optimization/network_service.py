"""Network service for HyperBoost X."""

import socket
import time
from utils.shell import ShellUtil
from core.logger import Logger


logger = Logger.get_logger(__name__)


class NetworkService:
    """Service for network optimization."""
    
    @staticmethod
    def test_dns() -> dict:
        """Test DNS performance."""
        logger.info("Testing DNS")
        start = time.perf_counter()
        socket.getaddrinfo("dns.google", 53)
        response_time = round((time.perf_counter() - start) * 1000, 2)
        status = "Good" if response_time < 100 else "Fair" if response_time < 250 else "Slow"
        return {"response_time": response_time, "status": status}
    
    @staticmethod
    def flush_dns() -> bool:
        """Flush DNS cache."""
        logger.info("Flushing DNS")
        try:
            success, _ = ShellUtil.execute_command("ipconfig /flushdns", admin=True)
            return success
        except Exception as e:
            logger.error(f"Failed to flush DNS: {e}")
            return False
    
    @staticmethod
    def optimize_tcp() -> bool:
        """Optimize TCP settings."""
        logger.info("Optimizing TCP")
        try:
            success, _ = ShellUtil.execute_command(
                "netsh int tcp set global autotuninglevel=normal",
                admin=True
            )
            return success
        except Exception as e:
            logger.error(f"Failed to optimize TCP: {e}")
            return False
