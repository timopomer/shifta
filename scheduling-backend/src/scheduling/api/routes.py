"""API routes for the optimization service."""

from fastapi import APIRouter

from scheduling.api.dto import (
    EmployeeDto,
    OptimizeRequest,
    OptimizeResponse,
    PreferPeriodDto,
    PreferShiftDto,
    ShiftDto,
    SolutionDto,
    SolutionMetricsDto,
    UnavailablePeriodDto,
)
from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.preferences.unavailable import UnavailablePeriodPreference
from scheduling.models.shift import Shift
from scheduling.solver.scheduler import Scheduler
from scheduling.types import Ability, EmployeeId, ShiftId

router = APIRouter(prefix="/api", tags=["optimization"])


def _convert_employee(dto: EmployeeDto) -> Employee:
    """Convert EmployeeDto to internal Employee model."""
    preferences = []
    for pref in dto.preferences:
        if isinstance(pref, PreferShiftDto):
            preferences.append(
                PreferShiftPreference(
                    shift_id=ShiftId(pref.shift_id),
                    is_hard=pref.is_hard,
                )
            )
        elif isinstance(pref, PreferPeriodDto):
            preferences.append(
                PreferPeriodPreference(
                    start=pref.start,
                    end=pref.end,
                    is_hard=pref.is_hard,
                )
            )
        elif isinstance(pref, UnavailablePeriodDto):
            preferences.append(
                UnavailablePeriodPreference(
                    start=pref.start,
                    end=pref.end,
                    is_hard=pref.is_hard,
                )
            )

    return Employee(
        id=EmployeeId(dto.id),
        name=dto.name,
        abilities=[Ability(a) for a in dto.abilities],
        preferences=preferences,
    )


def _convert_shift(dto: ShiftDto) -> Shift:
    """Convert ShiftDto to internal Shift model."""
    return Shift(
        id=ShiftId(dto.id),
        name=dto.name,
        start_time=dto.start_time,
        end_time=dto.end_time,
        required_abilities=[Ability(a) for a in dto.required_abilities],
    )


@router.post("/optimize", response_model=OptimizeResponse)
def optimize(request: OptimizeRequest) -> OptimizeResponse:
    """Run the optimization solver on the provided schedule data.

    This endpoint is stateless - all data must be provided in the request.
    """
    try:
        # Convert DTOs to internal models
        employees = [_convert_employee(e) for e in request.employees]
        shifts = [_convert_shift(s) for s in request.shifts]

        # Run the solver
        scheduler = Scheduler(employees=employees, shifts=shifts)
        solutions = scheduler.solve(max_solutions=request.max_solutions)

        # Convert solutions to DTOs
        solution_dtos = [
            SolutionDto(
                assignments={str(k): str(v) for k, v in sol.assignments.items()},
                metrics=SolutionMetricsDto(
                    soft_preference_score=sol.metrics.soft_preference_score,
                    fairness_score=sol.metrics.fairness_score,
                    preferences_satisfied={
                        str(k): v for k, v in sol.metrics.preferences_satisfied.items()
                    },
                    total_shifts_assigned=sol.metrics.total_shifts_assigned,
                ),
            )
            for sol in solutions
        ]

        return OptimizeResponse(success=True, solutions=solution_dtos)

    except ValueError as e:
        return OptimizeResponse(success=False, error=str(e))
    except Exception as e:
        return OptimizeResponse(success=False, error=f"Optimization failed: {e!s}")


@router.get("/health")
def health() -> dict[str, str]:
    """Health check endpoint."""
    return {"status": "healthy"}
