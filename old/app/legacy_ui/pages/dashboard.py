"""Dashboard page for PySide6."""

from PySide6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QGridLayout,
                               QLabel, QPushButton, QProgressBar, QGroupBox)
from PySide6.QtGui import QFont, QColor
from PySide6.QtCore import Qt, QTimer
from legacy_ui.pages.base_page import BasePage
from services.monitoring.system_info_service import SystemInfoService
from services.monitoring.monitor_service import MonitorService


class DashboardPage(BasePage):
    """Dashboard page with system overview."""
    
    def __init__(self):
        self.system_info_service = SystemInfoService()
        self.monitor_service = MonitorService()
        self.update_timer = None
        self.progress_bars = {}
        self.identity_labels = {}
        super().__init__()
    
    def _create_widgets(self):
        """Create dashboard widgets."""
        self.create_title("System Dashboard")
        
        # System Core / Identity
        identity_group = self.create_section("System Core")
        identity_layout = QVBoxLayout(identity_group)
        identity_fields = [
            ("os_info", "OS"),
            ("host_info", "Hostname / User"),
            ("uptime", "Uptime"),
            ("edition", "Windows Edition"),
            ("secure_boot", "Secure Boot"),
        ]
        for key, text in identity_fields:
            label = QLabel(f"{text}: --")
            label.setStyleSheet("color: #ffffff; font-size: 10pt;")
            identity_layout.addWidget(label)
            self.identity_labels[key] = label
        
        # Health Score Card
        health_group = self.create_section("System Health")
        health_layout = QVBoxLayout(health_group)
        
        self.health_label = QLabel("85/100")
        self.health_label.setFont(QFont("Arial", 32, QFont.Bold))
        self.health_label.setStyleSheet("color: #4CAF50;")
        health_layout.addWidget(self.health_label)
        
        status_label = QLabel("System is optimized and healthy")
        status_label.setStyleSheet("color: #999999;")
        health_layout.addWidget(status_label)
        
        # Stats Grid
        stats_group = self.create_section("System Statistics")
        stats_layout = QGridLayout(stats_group)
        
        self.stats_labels = {}
        stats = [
            ("CPU Usage", "cpu_usage", 0, 0),
            ("Memory Usage", "memory_usage", 0, 1),
            ("Disk Usage", "disk_usage", 1, 0),
            ("Network", "network_status", 1, 1),
        ]
        
        for label, stat_id, row, col in stats:
            stat_widget = self._create_stat_card(label, stat_id)
            stats_layout.addWidget(stat_widget, row, col)
            self.stats_labels[stat_id] = stat_widget.findChild(QLabel, "value")
        
        # Action buttons
        action_group = self.create_section("Quick Actions")
        action_layout = QHBoxLayout(action_group)
        
        optimize_btn = QPushButton("One-Click Optimize")
        optimize_btn.setMinimumHeight(40)
        optimize_btn.setStyleSheet("""
            QPushButton {
                background-color: #2196F3;
                color: #ffffff;
                border: none;
                border-radius: 4px;
                padding: 8px 20px;
                font-weight: bold;
                font-size: 11pt;
            }
            QPushButton:hover {
                background-color: #1976D2;
            }
            QPushButton:pressed {
                background-color: #1565C0;
            }
        """)
        action_layout.addWidget(optimize_btn)
        
        # Add stretch to push content to top
        self.layout.addStretch()
        
        # Setup real-time monitoring timer
        self.update_timer = QTimer()
        self.update_timer.timeout.connect(self.refresh)
        self.update_timer.start(1000)  # Update every 1 second (1000ms)
        
        # Initial refresh
        self.refresh()
    
    def _create_stat_card(self, title: str, stat_id: str) -> QWidget:
        """Create a statistics card."""
        card = QWidget()
        card.setStyleSheet("""
            QWidget {
                background-color: #2c2c2c;
                border: 1px solid #3c3c3c;
                border-radius: 4px;
                padding: 15px;
                min-height: 140px;
            }
        """)
        
        layout = QVBoxLayout(card)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(8)
        
        title_label = QLabel(title)
        title_label.setStyleSheet("color: #999999; font-size: 10pt;")
        layout.addWidget(title_label)
        
        value_label = QLabel("--")
        value_label.setObjectName("value")
        value_label.setFont(QFont("Arial", 12, QFont.Bold))
        value_label.setStyleSheet("color: #2196F3; line-height: 1.4;")
        value_label.setWordWrap(True)
        layout.addWidget(value_label)
        
        progress = QProgressBar()
        progress.setMaximum(100)
        progress.setValue(0)
        progress.setStyleSheet("""
            QProgressBar {
                border: none;
                background-color: #1e1e1e;
                border-radius: 3px;
                height: 4px;
            }
            QProgressBar::chunk {
                background-color: #2196F3;
                border-radius: 3px;
            }
        """)
        layout.addWidget(progress)
        layout.addStretch()
        
        # Store progress bar reference for updates
        self.progress_bars[stat_id] = progress
        
        return card
    
    def refresh(self):
        """Refresh dashboard data in real-time."""
        try:
            self.clear_message()
            stats = self.monitor_service.get_current_stats()
            identity = self.system_info_service.get_system_identity()
            windows_details = self.system_info_service.get_windows_system_details()
            
            if identity:
                self.identity_labels["os_info"].setText(
                    f"OS: {identity.get('os_name', '--')} {identity.get('os_release', '')} {identity.get('os_version', '')}"
                )
                self.identity_labels["host_info"].setText(
                    f"Host: {identity.get('hostname', '--')} | User: {identity.get('user', '--')}"
                )
                self.identity_labels["uptime"].setText(
                    f"Uptime: {identity.get('uptime', {}).get('formatted', '--')}"
                )
            if windows_details:
                self.identity_labels["edition"].setText(
                    f"Edition: {windows_details.get('edition', '--')}"
                )
                self.identity_labels["secure_boot"].setText(
                    f"Secure Boot: {windows_details.get('secure_boot', '--')}"
                )
            
            if not stats:
                self.show_message("Unable to load dashboard statistics. Check permissions or backend status.", "warning")
                return
            
            # Update CPU stats with frequency and cores info
            if "cpu" in stats and stats["cpu"] is not None:
                cpu_value = float(stats['cpu'])
                cpu_freq = float(stats.get("cpu_freq", 0))
                cores = int(stats.get("cpu_cores", 0))
                threads = int(stats.get("cpu_threads", 0))
                
                cpu_text = f"{cpu_value:.1f}%"
                if cpu_freq > 0:
                    cpu_text += f"\n({cpu_freq:.2f}GHz)"
                if cores > 0:
                    cpu_text += f"\n({cores}C/{threads}T)"
                
                self.stats_labels["cpu_usage"].setText(cpu_text)
                self.progress_bars["cpu_usage"].setValue(min(100, max(0, int(cpu_value))))
            
            # Update Memory stats with GB values
            if "memory" in stats and stats["memory"] is not None:
                memory_value = float(stats['memory'])
                memory_used_gb = float(stats.get("memory_used_gb", 0))
                memory_total_gb = float(stats.get("memory_total_gb", 0))
                
                memory_text = f"{memory_value:.1f}%"
                if memory_total_gb > 0:
                    memory_text += f"\n({memory_used_gb:.1f}GB / {memory_total_gb:.1f}GB)"
                
                self.stats_labels["memory_usage"].setText(memory_text)
                self.progress_bars["memory_usage"].setValue(min(100, max(0, int(memory_value))))
            
            # Update Disk stats with GB values
            if "disk" in stats and stats["disk"] is not None:
                disk_value = float(stats['disk'])
                disk_used_gb = float(stats.get("disk_used_gb", 0))
                disk_total_gb = float(stats.get("disk_total_gb", 0))
                
                disk_text = f"{disk_value:.1f}%"
                if disk_total_gb > 0:
                    disk_text += f"\n({disk_used_gb:.1f}GB / {disk_total_gb:.1f}GB)"
                
                self.stats_labels["disk_usage"].setText(disk_text)
                self.progress_bars["disk_usage"].setValue(min(100, max(0, int(disk_value))))
            
            # Update Network stats with detailed info
            net_stats = self.monitor_service.get_network_stats()
            if net_stats:
                sent_mb = net_stats.get('bytes_sent', 0) / (1024 * 1024)
                recv_mb = net_stats.get('bytes_recv', 0) / (1024 * 1024)
                self.stats_labels["network_status"].setText(
                    f"↓{recv_mb:.1f}MB\n↑{sent_mb:.1f}MB"
                )
                # Network progress as percentage of data (capped at 100)
                total_mb = sent_mb + recv_mb
                network_percent = min(100, max(0, int(total_mb % 100)))
                self.progress_bars["network_status"].setValue(network_percent)
            
        except Exception as e:
            from core.logger import Logger
            Logger.get_logger(__name__).error(f"Error refreshing dashboard: {e}")
            self.show_message(f"Dashboard refresh failed: {e}", "error")
    
    def showEvent(self, event):
        """Start monitoring when page becomes visible."""
        if self.update_timer and not self.update_timer.isActive():
            self.update_timer.start(1000)
        super().showEvent(event) if hasattr(super(), 'showEvent') else None
    
    def hideEvent(self, event):
        """Stop monitoring when page becomes hidden."""
        if self.update_timer and self.update_timer.isActive():
            self.update_timer.stop()
        super().hideEvent(event) if hasattr(super(), 'hideEvent') else None
    
    def closeEvent(self, event):
        """Clean up timer when page is closed."""
        if self.update_timer:
            self.update_timer.stop()
        super().closeEvent(event) if hasattr(super(), 'closeEvent') else None
