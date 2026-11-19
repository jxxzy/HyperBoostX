"""
Legacy Python UI entrypoint stub for HyperBoostX.
This file is kept for compatibility and redirects to app/dev_client.py.
"""

import warnings
from dev_client import main

warnings.warn(
    "app/main.py is legacy. Use app/dev_client.py for the Python UI entrypoint.",
    DeprecationWarning,
)

if __name__ == "__main__":
    main()
