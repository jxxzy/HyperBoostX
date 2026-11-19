"""
Validators for HyperBoost X.
Provides data validation utilities.
"""

import re
from typing import Optional, Any


class Validators:
    """Data validation utilities."""
    
    @staticmethod
    def is_valid_ip(ip: str) -> bool:
        """Validate IP address."""
        pattern = r'^(\d{1,3}\.){3}\d{1,3}$'
        if re.match(pattern, ip):
            parts = ip.split('.')
            return all(0 <= int(part) <= 255 for part in parts)
        return False
    
    @staticmethod
    def is_valid_dns(dns: str) -> bool:
        """Validate DNS address."""
        return Validators.is_valid_ip(dns)
    
    @staticmethod
    def is_valid_email(email: str) -> bool:
        """Validate email address."""
        pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
        return re.match(pattern, email) is not None
    
    @staticmethod
    def is_safe_string(value: str, max_length: int = 256) -> bool:
        """Check if string is safe for operations."""
        if not isinstance(value, str):
            return False
        if len(value) > max_length:
            return False
        if any(char in value for char in ['..', '\\\\', '/*', '*/']):
            return False
        return True
