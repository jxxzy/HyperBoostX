"""Monitor page for PySide6."""

from PySide6.QtWidgets import (QVBoxLayout, QHBoxLayout, QPushButton, QLabel, 
                               QScrollArea, QWidget)
from PySide6.QtCore import QTimer
from legacy_ui.pages.base_page import BasePage
from services.monitoring.monitor_service import MonitorService


class MonitorPage(BasePage):
    """Page for real-time system monitoring."""
    
    def __init__(self):
        self.monitor_service = MonitorService()
        self.update_timer = None
        self.stat_labels = {}
        self.process_labels = []
        super().__init__()
    
    def _create_widgets(self):
        """Create monitor page widgets."""
        self.create_title("System Monitor")
        
        # Primary Stats Section
        stats_group = self.create_section("System Metrics")
        stats_layout = QVBoxLayout(stats_group)
        
        # Create labels for different stats
        stat_keys = [
            ("cpu", "CPU Usage"),
            ("cpu_freq", "CPU Frequency"),
            ("memory", "Memory Usage"),
            ("disk", "Disk Usage"),
            ("disk_speed", "Disk Speed"),
            ("network", "Network Status"),
            ("network_speed", "Network Speed"),
            ("gpu", "GPU Usage"),
            ("processes", "Running Processes"),
        ]
        
        for key, label_text in stat_keys:
            label = QLabel(f"{label_text}: --")
            label.setStyleSheet("color: #4CAF50; font-family: Courier; font-size: 10pt;")
            stats_layout.addWidget(label)
            self.stat_labels[key] = label
        
        # Top Processes Section
        processes_group = self.create_section("Top Processes by Memory")
        processes_layout = QVBoxLayout(processes_group)
        
        # Scrollable area for processes
        scroll = QScrollArea()
        scroll.setStyleSheet("""
            QScrollArea {
                background-color: #1e1e1e;
                border: 1px solid #3c3c3c;
                border-radius: 4px;
            }
        """)
        
        scroll_widget = QWidget()
        scroll_widget.setStyleSheet("background-color: #1e1e1e;")
        self.process_layout = QVBoxLayout(scroll_widget)
        self.process_layout.setContentsMargins(5, 5, 5, 5)
        
        # Create 5 placeholder labels for top processes
        for i in range(5):
            proc_label = QLabel(f"{i+1}. --")
            proc_label.setStyleSheet("color: #2196F3; font-family: Courier; font-size: 9pt;")
            self.process_layout.addWidget(proc_label)
            self.process_labels.append(proc_label)
        
        scroll.setWidget(scroll_widget)
        processes_layout.addWidget(scroll)
        
        # Control buttons
        control_group = self.create_section("Controls")
        control_layout = QHBoxLayout(control_group)
        
        start_btn = QPushButton("Start Monitoring")
        start_btn.setMinimumHeight(40)
        start_btn.setStyleSheet("""
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
        start_btn.clicked.connect(self.start_monitoring)
        control_layout.addWidget(start_btn)
        
        stop_btn = QPushButton("Stop Monitoring")
        stop_btn.setMinimumHeight(40)
        stop_btn.setStyleSheet("""
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
        stop_btn.clicked.connect(self.stop_monitoring)
        control_layout.addWidget(stop_btn)
        
        self.layout.addStretch()
        
        # Setup and start monitoring timer
        self.update_timer = QTimer()
        self.update_timer.timeout.connect(self.refresh)
        self.update_timer.start(1000)  # Update every 1 second
    
    def start_monitoring(self):
        """Start real-time monitoring."""
        if self.update_timer and not self.update_timer.isActive():
            self.update_timer.start(1000)
    
    def stop_monitoring(self):
        """Stop real-time monitoring."""
        if self.update_timer:
            self.update_timer.stop()
    
    def refresh(self):
        """Refresh monitor data in real-time."""
        try:
            stats = self.monitor_service.get_current_stats()
            
            if not stats:
                return  # Skip update if no data
            
            # Update CPU
            if "cpu" in stats and stats["cpu"] is not None:
                cpu_val = float(stats['cpu'])
                self.stat_labels["cpu"].setText(f"CPU Usage: {cpu_val:.1f}%")
            
            # Update CPU Frequency
            if "cpu_freq" in stats and stats["cpu_freq"] is not None:
                freq_val = float(stats["cpu_freq"])
                cores = int(stats.get("cpu_cores", 0))
                threads = int(stats.get("cpu_threads", 0))
                self.stat_labels["cpu_freq"].setText(
                    f"CPU Frequency: {freq_val:.2f} GHz ({cores}C/{threads}T)"
                )
            
            # Update Memory (with detailed GB info)
            if "memory" in stats and stats["memory"] is not None:
                mem_percent = float(stats['memory'])
                mem_used = float(stats.get("memory_used_gb", 0))
                mem_total = float(stats.get("memory_total_gb", 0))
                self.stat_labels["memory"].setText(
                    f"Memory Usage: {mem_percent:.1f}% ({mem_used:.1f}GB / {mem_total:.1f}GB)"
                )
            
            # Update Disk (with detailed GB info)
            if "disk" in stats and stats["disk"] is not None:
                disk_percent = float(stats['disk'])
                disk_used = float(stats.get("disk_used_gb", 0))
                disk_total = float(stats.get("disk_total_gb", 0))
                self.stat_labels["disk"].setText(
                    f"Disk Usage: {disk_percent:.1f}% ({disk_used:.1f}GB / {disk_total:.1f}GB)"
                )
            
            # Update Disk Speed
            if "disk_read_mb_s" in stats and "disk_write_mb_s" in stats:
                read_speed = float(stats.get("disk_read_mb_s", 0))
                write_speed = float(stats.get("disk_write_mb_s", 0))
                self.stat_labels["disk_speed"].setText(
                    f"Read: {read_speed:.2f} MB/s\nWrite: {write_speed:.2f} MB/s"
                )
            
            # Update Process Count
            if "processes" in stats:
                proc_count = int(stats['processes'])
                self.stat_labels["processes"].setText(f"Running Processes: {proc_count}")
            
            # Update Network
            if "network" in stats:
                net_total_mb = float(stats.get('network', 0)) / (1024 * 1024)
                self.stat_labels["network"].setText(
                    f"Network Total: {net_total_mb:.1f}MB"
                )
            if "network_download_mb_s" in stats and "network_upload_mb_s" in stats:
                download = float(stats.get('network_download_mb_s', 0))
                upload = float(stats.get('network_upload_mb_s', 0))
                self.stat_labels["network_speed"].setText(
                    f"↓{download:.2f} MB/s\n↑{upload:.2f} MB/s"
                )
            
            # Update GPU
            gpu_info = stats.get('gpu', {})
            if gpu_info:
                gpu_load = float(gpu_info.get('load', 0))
                gpu_mem = float(gpu_info.get('memory_used_mb', 0))
                gpu_total = float(gpu_info.get('memory_total_mb', 0))
                self.stat_labels["gpu"].setText(
                    f"GPU: {gpu_load:.1f}%\nVRAM: {gpu_mem:.1f}/{gpu_total:.1f} MB"
                )
            
            # Update Top Processes
            processes = self.monitor_service.get_process_list(limit=5)
            for i, proc in enumerate(processes):
                if i < len(self.process_labels):
                    proc_name = proc.get('name', 'Unknown')[:30]  # Limit name length
                    proc_mem = float(proc.get('memory', 0))
                    proc_cpu = float(proc.get('cpu', 0))
                    self.process_labels[i].setText(
                        f"{i+1}. {proc_name}: {proc_mem:.1f}% RAM, {proc_cpu:.1f}% CPU"
                    )
            
        except Exception as e:
            from core.logger import Logger
            Logger.get_logger(__name__).error(f"Error refreshing monitor: {e}")
            self.show_message(f"Monitor refresh failed: {e}", "error")
    
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
