"""Base page class for PySide6 UI."""

from PySide6.QtWidgets import QWidget, QVBoxLayout, QScrollArea, QLabel, QGroupBox
from PySide6.QtGui import QFont
from PySide6.QtCore import Qt
from abc import ABCMeta, abstractmethod


class MetaClass(type(QWidget), ABCMeta):
    """Metaclass combining QWidget and ABCMeta to resolve metaclass conflict."""
    pass


class BasePage(QWidget, metaclass=MetaClass):
    """Base class for all application pages."""
    
    def __init__(self):
        super().__init__()
        self.setStyleSheet("background-color: #1e1e1e;")
        self.layout = QVBoxLayout(self)
        self.layout.setContentsMargins(20, 20, 20, 20)
        self.layout.setSpacing(15)

        self.notification_label = QLabel()
        self.notification_label.setVisible(False)
        self.notification_label.setWordWrap(True)
        self.layout.addWidget(self.notification_label)

        self._create_widgets()
    
    @abstractmethod
    def _create_widgets(self):
        """Create page-specific widgets. Must be implemented by subclasses."""
        pass
    
    def refresh(self):
        """Refresh page data. Can be overridden by subclasses."""
        pass
    
    def create_title(self, text: str) -> QLabel:
        """Create a page title."""
        title = QLabel(text)
        title.setFont(QFont("Arial", 18, QFont.Bold))
        title.setStyleSheet("color: #2196F3; padding: 10px 0px;")
        self.layout.addWidget(title)
        return title
    
    def create_section(self, title: str) -> QGroupBox:
        """Create a section group."""
        group = QGroupBox(title)
        group.setStyleSheet("""
            QGroupBox {
                color: #ffffff;
                border: 1px solid #3c3c3c;
                border-radius: 4px;
                margin-top: 10px;
                padding-top: 10px;
                background-color: #2c2c2c;
            }
            QGroupBox::title {
                subcontrol-origin: margin;
                left: 10px;
                padding: 0px 3px 0px 3px;
            }
        """)
        self.layout.addWidget(group)
        return group
    
    def create_scrollable_area(self):
        """Create a scrollable area for the page."""
        scroll = QScrollArea()
        scroll.setStyleSheet("""
            QScrollArea {
                background-color: #1e1e1e;
                border: none;
            }
        """)
        scroll.setWidgetResizable(True)
        self.layout.addWidget(scroll)
        return scroll

    def show_message(self, message: str, level: str = "info"):
        """Show a notification message on the page."""
        colors = {
            "info": "#2196F3",
            "success": "#4CAF50",
            "warning": "#FF9800",
            "error": "#f44336",
        }
        background_color = colors.get(level, colors["info"])
        self.notification_label.setText(message)
        self.notification_label.setStyleSheet(
            f"background-color: {background_color}; color: #ffffff;"
            "border-radius: 4px; padding: 10px;"
        )
        self.notification_label.setVisible(True)

    def clear_message(self):
        """Clear the page notification message."""
        if self.notification_label:
            self.notification_label.setVisible(False)
            self.notification_label.setText("")
