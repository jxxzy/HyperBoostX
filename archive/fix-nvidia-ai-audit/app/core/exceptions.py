"""Core exceptions used across HyperBoost X."""

class HyperBoostError(Exception):
    """Base exception for HyperBoost errors."""


class ServiceError(HyperBoostError):
    """Raised when a service operation fails."""
    def __init__(self, message: str, code: int = 500):
        super().__init__(message)
        self.code = code
