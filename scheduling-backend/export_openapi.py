#!/usr/bin/env python3
"""Export OpenAPI schema to stdout as JSON."""

import json

from scheduling.api.app import app

if __name__ == "__main__":
    print(json.dumps(app.openapi()))
