"""DTOs for the optimization API.

These are explicit transfer objects for communication with external services.
They are intentionally separate from internal domain models.
"""

from datetime import datetime
from typing import Literal

from pydantic import BaseModel, Field


# Request DTOs


class PreferShiftDto(BaseModel):
    """Preference for a specific shift."""

    type: Literal["prefer_shift"] = "prefer_shift"
    shift_id: str
    is_hard: bool = False


class PreferPeriodDto(BaseModel):
    """Preference for working during a specific time period."""

    type: Literal["prefer_period"] = "prefer_period"
    start: datetime
    end: datetime
    is_hard: bool = False


class UnavailablePeriodDto(BaseModel):
    """Unavailability during a specific time period."""

    type: Literal["unavailable_period"] = "unavailable_period"
    start: datetime
    end: datetime
    is_hard: bool = True


PreferenceDto = PreferShiftDto | PreferPeriodDto | UnavailablePeriodDto


class EmployeeDto(BaseModel):
    """Employee data for optimization."""

    id: str
    name: str
    abilities: list[str] = Field(default_factory=list)
    preferences: list[PreferenceDto] = Field(default_factory=list)


class ShiftDto(BaseModel):
    """Shift data for optimization."""

    id: str
    name: str
    start_time: datetime
    end_time: datetime
    required_abilities: list[str] = Field(default_factory=list)


class OptimizeRequest(BaseModel):
    """Request to optimize a schedule."""

    employees: list[EmployeeDto]
    shifts: list[ShiftDto]
    max_solutions: int = Field(default=1, ge=1, le=100)


# Response DTOs


class SolutionMetricsDto(BaseModel):
    """Metrics for a solution."""

    soft_preference_score: int = 0
    fairness_score: float = 0.0
    preferences_satisfied: dict[str, int] = Field(default_factory=dict)
    total_shifts_assigned: int = 0


class SolutionDto(BaseModel):
    """A single solution from the optimizer."""

    assignments: dict[str, str]  # shift_id -> employee_id
    metrics: SolutionMetricsDto


class OptimizeResponse(BaseModel):
    """Response from the optimization endpoint."""

    success: bool
    solutions: list[SolutionDto] = Field(default_factory=list)
    error: str | None = None
