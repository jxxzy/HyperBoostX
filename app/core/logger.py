"""
Logging system for HyperBoost X.
Provides structured logging throughout the application.
"""

import logging
import logging.handlers
from pathlib import Path
from typing import Optional
from core.config import Config


class Logger:
    """Application logger handler."""
    
    _loggers: dict = {}
    _initialized: bool = False
    
    @classmethod
    def initialize(cls) -> None:
        """Initialize logging system."""
        if cls._initialized:
            return
        
        log_file = Config.LOG_DIR / "hyperboost.log"
        
        # Configure root logger
        root_logger = logging.getLogger()
        root_logger.setLevel(logging.DEBUG)
        if root_logger.handlers:
            cls._initialized = True
            return

        # Formatter
        formatter = logging.Formatter(
            '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )

        try:
            Config.LOG_DIR.mkdir(parents=True, exist_ok=True)
            file_handler = logging.handlers.RotatingFileHandler(
                log_file, maxBytes=10*1024*1024, backupCount=5
            )
            file_handler.setLevel(logging.DEBUG)
            file_handler.setFormatter(formatter)
            root_logger.addHandler(file_handler)
        except OSError:
            # Test and restricted environments may not allow writing under LocalAppData.
            pass

        # Console handler
        console_handler = logging.StreamHandler()
        console_handler.setLevel(logging.INFO)
        console_handler.setFormatter(formatter)
        root_logger.addHandler(console_handler)
        
        cls._initialized = True
    
    @classmethod
    def get_logger(cls, name: str) -> logging.Logger:
        """Get logger instance for module."""
        if name not in cls._loggers:
            cls._loggers[name] = logging.getLogger(name)
        return cls._loggers[name]
