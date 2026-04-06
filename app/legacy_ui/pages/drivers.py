"""Drivers page for PySide6."""

from PySide6.QtWidgets import (QVBoxLayout, QHBoxLayout, QPushButton,
                               QTableWidget, QTableWidgetItem, QHeaderView, QLabel)
from PySide6.QtGui import QColor
from PySide6.QtCore import Qt, QTimer
from legacy_ui.pages.base_page import BasePage
from services.repair.driver_service import DriverService
from core.logger import Logger


class DriversPage(BasePage):
    """Page for driver management."""

    def __init__(self):
        self.driver_service = DriverService()
        self.update_timer = None
        self.drivers_table = None
        super().__init__()

    def _create_widgets(self):
        """Create drivers page widgets."""
        self.create_title("Driver Center")

        # Driver status table
        drivers_group = self.create_section("Installed Drivers")
        drivers_layout = QVBoxLayout(drivers_group)

        self.drivers_table = QTableWidget()
        self.drivers_table.setColumnCount(4)
        self.drivers_table.setHorizontalHeaderLabels(["Device", "Manufacturer", "Version", "Status"])
        self.drivers_table.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)
        self.drivers_table.verticalHeader().setVisible(False)
        self.drivers_table.setEditTriggers(QTableWidget.NoEditTriggers)
        self.drivers_table.setSelectionBehavior(QTableWidget.SelectRows)
        self.drivers_table.setStyleSheet("""
            QTableWidget {
                background-color: #1e1e1e;
                color: #ffffff;
                gridline-color: #3c3c3c;
            }
        """)
        drivers_layout.addWidget(self.drivers_table)

        # Action buttons
        action_group = self.create_section("Actions")
        action_layout = QHBoxLayout(action_group)

        refresh_btn = QPushButton("Refresh Drivers")
        refresh_btn.setMinimumHeight(40)
        refresh_btn.setStyleSheet("""
            QPushButton {
                background-color: #2196F3;
                color: #ffffff;
                border: none;
                border-radius: 4px;
                padding: 10px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #1976D2;
            }
            QPushButton:pressed {
                background-color: #1565C0;
            }
        """)
        refresh_btn.clicked.connect(self.refresh)
        action_layout.addWidget(refresh_btn)

        self.update_timer = QTimer()
        self.update_timer.timeout.connect(self.refresh)
        self.update_timer.start(20000)

        self.layout.addStretch()
        self.refresh()

    def refresh(self):
        """Refresh driver information."""
        try:
            self.clear_message()
            drivers = self.driver_service.get_installed_drivers()
            self.drivers_table.setRowCount(len(drivers))

            if not drivers:
                self.show_message(
                    "No drivers could be read. Run as administrator or check system access.",
                    "warning"
                )
                return

            for row, driver in enumerate(drivers):
                name = QTableWidgetItem(driver.get("name", "Unknown"))
                manufacturer = QTableWidgetItem(driver.get("manufacturer", "Unknown"))
                version = QTableWidgetItem(driver.get("version", "Unknown"))
                status_text = driver.get("status", "Unknown")
                status_item = QTableWidgetItem(status_text)

                status_color = QColor("#9E9E9E")
                status_lower = status_text.lower()
                if "outdated" in status_lower or "update" in status_lower:
                    status_color = QColor("#FF9800")
                elif "error" in status_lower or "fail" in status_lower:
                    status_color = QColor("#f44336")
                elif "updated" in status_lower or "installed" in status_lower or "ok" in status_lower:
                    status_color = QColor("#4CAF50")

                status_item.setForeground(status_color)

                for item in (name, manufacturer, version, status_item):
                    item.setFlags(Qt.ItemIsEnabled | Qt.ItemIsSelectable)

                self.drivers_table.setItem(row, 0, name)
                self.drivers_table.setItem(row, 1, manufacturer)
                self.drivers_table.setItem(row, 2, version)
                self.drivers_table.setItem(row, 3, status_item)

        except Exception as e:
            Logger.get_logger(__name__).error(f"Error refreshing driver list: {e}")
            self.show_message(f"Driver refresh failed: {e}", "error")

    def showEvent(self, event):
        if self.update_timer and not self.update_timer.isActive():
            self.update_timer.start(20000)
        super().showEvent(event) if hasattr(super(), "showEvent") else None

    def hideEvent(self, event):
        if self.update_timer and self.update_timer.isActive():
            self.update_timer.stop()
        super().hideEvent(event) if hasattr(super(), "hideEvent") else None
