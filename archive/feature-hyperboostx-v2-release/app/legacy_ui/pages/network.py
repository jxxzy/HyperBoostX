"""Network page for PySide6."""

from PySide6.QtWidgets import QVBoxLayout, QHBoxLayout, QPushButton, QLabel
from PySide6.QtCore import QTimer
from legacy_ui.pages.base_page import BasePage
from services.monitoring.system_info_service import SystemInfoService
from services.monitoring.monitor_service import MonitorService


class NetworkPage(BasePage):
    """Page for network optimization."""

    def __init__(self):
        self.system_info_service = SystemInfoService()
        self.monitor_service = MonitorService()
        self.network_labels = {}
        self.update_timer = None
        super().__init__()

    def _create_widgets(self):
        """Create network page widgets."""
        self.create_title("Network Optimizer")

        # Network info
        info_group = self.create_section("Network Information")
        info_layout = QVBoxLayout(info_group)

        fields = [
            ("adapter", "Adapter"),
            ("ip", "IP Address"),
            ("mac", "MAC Address"),
            ("speed", "Link Speed"),
            ("status", "Status"),
            ("download", "Download"),
            ("upload", "Upload"),
        ]

        for key, label_text in fields:
            label = QLabel(f"{label_text}: --")
            label.setStyleSheet("color: #ffffff;")
            info_layout.addWidget(label)
            self.network_labels[key] = label

        # Tools
        tools_group = self.create_section("Network Tools")
        tools_layout = QVBoxLayout(tools_group)

        tools = ["Test MTU", "Flush DNS", "Reset Adapter", "DNS Benchmark"]

        for tool in tools:
            btn = QPushButton(tool)
            btn.setMinimumHeight(40)
            btn.setStyleSheet("""
                QPushButton {
                    background-color: #2196F3;
                    color: #ffffff;
                    border: none;
                    border-radius: 4px;
                    padding: 8px;
                    font-weight: bold;
                }
                QPushButton:hover {
                    background-color: #1976D2;
                }
                QPushButton:pressed {
                    background-color: #1565C0;
                }
            """)
            tools_layout.addWidget(btn)

        self.update_timer = QTimer()
        self.update_timer.timeout.connect(self.refresh)
        self.update_timer.start(3000)

        self.layout.addStretch()
        self.refresh()

    def refresh(self):
        """Refresh network details."""
        try:
            self.clear_message()
            interfaces = self.system_info_service.get_network_info()
            stats = self.monitor_service.get_current_stats()

            if not interfaces:
                self.show_message("Network adapters not detected.", "warning")
                return

            # pick first active interface
            active_iface = next((name for name, data in interfaces.items() if data['stats'].get('is_up')), None)
            if not active_iface:
                active_iface = next(iter(interfaces), None)

            iface = interfaces.get(active_iface, {})
            self.network_labels["adapter"].setText(f"Adapter: {active_iface or '--'}")
            self.network_labels["ip"].setText(
                f"IP Address: {', '.join([a for a in iface.get('addresses', []) if ':' not in a and '.' in a]) or '--'}"
            )
            self.network_labels["mac"].setText(f"MAC Address: {iface.get('mac', '--')}" )
            self.network_labels["speed"].setText(f"Link Speed: {iface.get('stats', {}).get('speed_mbps', 0)} Mbps")
            self.network_labels["status"].setText(f"Status: {'Up' if iface.get('stats', {}).get('is_up') else 'Down'}")

            if stats:
                download = stats.get('network_download_mb_s', 0)
                upload = stats.get('network_upload_mb_s', 0)
                self.network_labels["download"].setText(f"Download: {download:.2f} MB/s")
                self.network_labels["upload"].setText(f"Upload: {upload:.2f} MB/s")
        except Exception as e:
            from core.logger import Logger
            Logger.get_logger(__name__).error(f"Error refreshing network page: {e}")
            self.show_message(f"Network refresh failed: {e}", "error")

    def showEvent(self, event):
        if self.update_timer and not self.update_timer.isActive():
            self.update_timer.start(3000)
        super().showEvent(event) if hasattr(super(), 'showEvent') else None

    def hideEvent(self, event):
        if self.update_timer and self.update_timer.isActive():
            self.update_timer.stop()
        super().hideEvent(event) if hasattr(super(), 'hideEvent') else None