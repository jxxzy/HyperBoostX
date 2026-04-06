"""Game Booster page for PySide6."""

from PySide6.QtWidgets import QVBoxLayout, QHBoxLayout, QPushButton, QComboBox, QLabel
from PySide6.QtGui import QFont
from legacy_ui.pages.base_page import BasePage
from services.optimization.booster_service import BoosterService


class BoosterPage(BasePage):
    """Page for game optimization and boosting."""
    
    def __init__(self):
        self.booster_service = BoosterService()
        super().__init__()
    
    def _create_widgets(self):
        """Create booster page widgets."""
        self.create_title("Game Booster Engine")
        
        # Profile selection
        profile_group = self.create_section("Select Optimization Profile")
        profile_layout = QVBoxLayout(profile_group)
        
        profiles = ["FPS Mode", "Low Latency Mode", "Streaming Mode", "Balanced Mode"]
        
        for profile in profiles:
            btn = QPushButton(profile)
            btn.setMinimumHeight(45)
            btn.setStyleSheet("""
                QPushButton {
                    background-color: #2c2c2c;
                    color: #ffffff;
                    border: 1px solid #3c3c3c;
                    border-radius: 4px;
                    padding: 10px;
                    font-weight: bold;
                }
                QPushButton:hover {
                    background-color: #2196F3;
                    border: 1px solid #1976D2;
                }
                QPushButton:pressed {
                    background-color: #1565C0;
                    border: 1px solid #1565C0;
                }
            """)
            profile_layout.addWidget(btn)
        
        # Status
        status_group = self.create_section("Current Status")
        status_layout = QVBoxLayout(status_group)
        
        self.status_label = QLabel("No profile active")
        self.status_label.setStyleSheet("color: #999999; font-size: 11pt;")
        status_layout.addWidget(self.status_label)
        
        # Control buttons
        control_group = self.create_section("Controls")
        control_layout = QHBoxLayout(control_group)
        
        apply_btn = QPushButton("Apply Profile")
        apply_btn.setMinimumHeight(40)
        apply_btn.setStyleSheet("""
            QPushButton {
                background-color: #4CAF50;
                color: #ffffff;
                border: none;
                border-radius: 4px;
                padding: 10px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #45a049;
            }
            QPushButton:pressed {
                background-color: #3d8b40;
            }
        """)
        control_layout.addWidget(apply_btn)
        
        revert_btn = QPushButton("Revert Changes")
        revert_btn.setMinimumHeight(40)
        revert_btn.setStyleSheet("""
            QPushButton {
                background-color: #f44336;
                color: #ffffff;
                border: none;
                border-radius: 4px;
                padding: 10px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #da190b;
            }
            QPushButton:pressed {
                background-color: #ba0000;
            }
        """)
        control_layout.addWidget(revert_btn)
        
        self.layout.addStretch()
