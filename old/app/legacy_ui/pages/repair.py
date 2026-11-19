"""Repair page for PySide6."""

from PySide6.QtWidgets import QVBoxLayout, QHBoxLayout, QPushButton, QLabel
from legacy_ui.pages.base_page import BasePage


class RepairPage(BasePage):
    """Page for system repair and maintenance."""
    
    def _create_widgets(self):
        """Create repair page widgets."""
        self.create_title("Repair & Maintenance Tools")
        
        # Repair tools
        tools_group = self.create_section("System Repair Tools")
        tools_layout = QVBoxLayout(tools_group)
        
        tools = [
            ("Run SFC Scan", "System File Checker"),
            ("Run DISM", "Deployment Image Servicing"),
            ("Flush DNS", "Clear DNS cache"),
            ("Create Restore Point", "Safe system backup"),
        ]
        
        for tool_name, description in tools:
            tool_layout = QHBoxLayout()
            
            info_layout = QVBoxLayout()
            name_label = QLabel(tool_name)
            name_label.setStyleSheet("color: #ffffff; font-weight: bold;")
            info_layout.addWidget(name_label)
            
            desc_label = QLabel(description)
            desc_label.setStyleSheet("color: #999999; font-size: 9pt;")
            info_layout.addWidget(desc_label)
            
            tool_layout.addLayout(info_layout)
            
            btn = QPushButton("Run")
            btn.setMaximumWidth(100)
            btn.setMinimumHeight(35)
            btn.setStyleSheet("""
                QPushButton {
                    background-color: #2196F3;
                    color: #ffffff;
                    border: none;
                    border-radius: 4px;
                    padding: 5px;
                    font-weight: bold;
                }
                QPushButton:hover {
                    background-color: #1976D2;
                }
                QPushButton:pressed {
                    background-color: #1565C0;
                }
            """)
            tool_layout.addWidget(btn)
            
            tools_layout.addLayout(tool_layout)
        
        self.layout.addStretch()
