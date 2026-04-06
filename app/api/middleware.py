"""
Middleware for HyperBoost X API
Provides request validation, error handling, and security features
"""

import json
from functools import wraps
from flask import request, jsonify
from core.logger import Logger

logger = Logger.get_logger(__name__)


def validate_json(required_fields=None):
    """Decorator to validate JSON requests and required fields."""
    def decorator(f):
        @wraps(f)
        def wrapper(*args, **kwargs):
            if not request.is_json:
                return jsonify({"error": "Request must be JSON"}), 400

            data = request.get_json()
            if required_fields:
                missing = [field for field in required_fields if field not in data]
                if missing:
                    return jsonify({"error": f"Missing required fields: {', '.join(missing)}"}), 400

            return f(*args, **kwargs)
        return wrapper
    return decorator


def handle_errors(f):
    """Decorator to handle exceptions and return proper error responses."""
    @wraps(f)
    def wrapper(*args, **kwargs):
        try:
            return f(*args, **kwargs)
        except ValueError as e:
            logger.warning(f"Validation error in {f.__name__}: {e}")
            return jsonify({"error": str(e)}), 400
        except PermissionError as e:
            logger.warning(f"Permission error in {f.__name__}: {e}")
            return jsonify({"error": "Insufficient permissions. Try running as administrator."}), 403
        except Exception as e:
            logger.error(f"Unexpected error in {f.__name__}: {e}")
            return jsonify({"error": "Internal server error"}), 500
    return wrapper


def log_requests(f):
    """Decorator to log API requests."""
    @wraps(f)
    def wrapper(*args, **kwargs):
        logger.info(f"API Request: {request.method} {request.path} from {request.remote_addr}")
        response = f(*args, **kwargs)
        logger.info(f"API Response: {getattr(response, 'status_code', 'unknown')}")
        return response
    return wrapper


class APIMiddleware:
    """API middleware manager."""

    @staticmethod
    def init_app(app):
        """Initialize middleware for the Flask app."""

        @app.before_request
        def log_request_info():
            """Log incoming requests."""
            logger.debug(f"Request: {request.method} {request.url}")

        @app.after_request
        def add_security_headers(response):
            """Add security headers to responses."""
            response.headers['X-Content-Type-Options'] = 'nosniff'
            response.headers['X-Frame-Options'] = 'DENY'
            response.headers['X-XSS-Protection'] = '1; mode=block'
            return response

        @app.errorhandler(404)
        def not_found(error):
            """Handle 404 errors."""
            return jsonify({"error": "Endpoint not found"}), 404

        @app.errorhandler(405)
        def method_not_allowed(error):
            """Handle 405 errors."""
            return jsonify({"error": "Method not allowed"}), 405

        @app.errorhandler(500)
        def internal_error(error):
            """Handle 500 errors."""
            logger.error(f"Internal server error: {error}")
            return jsonify({"error": "Internal server error"}), 500