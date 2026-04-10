"""
Configuration management for HyperBoost X.
Handles app settings, paths, and configuration files.
"""

import json
import os
from pathlib import Path
from typing import Any, Dict, Optional


class Config:
    """Application configuration handler."""
    
    APP_NAME = "HyperBoost X"
    VERSION = "1.2.2-dev"
    
    # Default paths
    APP_DIR = Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) / "HyperBoost X"
    CONFIG_DIR = APP_DIR / "config"
    DATA_DIR = APP_DIR / "data"
    LOG_DIR = APP_DIR / "logs"
    BACKUP_DIR = APP_DIR / "backups"
    
    # Default configuration
    DEFAULT_CONFIG = {
        "theme": "dark",
        "auto_backup": True,
        "log_level": "INFO",
        "check_updates": True,
        "startup_minimized": False,
        "auto_optimize_interval": 3600,  # 1 hour
    }
    
    _config: Dict[str, Any] = {}
    _initialized: bool = False
    
    @classmethod
    def initialize(cls) -> None:
        """Initialize configuration directories and load settings."""
        if cls._initialized:
            return
        
        # Create directories
        cls.CONFIG_DIR.mkdir(parents=True, exist_ok=True)
        cls.DATA_DIR.mkdir(parents=True, exist_ok=True)
        cls.LOG_DIR.mkdir(parents=True, exist_ok=True)
        cls.BACKUP_DIR.mkdir(parents=True, exist_ok=True)
        
        # Load or create config
        cls._load_config()
        cls._initialized = True
    
    @classmethod
    def _load_config(cls) -> None:
        """Load configuration from file or create default."""
        config_file = cls.CONFIG_DIR / "config.json"
        
        if config_file.exists():
            try:
                with open(config_file, 'r') as f:
                    cls._config = json.load(f)
            except Exception:
                cls._config = cls.DEFAULT_CONFIG.copy()
        else:
            cls._config = cls.DEFAULT_CONFIG.copy()
            cls._save_config()
    
    @classmethod
    def _save_config(cls) -> None:
        """Save configuration to file."""
        config_file = cls.CONFIG_DIR / "config.json"
        try:
            with open(config_file, 'w') as f:
                json.dump(cls._config, f, indent=2)
        except Exception as e:
            print(f"Failed to save config: {e}")
    
    @classmethod
    def get(cls, key: str, default: Any = None) -> Any:
        """Get configuration value."""
        return cls._config.get(key, default)
    
    @classmethod
    def set(cls, key: str, value: Any) -> None:
        """Set configuration value."""
        cls._config[key] = value
        cls._save_config()
    
    @classmethod
    def get_all(cls) -> Dict[str, Any]:
        """Get all configuration values."""
        return cls._config.copy()
