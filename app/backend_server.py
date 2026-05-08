"""
Hybrid Backend Service for HyperBoost X
Acts as a bridge between C# WPF frontend and Python services via REST API
Refactored with Flask blueprints for better organization and maintainability
"""

import json
import threading
from typing import Dict, Any
from urllib.parse import urlparse
from flask import Flask, request
from core.config import Config
from core.logger import Logger

# Import blueprints
from api.health import health_bp
from api.system_info import system_bp
from api.monitoring import monitoring_bp
from api.tweaks import tweaks_bp
from api.booster import booster_bp
from api.drivers import drivers_bp
from api.repair import repair_bp
from api.network import network_bp
from api.startup import startup_bp
from api.websocket import ws_bp
from api.middleware import APIMiddleware

Config.initialize()
Logger.initialize()
logger = Logger.get_logger(__name__)

ALLOWED_CORS_HOSTS = {"127.0.0.1", "localhost"}


class HyperBoostBackendServer:
    """Backend API server for HyperBoost X with blueprint architecture."""
    
    def __init__(self, host: str = "127.0.0.1", port: int = 5000):
        self.app = Flask(__name__)
        self.host = host
        self.port = port
        self.running = False
        
        # Initialize logger
        self.logger = Logger.get_logger(__name__)
        
        # Configure Flask
        self._configure_app()
        
        # Register blueprints
        self._register_blueprints()
        
        self.logger.info("HyperBoost Backend Server initialized with blueprint architecture")
    
    def _configure_app(self):
        """Configure Flask application settings."""
        self.app.config['JSON_SORT_KEYS'] = False
        self.app.config['JSONIFY_PRETTYPRINT_REGULAR'] = True
        
        # Initialize middleware
        APIMiddleware.init_app(self.app)
        
        # Add CORS headers for cross-origin requests (useful for web clients)
        @self.app.after_request
        def add_cors_headers(response):
            origin = request.headers.get('Origin', '')
            if self._is_allowed_cors_origin(origin):
                response.headers['Access-Control-Allow-Origin'] = origin
                response.headers['Vary'] = 'Origin'
                response.headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE, OPTIONS'
                response.headers['Access-Control-Allow-Headers'] = 'Content-Type, Authorization'
            return response

    @staticmethod
    def _is_allowed_cors_origin(origin: str) -> bool:
        if not origin:
            return False

        parsed = urlparse(origin)
        return parsed.scheme in {"http", "https"} and parsed.hostname in ALLOWED_CORS_HOSTS
    
    def _register_blueprints(self):
        """Register API blueprints."""
        self.app.register_blueprint(health_bp)
        self.app.register_blueprint(system_bp)
        self.app.register_blueprint(monitoring_bp)
        self.app.register_blueprint(tweaks_bp)
        self.app.register_blueprint(booster_bp)
        self.app.register_blueprint(drivers_bp)
        self.app.register_blueprint(repair_bp)
        self.app.register_blueprint(network_bp)
        self.app.register_blueprint(startup_bp)
        self.app.register_blueprint(ws_bp)
        
        self.logger.info("API blueprints registered successfully")
    
    def start(self):
        """Start the Flask server in a background thread."""
        if self.running:
            self.logger.warning("Server is already running")
            return
        
        def run_server():
            try:
                self.logger.info(f"Starting HyperBoost Backend Server on {self.host}:{self.port}")
                self.app.run(host=self.host, port=self.port, debug=False, threaded=True)
            except Exception as e:
                self.logger.error(f"Failed to start server: {e}")
        
        server_thread = threading.Thread(target=run_server, daemon=True)
        server_thread.start()
        self.running = True
        logger.info("Backend server started in background thread")
    
    def stop(self):
        """Stop the server (shutdown signal will be sent)."""
        if not self.running:
            logger.warning("Server is not running")
            return
        
        # Flask doesn't have a direct stop method, but we can set a flag
        # In a production setup, you'd use a more sophisticated shutdown mechanism
        self.running = False
        self.logger.info("Backend server stop signal sent")
    
    def is_running(self) -> bool:
        """Check if the server is running."""
        return self.running


def main():
    """Run the backend server standalone."""
    server = HyperBoostBackendServer()
    
    # Run Flask app directly (blocking)
    try:
        server.logger.info(f"Starting HyperBoost Backend Server on {server.host}:{server.port}")
        server.app.run(host=server.host, port=server.port, debug=False, threaded=True)
    except KeyboardInterrupt:
        server.logger.info("Server stopped by user")
    except Exception as e:
        server.logger.error(f"Server error: {e}")


if __name__ == "__main__":
    main()
