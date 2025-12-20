import statistics
from collections import defaultdict

from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.preferences.unavailable import UnavailablePeriodPreference
from scheduling.models.shift import Shift
from scheduling.models.solution import SolutionMetrics
from scheduling.types import EmployeeId, ShiftId


def compute_metrics(
    assignments: dict[ShiftId, EmployeeId],
    employees: list[Employee],
    shifts: list[Shift],
) -> SolutionMetrics:
    shift_map = {s.id: s for s in shifts}

    shifts_per_employee: dict[EmployeeId, int] = defaultdict(int)
    for employee_id in assignments.values():
        shifts_per_employee[employee_id] += 1

    all_shift_counts = [shifts_per_employee.get(e.id, 0) for e in employees]

    fairness_score = statistics.stdev(all_shift_counts) if len(all_shift_counts) > 1 else 0.0

    preferences_satisfied: dict[str, int] = defaultdict(int)
    soft_preference_score = 0

    for employee in employees:
        for pref in employee.preferences:
            if pref.is_hard:
                continue

            if _is_preference_satisfied(pref, employee.id, assignments, shift_map):
                preferences_satisfied[pref.type] += 1
                soft_preference_score += 1

    return SolutionMetrics(
        soft_preference_score=soft_preference_score,
        fairness_score=fairness_score,
        preferences_satisfied=dict(preferences_satisfied),
        total_shifts_assigned=len(assignments),
    )


def _is_preference_satisfied(
    pref,
    employee_id: EmployeeId,
    assignments: dict[ShiftId, EmployeeId],
    shift_map: dict[ShiftId, Shift],
) -> bool:
    if isinstance(pref, PreferShiftPreference):
        return assignments.get(pref.shift_id) == employee_id

    if isinstance(pref, PreferPeriodPreference):
        for shift_id, assigned_employee in assignments.items():
            if assigned_employee == employee_id:
                shift = shift_map.get(shift_id)
                if shift and pref.overlaps_with(shift.start_time, shift.end_time):
                    return True
        return False

    if isinstance(pref, UnavailablePeriodPreference):
        for shift_id, assigned_employee in assignments.items():
            if assigned_employee == employee_id:
                shift = shift_map.get(shift_id)
                if shift and pref.overlaps_with(shift.start_time, shift.end_time):
                    return False
        return True

    return False
