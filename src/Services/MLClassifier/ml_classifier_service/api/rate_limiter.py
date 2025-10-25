"""Shared rate limiter instance for the API."""

from slowapi import Limiter
from slowapi.util import get_remote_address

# Initialize rate limiter (100 requests per minute per IP)
# This is a shared instance used across all classification endpoints
limiter = Limiter(key_func=get_remote_address, default_limits=["100/minute"])
