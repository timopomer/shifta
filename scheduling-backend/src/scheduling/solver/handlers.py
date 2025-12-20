from ortools.sat.python import cp_model

from scheduling.models.employee import Employee
from scheduling.models.preferences.base import BasePreference
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.preferences.unavailable import UnavailablePeriodPreference
from scheduling.models.shift import Shift
from scheduling.types import EmployeeId, ShiftId


def apply_preference(
    pref: BasePreference,
    employee: Employee,
    shifts: list[Shift],
    assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    model: cp_model.CpModel,
) -> list[cp_model.IntVar]:
    if isinstance(pref, UnavailablePeriodPreference):
        return _handle_unavailable_period(pref, employee, shifts, assign_vars, model)
    if isinstance(pref, PreferShiftPreference):
        return _handle_prefer_shift(pref, employee, assign_vars, model)
    if isinstance(pref, PreferPeriodPreference):
        return _handle_prefer_period(pref, employee, shifts, assign_vars, model)
    return []


def _handle_unavailable_period(
    pref: UnavailablePeriodPreference,
    employee: Employee,
    shifts: list[Shift],
    assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    model: cp_model.CpModel,
) -> list[cp_model.IntVar]:
    overlapping_shifts = [s for s in shifts if pref.overlaps_with(s.start_time, s.end_time)]

    if pref.is_hard:
        for shift in overlapping_shifts:
            key = (employee.id, shift.id)
            if key in assign_vars:
                model.Add(assign_vars[key] == 0)
        return []

    if not overlapping_shifts:
        return []

    overlap_vars = [
        assign_vars[(employee.id, s.id)]
        for s in overlapping_shifts
        if (employee.id, s.id) in assign_vars
    ]

    if not overlap_vars:
        return []

    indicator = model.NewBoolVar(f"unavail_soft_{employee.id}_{pref.start.isoformat()}")
    model.Add(sum(overlap_vars) == 0).OnlyEnforceIf(indicator)
    model.Add(sum(overlap_vars) > 0).OnlyEnforceIf(indicator.Not())

    return [indicator]


def _handle_prefer_shift(
    pref: PreferShiftPreference,
    employee: Employee,
    assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    model: cp_model.CpModel,
) -> list[cp_model.IntVar]:
    key = (employee.id, pref.shift_id)

    if key not in assign_vars:
        return []

    if pref.is_hard:
        model.Add(assign_vars[key] == 1)
        return []

    return [assign_vars[key]]


def _handle_prefer_period(
    pref: PreferPeriodPreference,
    employee: Employee,
    shifts: list[Shift],
    assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    model: cp_model.CpModel,
) -> list[cp_model.IntVar]:
    """Handle preference for working during a specific time period."""
    period_shifts = [s for s in shifts if pref.overlaps_with(s.start_time, s.end_time)]

    if not period_shifts:
        return []

    period_vars = [
        assign_vars[(employee.id, s.id)]
        for s in period_shifts
        if (employee.id, s.id) in assign_vars
    ]

    if not period_vars:
        return []

    if pref.is_hard:
        # Hard preference: must work at least one shift in this period
        model.Add(sum(period_vars) >= 1)
        return []

    # Soft preference: indicator is 1 if assigned to at least one shift in period
    indicator = model.NewBoolVar(f"prefer_period_soft_{employee.id}_{pref.start.isoformat()}")
    model.Add(sum(period_vars) >= 1).OnlyEnforceIf(indicator)
    model.Add(sum(period_vars) == 0).OnlyEnforceIf(indicator.Not())

    return [indicator]
