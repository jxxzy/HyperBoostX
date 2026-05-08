"""
HyperBoost X - Premium Windows Optimization & Gaming Boost Utility
Main entry point for the application.
"""

import sys
import os
from pathlib import Path

# Add app to path
sys.path.insert(0, str(Path(__file__).parent))

from core.config import Config
from core.logger import Logger
from core.permissions import Permissions
from legacy_ui.main_window import MainWindow
from PySide6.QtWidgets import QApplication, QSplashScreen
from PySide6.QtGui import QPixmap, QFont
from PySide6.QtCore import Qt


def main():
    """Initialize and run the HyperBoost X application."""
    # Initialize configuration
    Config.initialize()
    
    # Initialize logger
    Logger.initialize()
    logger = Logger.get_logger(__name__)
    
    # Check for admin privileges
    if not Permissions.is_admin():
        logger.warning("Application should run with admin privileges for full functionality")
    
    logger.info("Starting HyperBoost X...")
    
    # Create application
    app = QApplication(sys.argv)
    app.setApplicationName("HyperBoost X")
    app.setApplicationVersion("1.2.5")
    
    # Apply dark theme
    apply_dark_theme(app)
    
    # Create main window
    main_window = MainWindow()
    main_window.show()
    
    logger.info("HyperBoost X initialized successfully")
    
    # Start application
    sys.exit(app.exec())


def apply_dark_theme(app: QApplication):
    """Apply dark gaming theme."""
    dark_stylesheet = """
    QMainWindow, QDialog, QWidget {
        background-color: #1e1e1e;
        color: #ffffff;
    }
    QMenuBar {
        background-color: #2c2c2c;
        color: #ffffff;
        border-bottom: 1px solid #3c3c3c;
    }
    QMenuBar::item:selected {
        background-color: #2196F3;
    }
    QMenu {
        background-color: #2c2c2c;
        color: #ffffff;
        border: 1px solid #3c3c3c;
    }
    QMenu::item:selected {
        background-color: #2196F3;
    }
    QPushButton {
        background-color: #2196F3;
        color: #ffffff;
        border: none;
        border-radius: 4px;
        padding: 6px 16px;
        font-weight: bold;
    }
    QPushButton:hover {
        background-color: #1976D2;
    }
    QPushButton:pressed {
        background-color: #1565C0;
    }
    QLineEdit, QTextEdit, QComboBox {
        background-color: #2c2c2c;
        color: #ffffff;
        border: 1px solid #3c3c3c;
        border-radius: 4px;
        padding: 4px;
    }
    QLineEdit:focus, QTextEdit:focus, QComboBox:focus {
        border: 2px solid #2196F3;
    }
    QTabWidget::pane {
        border: 1px solid #3c3c3c;
    }
    QTabBar::tab {
        background-color: #2c2c2c;
        color: #ffffff;
        padding: 6px 16px;
        border: none;
    }
    QTabBar::tab:selected {
        background-color: #2196F3;
        color: #ffffff;
    }
    QHeaderView::section {
        background-color: #2c2c2c;
        color: #ffffff;
        padding: 4px;
        border: none;
        border-right: 1px solid #3c3c3c;
    }
    QTableWidget, QTreeWidget, QListWidget {
        background-color: #1e1e1e;
        color: #ffffff;
        gridline-color: #3c3c3c;
        border: 1px solid #3c3c3c;
    }
    QTableWidget::item:selected, QTreeWidget::item:selected, QListWidget::item:selected {
        background-color: #2196F3;
    }
    QScrollBar:vertical {
        background-color: #1e1e1e;
        width: 12px;
        border: none;
    }
    QScrollBar::handle:vertical {
        background-color: #555555;
        border-radius: 6px;
        min-height: 20px;
    }
    QScrollBar::handle:vertical:hover {
        background-color: #2196F3;
    }
    QScrollBar:horizontal {
        background-color: #1e1e1e;
        height: 12px;
        border: none;
    }
    QScrollBar::handle:horizontal {
        background-color: #555555;
        border-radius: 6px;
        min-width: 20px;
    }
    QScrollBar::handle:horizontal:hover {
        background-color: #2196F3;
    }
    """
    app.setStyleSheet(dark_stylesheet)


if __name__ == "__main__":
    main()


if __name__ == "__main__":
    main()
