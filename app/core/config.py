"""
Configuration management for HyperBoostX.
Handles app settings, paths, and configuration files.
"""

import json
import os
from pathlib import Path
from typing import Any, Dict, Optional

from core.constants import APP_NAME as DEFAULT_APP_NAME
from core.constants import APP_VERSION


def _env_bool(name: str, default: bool) -> bool:
    value = os.environ.get(name)
    if value is None:
        return default
    return value.strip().lower() in {"1", "true", "yes", "on"}


def _env_int(name: str, default: int) -> int:
    try:
        return int(os.environ.get(name, str(default)) or default)
    except Exception:
        return default


class Config:
    """Application configuration handler."""
    
    APP_NAME = DEFAULT_APP_NAME
    VERSION = APP_VERSION
    AI_PROVIDER = os.environ.get("AI_PROVIDER", "nvidia")
    NVIDIA_BASE_URL = os.environ.get("NVIDIA_BASE_URL", "https://integrate.api.nvidia.com/v1")
    NVIDIA_CHAT_ENDPOINT = os.environ.get("NVIDIA_CHAT_ENDPOINT", "/chat/completions")
    NVIDIA_DEFAULT_MODEL = os.environ.get("NVIDIA_DEFAULT_MODEL", "nvidia/nemotron-3-nano-30b-a3b")
    NVIDIA_FALLBACK_MODEL = os.environ.get("NVIDIA_FALLBACK_MODEL", "nvidia/nvidia-nemotron-nano-9b-v2")
    AI_ASSISTANT_MODEL = os.environ.get("AI_ASSISTANT_MODEL", NVIDIA_DEFAULT_MODEL)
    AI_ANALYZER_MODEL = os.environ.get("AI_ANALYZER_MODEL", "nvidia/llama-3.3-nemotron-super-49b-v1.5")
    AI_SAFETY_MODEL = os.environ.get("AI_SAFETY_MODEL", "nvidia/nemotron-content-safety-reasoning-4b")
    AI_EMBED_MODEL = os.environ.get("AI_EMBED_MODEL", "nvidia/llama-nemotron-embed-1b-v2")
    AI_CLOUD_ENABLED = _env_bool("AI_CLOUD_ENABLED", False)
    AI_MODEL_AUTO_FALLBACK = _env_bool("AI_MODEL_AUTO_FALLBACK", True)
    AI_REQUIRE_ACTION_APPROVAL = _env_bool("AI_REQUIRE_ACTION_APPROVAL", True)
    AI_ENABLE_SAFETY_GUARD = _env_bool("AI_ENABLE_SAFETY_GUARD", True)
    AI_TIMEOUT_MS = _env_int("AI_TIMEOUT_MS", 30000)
    AI_MAX_RETRIES = _env_int("AI_MAX_RETRIES", 2)
    NVIDIA_MODELS = [
        {"id": "nvidia/nemotron-3-nano-30b-a3b", "label": "Fast Default", "purpose": "chat cepat, default, rekomendasi ringan"},
        {"id": "nvidia/llama-3.3-nemotron-super-49b-v1.5", "label": "Smart Balanced", "purpose": "analisis PC harian"},
        {"id": "nvidia/nemotron-3-super-120b-a12b", "label": "Deep Analyzer", "purpose": "bottleneck dan troubleshooting lebih dalam"},
        {"id": "nvidia/nemotron-3-ultra-550b-a55b", "label": "Max Reasoning", "purpose": "reasoning berat dan masalah kompleks"},
        {"id": "nvidia/llama-3.1-nemotron-ultra-253b-v1", "label": "Legacy Ultra", "purpose": "fallback reasoning kuat"},
        {"id": "nvidia/nvidia-nemotron-nano-9b-v2", "label": "Nano Lite", "purpose": "fallback cepat dan ringan"},
        {"id": "nvidia/nemotron-mini-4b-instruct", "label": "Mini Fast", "purpose": "respons cepat/simple"},
        {"id": "nvidia/nemotron-content-safety-reasoning-4b", "label": "Safety Reasoning", "purpose": "validasi aksi berisiko"},
        {"id": "nvidia/llama-3.1-nemoguard-8b-content-safety", "label": "Content Guard", "purpose": "blok rekomendasi tidak aman"},
        {"id": "nvidia/llama-3.1-nemoguard-8b-topic-control", "label": "Topic Guard", "purpose": "jaga AI tetap fokus ke HyperBoostX, optimasi PC, repair, gaming, monitoring"},
    ]
    
    # Default paths
    APP_DIR = Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) / "HyperBoost X"
    CONFIG_DIR = APP_DIR / "config"
    DATA_DIR = APP_DIR / "data"
    LOG_DIR = APP_DIR / "logs"
    BACKUP_DIR = APP_DIR / "backups"
    REPORTS_DIR = APP_DIR / "reports"
    PROFILES_DIR = APP_DIR / "profiles"
    SESSIONS_DIR = APP_DIR / "sessions"
    DIAGNOSTICS_DIR = APP_DIR / "diagnostics"
    
    # Default configuration
    DEFAULT_CONFIG = {
        "theme": "dark",
        "auto_backup": True,
        "log_level": "INFO",
        "check_updates": True,
        "startup_minimized": False,
        "auto_optimize_interval": 3600,  # 1 hour
        "ai_provider": AI_PROVIDER,
        "ai_cloud_enabled": AI_CLOUD_ENABLED,
        "ai_assistant_model": AI_ASSISTANT_MODEL,
        "ai_analyzer_model": AI_ANALYZER_MODEL,
        "ai_safety_model": AI_SAFETY_MODEL,
        "ai_embed_model": AI_EMBED_MODEL,
        "ai_model_auto_fallback": AI_MODEL_AUTO_FALLBACK,
        "ai_require_action_approval": AI_REQUIRE_ACTION_APPROVAL,
        "ai_enable_safety_guard": AI_ENABLE_SAFETY_GUARD,
        "nvidia_default_model": NVIDIA_DEFAULT_MODEL,
        "nvidia_fallback_model": NVIDIA_FALLBACK_MODEL,
        "ai_timeout_ms": AI_TIMEOUT_MS,
        "ai_max_retries": AI_MAX_RETRIES,
        "nvidia_base_url": NVIDIA_BASE_URL,
        "nvidia_chat_endpoint": NVIDIA_CHAT_ENDPOINT,
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
        cls.REPORTS_DIR.mkdir(parents=True, exist_ok=True)
        cls.PROFILES_DIR.mkdir(parents=True, exist_ok=True)
        cls.SESSIONS_DIR.mkdir(parents=True, exist_ok=True)
        cls.DIAGNOSTICS_DIR.mkdir(parents=True, exist_ok=True)
        
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


