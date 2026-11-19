"""Tweaks page for PySide6."""

from PySide6.QtWidgets import (QVBoxLayout, QHBoxLayout, QLineEdit, QCheckBox,
                               QPushButton, QTableWidget, QTableWidgetItem, QHeaderView)
from PySide6.QtCore import Qt
from PySide6.QtGui import QColor
from legacy_ui.pages.base_page import BasePage
from services.optimization.tweak_service import TweakService


class TweaksPage(BasePage):
    """Page for Windows tweaks and optimizations."""
    
    def __init__(self):
        self.tweak_service = TweakService()
        super().__init__()
    
    def _create_widgets(self):
        """Create tweaks page widgets."""
        self.create_title("Windows Tweaks & Optimizations")
        
        # Search bar
        search_layout = QHBoxLayout()
        search_label = QHBoxLayout()
        search_input = QLineEdit()
        search_input.setPlaceholderText("Search tweaks...")
        search_input.setMinimumHeight(35)
        search_input.setStyleSheet("""
            QLineEdit {
                background-color: #2c2c2c;
                color: #ffffff;
                border: 1px solid #3c3c3c;
                border-radius: 4px;
                padding: 8px;
            }
        """)
        search_layout.addWidget(search_input)
        self.layout.addLayout(search_layout)
        
        # Tweaks table
        tweaks_group = self.create_section("Available Tweaks")
        tweaks_layout = QVBoxLayout(tweaks_group)
        
        self.tweaks_table = QTableWidget()
        self.tweaks_table.setColumnCount(5)
        self.tweaks_table.setHorizontalHeaderLabels(["Name", "Description", "Risk", "Category", "Apply"])
        self.tweaks_table.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)
        self.tweaks_table.setStyleSheet("""
            QTableWidget {
                background-color: #1e1e1e;
                color: #ffffff;
                gridline-color: #3c3c3c;
                border: none;
            }
            QTableWidget::item {
                padding: 5px;
            }
            QTableWidget::item:selected {
                background-color: #2196F3;
            }
        """)
        tweaks_layout.addWidget(self.tweaks_table)
        
        # Apply button
        apply_btn = QPushButton("Apply Selected Tweaks")
        apply_btn.setMinimumHeight(40)
        apply_btn.setStyleSheet("""
            QPushButton {
                background-color: #2196F3;
                color: #ffffff;
                border: none;
                border-radius: 4px;
                padding: 8px 20px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #1976D2;
            }
            QPushButton:pressed {
                background-color: #1565C0;
            }
        """)
        tweaks_layout.addWidget(apply_btn)
        
        # Populate tweaks
        self.refresh()
        self.layout.addStretch()
    
    def refresh(self):
        """Refresh tweaks list."""
        try:
            tweaks = self.tweak_service.get_all_tweaks()
            self.tweaks_table.setRowCount(len(tweaks))
            
            for row, tweak in enumerate(tweaks):
                self.tweaks_table.setItem(row, 0, QTableWidgetItem(tweak.get("name", "")))
                self.tweaks_table.setItem(row, 1, QTableWidgetItem(tweak.get("description", "")))
                
                risk_item = QTableWidgetItem(tweak.get("risk", ""))
                risk_color = QColor("#4CAF50") if tweak.get("risk") == "Low" else QColor("#ff9800") if tweak.get("risk") == "Medium" else QColor("#f44336")
                risk_item.setForeground(risk_color)
                self.tweaks_table.setItem(row, 2, risk_item)
                
                self.tweaks_table.setItem(row, 3, QTableWidgetItem(tweak.get("category", "")))
                
                checkbox = QCheckBox()
                self.tweaks_table.setCellWidget(row, 4, checkbox)
        except Exception as e:
            self.show_message(f"Error loading tweaks: {e}", "error")
