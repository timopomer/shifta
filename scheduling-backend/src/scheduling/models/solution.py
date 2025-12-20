from pydantic import BaseModel, Field

from scheduling.types import EmployeeId, PreferenceType, ShiftId


class SolutionMetrics(BaseModel):
    soft_preference_score: int = 0
    fairness_score: float = 0.0
    preferences_satisfied: dict[PreferenceType, int] = Field(default_factory=dict)
    total_shifts_assigned: int = 0


class Solution(BaseModel):
    assignments: dict[ShiftId, EmployeeId]
    metrics: SolutionMetrics = Field(default_factory=SolutionMetrics)
