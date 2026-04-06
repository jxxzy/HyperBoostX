"""
Parsers for HyperBoost X.
Provides data parsing utilities.
"""

import json
from typing import Dict, Any, Optional
from core.logger import Logger


logger = Logger.get_logger(__name__)


class Parsers:
    """Data parsing utilities."""
    
    @staticmethod
    def parse_json(json_str: str) -> Optional[Dict[str, Any]]:
        """Parse JSON string."""
        try:
            return json.loads(json_str)
        except json.JSONDecodeError as e:
            logger.error(f"Failed to parse JSON: {e}")
            return None
    
    @staticmethod
    def parse_json_file(file_path: str) -> Optional[Dict[str, Any]]:
        """Parse JSON file."""
        try:
            with open(file_path, 'r') as f:
                return json.load(f)
        except Exception as e:
            logger.error(f"Failed to parse JSON file {file_path}: {e}")
            return None
    
    @staticmethod
    def to_json(data: Dict[str, Any], indent: int = 2) -> str:
        """Convert data to JSON string."""
        try:
            return json.dumps(data, indent=indent)
        except Exception as e:
            logger.error(f"Failed to convert to JSON: {e}")
            return ""
