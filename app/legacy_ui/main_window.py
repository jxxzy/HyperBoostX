"""
Main window UI for HyperBoost X using PySide6.
"""

from PySide6.QtWidgets import (QMainWindow, QWidget, QHBoxLayout, QVBoxLayout, 
                               QStackedWidget, QPushButton, QLabel)
from PySide6.QtGui import QFont
from PySide6.QtCore import Qt
from legacy_ui.pages.dashboard import DashboardPage
from legacy_ui.pages.tweaks import TweaksPage
from legacy_ui.pages.booster import BoosterPage
from legacy_ui.pages.monitor import MonitorPage
from legacy_ui.pages.network import NetworkPage
from legacy_ui.pages.repair import RepairPage
from legacy_ui.pages.drivers import DriversPage
from legacy_ui.pages.settings import SettingsPage
from core.logger import Logger

logger = Logger.get_logger(__name__)


class MainWindow(QMainWindow):
    """Main application window with sidebar navigation."""
    
    def __init__(self):
        super().__init__()
        self.setWindowTitle("HyperBoost X")
        self.setGeometry(100, 100, 1400, 900)
        self.setMinimumSize(1200, 800)
        
        # Pages dictionary
        self.pages = {}
        self.current_page = None
        
        # Create UI
        self._create_central_widget()
        self._create_sidebar()
        self._create_pages()
        
        # Apply default page
        self._show_page("dashboard")
    
    def _create_central_widget(self):
        """Create main layout."""
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        
        main_layout = QHBoxLayout(central_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)
        
        # Create stacked widget for pages
        self.stacked_widget = QStackedWidget()
        main_layout.addWidget(self.stacked_widget)
    
    def _create_sidebar(self):
        """Create sidebar navigation."""
        sidebar_widget = QWidget()
        sidebar_layout = QVBoxLayout(sidebar_widget)
        sidebar_layout.setContentsMargins(0, 0, 0, 0)
        sidebar_layout.setSpacing(0)
        
        # Title
        title = QLabel("HyperBoost X")
        title.setFont(QFont("Arial", 14, QFont.Bold))
        title.setStyleSheet("color: #2196F3; padding: 15px; background-color: #2c2c2c;")
        title.setAlignment(Qt.AlignCenter)
        sidebar_layout.addWidget(title)
        
        # Navigation buttons
        nav_items = [
            ("Dashboard", "dashboard"),
            ("Tweaks", "tweaks"),
            ("Game Booster", "booster"),
            ("Monitor", "monitor"),
            ("Network", "network"),
            ("Repair", "repair"),
            ("Drivers", "drivers"),
            ("Settings", "settings"),
        ]
        
        self.nav_buttons = {}
        for label, page_id in nav_items:
            btn = QPushButton(label)
            btn.setCheckable(True)
            btn.setMinimumHeight(45)
            btn.setStyleSheet("""
                QPushButton {
                    background-color: #2c2c2c;
                    color: #ffffff;
                    border: none;
                    text-align: left;
                    padding-left: 20px;
                    border-left: 3px solid transparent;
                }
                QPushButton:hover {
                    background-color: #3c3c3c;
                }
                QPushButton:checked {
                    background-color: #2196F3;
                    border-left: 3px solid #1976D2;
                }
            """)
            btn.clicked.connect(lambda checked, p=page_id: self._show_page(p))
            sidebar_layout.addWidget(btn)
            self.nav_buttons[page_id] = btn
        
        # Spacer
        sidebar_layout.addStretch()
        
        # Sidebar widget
        sidebar_widget.setMaximumWidth(200)
        sidebar_widget.setMinimumWidth(200)
        sidebar_widget.setStyleSheet("background-color: #2c2c2c; border-right: 1px solid #3c3c3c;")
        
        # Insert sidebar at the beginning
        self.stacked_widget.parent().layout().insertWidget(0, sidebar_widget)
    
    def _create_pages(self):
        """Create all application pages."""
        self.pages = {
            "dashboard": DashboardPage(),
            "tweaks": TweaksPage(),
            "booster": BoosterPage(),
            "monitor": MonitorPage(),
            "network": NetworkPage(),
            "repair": RepairPage(),
            "drivers": DriversPage(),
            "settings": SettingsPage(),
        }
        
        for page_id, page in self.pages.items():
            self.stacked_widget.addWidget(page)
    
    def _show_page(self, page_id: str):
        """Show a specific page."""
        if page_id in self.pages:
            if self.current_page:
                self.nav_buttons[self.current_page].setChecked(False)
            
            self.nav_buttons[page_id].setChecked(True)
            self.stacked_widget.setCurrentWidget(self.pages[page_id])
            self.pages[page_id].refresh()
            self.current_page = page_id
            logger.info(f"Switched to page: {page_id}")
