"""FastAPI application setup."""

from fastapi import FastAPI

from scheduling.api.routes import router

app = FastAPI(
    title="Shift Scheduling Optimizer",
    description="Stateless optimization service for shift scheduling using OR-Tools",
    version="1.0.0",
)

app.include_router(router)
