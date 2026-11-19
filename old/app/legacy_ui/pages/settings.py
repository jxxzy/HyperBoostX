"""Settings page for PySide6."""

from PySide6.QtWidgets import QVBoxLayout, QHBoxLayout, QPushButton, QLabel, QCheckBox
from legacy_ui.pages.base_page import BasePage


class SettingsPage(BasePage):
    """Page for application settings."""
    
    def _create_widgets(self):
        """Create settings page widgets."""
        self.create_title("Settings")
        
        # General settings
        general_group = self.create_section("General Settings")
        general_layout = QVBoxLayout(general_group)
        
        settings = [
            ("Auto Backup", True),
            ("Check Updates", True),
            ("Startup Minimized", False),
            ("Dark Theme", True),
        ]
        
        for setting_name, enabled in settings:
            checkbox = QCheckBox(setting_name)
            checkbox.setChecked(enabled)
            checkbox.setStyleSheet("color: #ffffff;")
            general_layout.addWidget(checkbox)
        
        # Advanced settings
        advanced_group = self.create_section("Advanced Settings")
        advanced_layout = QVBoxLayout(advanced_group)
        
        advanced_settings = [
            ("Enable Admin Warnings", True),
            ("Log All Actions", True),
            ("Enable Telemetry", False),
        ]
        
        for setting_name, enabled in advanced_settings:
            checkbox = QCheckBox(setting_name)
            checkbox.setChecked(enabled)
            checkbox.setStyleSheet("color: #ffffff;")
            advanced_layout.addWidget(checkbox)
        
        # Save button
        button_group = self.create_section("")
        button_layout = QHBoxLayout(button_group)
        
        save_btn = QPushButton("Save Settings")
        save_btn.setMinimumHeight(40)
        save_btn.setStyleSheet("""
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
        button_layout.addWidget(save_btn)
        
        reset_btn = QPushButton("Reset to Defaults")
        reset_btn.setMinimumHeight(40)
        reset_btn.setStyleSheet("""
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
        button_layout.addWidget(reset_btn)
        
        self.layout.addStretch()
