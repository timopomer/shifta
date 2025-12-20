from datetime import datetime

from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.shift import Shift
from scheduling.solver.scheduler import Scheduler


class TestSolutionOrdering:
    def test_solutions_ordered_by_soft_preference_score(self):
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
                preferences=[PreferShiftPreference(shift_id="shift2", is_hard=False)],
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

        assert len(solutions) >= 1

        best = solutions[0]
        assert best.metrics.soft_preference_score == 2
        assert best.assignments["shift1"] == "alice"
        assert best.assignments["shift2"] == "bob"

        scores = [s.metrics.soft_preference_score for s in solutions]
        assert scores == sorted(scores, reverse=True)

    def test_all_solutions_accessible(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
            Employee(id="carol", name="Carol", abilities=["waiter"]),
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

        assert len(solutions) == 3
        assigned = {s.assignments["shift1"] for s in solutions}
        assert assigned == {"alice", "bob", "carol"}


class TestFairnessMetrics:
    def test_fairness_score_perfect_distribution(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
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

        fair_solutions = [s for s in solutions if s.metrics.fairness_score == 0.0]
        assert len(fair_solutions) >= 1

        for solution in fair_solutions:
            assert solution.assignments["shift1"] != solution.assignments["shift2"]

    def test_fairness_score_uneven_distribution(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["bartender", "waiter"]),
            Employee(id="bob", name="Bob", abilities=[]),
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
                required_abilities=["bartender"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 1
        assert solutions[0].metrics.fairness_score > 0
        assert solutions[0].metrics.total_shifts_assigned == 2


class TestPreferenceSatisfactionBreakdown:
    def test_preferences_satisfied_by_type(self):
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="shift1", is_hard=False)],
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

        assert len(solutions) == 1
        metrics = solutions[0].metrics
        assert metrics.soft_preference_score == 1
        assert metrics.preferences_satisfied.get("prefer_shift", 0) == 1

    def test_multiple_preference_types_tracked(self):
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[
                    PreferShiftPreference(shift_id="shift1", is_hard=False),
                    PreferPeriodPreference(
                        start=datetime(2024, 12, 25, 0, 0),
                        end=datetime(2024, 12, 26, 0, 0),
                        is_hard=False,
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

        assert len(solutions) == 1
        metrics = solutions[0].metrics
        assert metrics.soft_preference_score == 2
        assert metrics.preferences_satisfied.get("prefer_shift", 0) == 1
        assert metrics.preferences_satisfied.get("prefer_period", 0) == 1


class TestMaxSolutions:
    def test_max_solutions_limit(self):
        employees = [
            Employee(id=f"emp{i}", name=f"Employee {i}", abilities=["waiter"]) for i in range(10)
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

        solutions = Scheduler(employees=employees, shifts=shifts).solve(max_solutions=5)

        assert len(solutions) <= 5
        assert len(solutions) > 0


class TestTotalShiftsAssigned:
    def test_total_shifts_assigned_count(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id=f"shift{i}",
                name=f"Shift {i}",
                start_time=datetime(2024, 12, 25, 8 + i * 6, 0),
                end_time=datetime(2024, 12, 25, 14 + i * 6, 0),
                required_abilities=["waiter"],
            )
            for i in range(2)
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        for solution in solutions:
            assert solution.metrics.total_shifts_assigned == 2
