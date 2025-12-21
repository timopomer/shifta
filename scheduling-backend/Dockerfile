# syntax=docker/dockerfile:1

FROM python:3.12-slim AS base
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
WORKDIR /app
COPY pyproject.toml .
RUN uv pip install --system --no-cache -r pyproject.toml
COPY src/ src/
RUN uv pip install --system --no-cache --no-deps .

FROM base AS test
RUN uv pip install --system --no-cache pytest pytest-cov ruff httpx
COPY tests/ tests/
CMD ["sh", "-c", "ruff check src tests && pytest -v --cov=scheduling --cov-report=term-missing"]

FROM base AS production
RUN useradd --create-home --shell /bin/bash appuser
USER appuser
EXPOSE 8000
CMD ["uvicorn", "scheduling.api.app:app", "--host", "0.0.0.0", "--port", "8000"]
