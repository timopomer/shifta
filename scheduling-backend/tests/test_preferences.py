from datetime import datetime

from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.preferences.unavailable import UnavailablePeriodPreference
from scheduling.models.shift import Shift
from scheduling.solver.scheduler import Scheduler


class TestHardUnavailability:
    def test_employee_with_day_off_not_assigned(self):
        employees = [
            Employee(
                id="carol",
                name="Carol",
                abilities=["bartender", "waiter"],
                preferences=[
                    UnavailablePeriodPreference(
                        start=datetime(2024, 12, 25, 0, 0),
                        end=datetime(2024, 12, 25, 23, 59),
                        is_hard=True,
                    )
                ],
            )
        ]
        shifts = [
            Shift(
                id="morning",
                name="Morning",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 0

    def test_hard_unavailability_with_fallback_employee(self):
        employees = [
            Employee(
                id="carol",
                name="Carol",
                abilities=["bartender", "waiter"],
                preferences=[
                    UnavailablePeriodPreference(
                        start=datetime(2024, 12, 25, 0, 0),
                        end=datetime(2024, 12, 25, 23, 59),
                        is_hard=True,
                    )
                ],
            ),
            Employee(id="alice", name="Alice", abilities=["bartender", "waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="morning",
                name="Morning",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="evening",
                name="Evening",
                start_time=datetime(2024, 12, 25, 18, 0),
                end_time=datetime(2024, 12, 26, 0, 0),
                required_abilities=["bartender"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        for solution in solutions:
            for _shift_id, employee_id in solution.assignments.items():
                assert employee_id != "carol"

    def test_partial_day_unavailability(self):
        employees = [
            Employee(
                id="eve",
                name="Eve",
                abilities=["waiter"],
                preferences=[
                    UnavailablePeriodPreference(
                        start=datetime(2024, 12, 25, 18, 0),
                        end=datetime(2024, 12, 25, 22, 0),
                        is_hard=True,
                    )
                ],
            )
        ]
        shifts = [
            Shift(
                id="morning",
                name="Morning",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="evening",
                name="Evening",
                start_time=datetime(2024, 12, 25, 18, 0),
                end_time=datetime(2024, 12, 25, 23, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 0

    def test_partial_day_unavailability_with_coverage(self):
        employees = [
            Employee(
                id="eve",
                name="Eve",
                abilities=["waiter"],
                preferences=[
                    UnavailablePeriodPreference(
                        start=datetime(2024, 12, 25, 18, 0),
                        end=datetime(2024, 12, 25, 22, 0),
                        is_hard=True,
                    )
                ],
            ),
            Employee(id="frank", name="Frank", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="morning",
                name="Morning",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="evening",
                name="Evening",
                start_time=datetime(2024, 12, 25, 18, 0),
                end_time=datetime(2024, 12, 25, 23, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        for solution in solutions:
            if "evening" in solution.assignments:
                assert solution.assignments["evening"] != "eve"


class TestSoftShiftPreference:
    def test_soft_shift_preference_maximized(self):
        employees = [
            Employee(
                id="dave",
                name="Dave",
                abilities=["bartender", "waiter"],
                preferences=[PreferShiftPreference(shift_id="evening", is_hard=False)],
            ),
            Employee(id="frank", name="Frank", abilities=["bartender", "waiter"]),
        ]
        shifts = [
            Shift(
                id="morning",
                name="Morning",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="evening",
                name="Evening",
                start_time=datetime(2024, 12, 25, 18, 0),
                end_time=datetime(2024, 12, 25, 23, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        best_solution = solutions[0]
        assert best_solution.assignments["evening"] == "dave"
        assert best_solution.metrics.soft_preference_score >= 1

    def test_competing_soft_preferences(self):
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="shift1", is_hard=False)],
            ),
            Employee(
                id="bob",
                name="Bob",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="shift1", is_hard=False)],
            ),
        ]
        shifts = [
            Shift(
                id="shift1",
                name="Shift 1",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="shift2",
                name="Shift 2",
                start_time=datetime(2024, 12, 25, 14, 0),
                end_time=datetime(2024, 12, 25, 20, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        for solution in solutions:
            assert solution.metrics.soft_preference_score == 1

    def test_hard_shift_preference(self):
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="shift1", is_hard=True)],
            ),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="shift1",
                name="Shift 1",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="shift2",
                name="Shift 2",
                start_time=datetime(2024, 12, 25, 14, 0),
                end_time=datetime(2024, 12, 25, 20, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        for solution in solutions:
            assert solution.assignments["shift1"] == "alice"


class TestSoftPeriodPreference:
    def test_soft_period_preference_maximized(self):
        """Employee prefers to work during a specific time period."""
        employees = [
            Employee(
                id="eve",
                name="Eve",
                abilities=["waiter"],
                preferences=[
                    PreferPeriodPreference(
                        start=datetime(2024, 12, 29, 0, 0),
                        end=datetime(2024, 12, 30, 0, 0),
                        is_hard=False,
                    )
                ],
            ),
            Employee(id="frank", name="Frank", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="sunday_shift",
                name="Sunday Shift",
                start_time=datetime(2024, 12, 29, 10, 0),
                end_time=datetime(2024, 12, 29, 18, 0),
                required_abilities=[],
            ),
            Shift(
                id="monday_shift",
                name="Monday Shift",
                start_time=datetime(2024, 12, 30, 10, 0),
                end_time=datetime(2024, 12, 30, 18, 0),
                required_abilities=[],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        best_solution = solutions[0]
        assert best_solution.assignments["sunday_shift"] == "eve"
        assert best_solution.metrics.soft_preference_score >= 1

    def test_period_preference_tracks_in_metrics(self):
        """Period preference satisfaction is tracked in metrics."""
        employees = [
            Employee(
                id="eve",
                name="Eve",
                abilities=["waiter"],
                preferences=[
                    PreferPeriodPreference(
                        start=datetime(2024, 12, 29, 0, 0),
                        end=datetime(2024, 12, 30, 0, 0),
                        is_hard=False,
                    )
                ],
            )
        ]
        shifts = [
            Shift(
                id="sunday_shift",
                name="Sunday Shift",
                start_time=datetime(2024, 12, 29, 10, 0),
                end_time=datetime(2024, 12, 29, 18, 0),
                required_abilities=[],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 1
        assert solutions[0].metrics.preferences_satisfied.get("prefer_period", 0) >= 1


class TestNoSolution:
    def test_impossible_constraints_no_solution(self):
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[
                    PreferShiftPreference(shift_id="shift1", is_hard=True),
                    UnavailablePeriodPreference(
                        start=datetime(2024, 12, 25, 0, 0),
                        end=datetime(2024, 12, 25, 23, 59),
                        is_hard=True,
                    ),
                ],
            )
        ]
        shifts = [
            Shift(
                id="shift1",
                name="Shift 1",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 0

    def test_employee_can_work_multiple_shifts(self):
        employees = [Employee(id="alice", name="Alice", abilities=["waiter"])]
        shifts = [
            Shift(
                id="shift1",
                name="Shift 1",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="shift2",
                name="Shift 2",
                start_time=datetime(2024, 12, 25, 14, 0),
                end_time=datetime(2024, 12, 25, 20, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 1
        assert solutions[0].assignments["shift1"] == "alice"
        assert solutions[0].assignments["shift2"] == "alice"
