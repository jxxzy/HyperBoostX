"""
Performance profiles management for HyperBoost X.
Handles different optimization profiles like Gaming, Streaming, etc.
"""

import json
from pathlib import Path
from typing import Dict, List, Optional
from dataclasses import dataclass, asdict
from core.config import Config
from core.logger import Logger


logger = Logger.get_logger(__name__)


@dataclass
class Profile:
    """Represents an optimization profile."""
    name: str
    description: str
    settings: Dict[str, any]
    enabled: bool = False
    
    def to_dict(self) -> dict:
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


class ProfileManager:
    """Manages optimization profiles."""
    
    # Predefined profiles
    PROFILES = {
        "gaming": Profile(
            name="Gaming Mode",
            description="Optimized for maximum FPS and low latency",
            settings={
                "disable_background_apps": True,
                "high_priority_cpu": True,
                "disable_visual_effects": True,
                "increase_timer_resolution": True,
                "disable_xbox_overlay": True,
            }
        ),
        "streaming": Profile(
            name="Streaming Mode",
            description="Balanced for streaming with stable performance",
            settings={
                "optimize_gpu_performance": True,
                "stable_frame_times": True,
                "reduce_network_latency": True,
                "background_recording": True,
            }
        ),
        "productivity": Profile(
            name="Productivity Mode",
            description="Optimized for work and multitasking",
            settings={
                "balanced_performance": True,
                "enable_indexing": True,
                "normal_visual_effects": True,
                "network_optimization": False,
            }
        ),
        "battery": Profile(
            name="Battery Saver Mode",
            description="Extends battery life on laptops",
            settings={
                "reduce_cpu_frequency": True,
                "dim_display": True,
                "disable_background_sync": True,
                "low_power_mode": True,
            }
        ),
    }
    
    @staticmethod
    def get_profile(name: str) -> Optional[Profile]:
        """Get a profile by name."""
        return ProfileManager.PROFILES.get(name.lower())
    
    @staticmethod
    def get_all_profiles() -> Dict[str, Profile]:
        """Get all available profiles."""
        return ProfileManager.PROFILES.copy()
    
    @staticmethod
    def apply_profile(profile: Profile) -> bool:
        """Apply a profile to the system."""
        try:
            logger.info(f"Applying profile: {profile.name}")
            # Implementation would apply the settings
            return True
        except Exception as e:
            logger.error(f"Failed to apply profile: {e}")
            return False
