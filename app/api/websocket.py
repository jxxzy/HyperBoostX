"""
WebSocket API Blueprint for HyperBoost X
Provides real-time updates for system monitoring
"""

import json
import threading
import time
from flask import Blueprint, request
from flask_sock import Sock
from core.logger import Logger
from services.monitoring.monitor_service import MonitorService

logger = Logger.get_logger(__name__)

ws_bp = Blueprint('websocket', __name__, url_prefix='/ws')
sock = Sock(ws_bp)

# Initialize service
monitor_service = MonitorService()

# WebSocket connections
active_connections = set()


@ws_bp.route('/system-stats')
@sock.route
def system_stats_ws(sock):
    """WebSocket endpoint for real-time system statistics."""
    try:
        active_connections.add(sock)
        logger.info(f"WebSocket connection established: {request.remote_addr}")

        while True:
            # Send system stats every second
            stats = monitor_service.get_current_stats()
            processes = monitor_service.get_process_list()[:10]  # Top 10 processes

            data = {
                "timestamp": time.time(),
                "stats": stats,
                "top_processes": processes
            }

            sock.send(json.dumps(data))
            time.sleep(1)  # Update every second

    except Exception as e:
        logger.error(f"WebSocket error: {e}")
    finally:
        active_connections.discard(sock)
        logger.info(f"WebSocket connection closed: {request.remote_addr}")


def broadcast_system_update():
    """Broadcast system updates to all connected WebSocket clients."""
    if not active_connections:
        return

    try:
        stats = monitor_service.get_current_stats()
        data = {
            "timestamp": time.time(),
            "type": "system_update",
            "stats": stats
        }

        message = json.dumps(data)
        disconnected = set()

        for sock in active_connections:
            try:
                sock.send(message)
            except Exception:
                disconnected.add(sock)

        # Remove disconnected clients
        active_connections.difference_update(disconnected)

    except Exception as e:
        logger.error(f"Error broadcasting system update: {e}")


# Start background thread for periodic broadcasts
def start_broadcast_thread():
    """Start background thread for broadcasting system updates."""
    def broadcast_loop():
        while True:
            broadcast_system_update()
            time.sleep(2)  # Broadcast every 2 seconds

    thread = threading.Thread(target=broadcast_loop, daemon=True)
    thread.start()
    logger.info("WebSocket broadcast thread started")


# Initialize broadcast thread when module is imported
start_broadcast_thread()