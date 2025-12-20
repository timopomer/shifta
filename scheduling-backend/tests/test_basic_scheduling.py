from datetime import datetime

from scheduling.models.employee import Employee
from scheduling.models.shift import Shift
from scheduling.solver.scheduler import Scheduler


class TestBasicScheduling:
    def test_two_employees_two_shifts(self):
        employees = [
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

        assert len(solutions) >= 1
        for solution in solutions:
            assert len(solution.assignments) == 2
            assert "morning" in solution.assignments
            assert "evening" in solution.assignments

    def test_two_employees_same_abilities(self):
        """Two employees with the same abilities can each be assigned to a shift.
        
        With rest optimization, the solver prefers assigning different employees
        to back-to-back shifts to maximize rest time. The returned solutions are
        optimal (no single employee works both back-to-back shifts).
        """
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

        # At least one solution should be found
        assert len(solutions) >= 1

        # All returned solutions should be optimal (different employees on back-to-back shifts)
        for solution in solutions:
            assert solution.assignments["shift1"] != solution.assignments["shift2"], \
                "Optimal solution should not have same employee on back-to-back shifts"

    def test_single_employee_single_shift(self):
        employees = [Employee(id="alice", name="Alice", abilities=["waiter"])]
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
        assert solutions[0].assignments == {"shift1": "alice"}

    def test_empty_employees_returns_no_solutions(self):
        shifts = [
            Shift(
                id="shift1",
                name="Shift 1",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=[],
            ),
        ]

        solutions = Scheduler(employees=[], shifts=shifts).solve()

        assert len(solutions) == 0

    def test_empty_shifts_returns_empty_assignment(self):
        employees = [Employee(id="alice", name="Alice", abilities=["waiter"])]

        solutions = Scheduler(employees=employees, shifts=[]).solve()

        assert len(solutions) == 1
        assert solutions[0].assignments == {}

    def test_shift_requires_no_abilities(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=[]),
            Employee(id="bob", name="Bob", abilities=["bartender"]),
        ]
        shifts = [
            Shift(
                id="shift1",
                name="Shift 1",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=[],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 2
        assigned_employees = {s.assignments["shift1"] for s in solutions}
        assert assigned_employees == {"alice", "bob"}


class TestOverlappingShifts:
    def test_single_employee_cannot_work_overlapping_shifts(self):
        """A single employee cannot be assigned to two shifts that overlap in time."""
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="shift1",
                name="Morning Shift",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="shift2",
                name="Overlapping Shift",
                start_time=datetime(2024, 12, 25, 12, 0),  # Starts before shift1 ends
                end_time=datetime(2024, 12, 25, 18, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        # No solution should exist - one employee cannot work two overlapping shifts
        assert len(solutions) == 0


class TestAbilityConstraints:
    def test_employee_without_ability_cannot_work_shift(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["bartender", "waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="bar_shift",
                name="Bar Shift",
                start_time=datetime(2024, 12, 25, 20, 0),
                end_time=datetime(2024, 12, 26, 2, 0),
                required_abilities=["bartender"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 1
        assert solutions[0].assignments["bar_shift"] == "alice"

    def test_multiple_required_abilities(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["bartender"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
            Employee(id="carol", name="Carol", abilities=["bartender", "waiter"]),
        ]
        shifts = [
            Shift(
                id="complex_shift",
                name="Complex Shift",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["bartender", "waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 1
        assert solutions[0].assignments["complex_shift"] == "carol"

    def test_no_employee_has_required_abilities(self):
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="bar_shift",
                name="Bar Shift",
                start_time=datetime(2024, 12, 25, 20, 0),
                end_time=datetime(2024, 12, 26, 2, 0),
                required_abilities=["bartender"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        assert len(solutions) == 0
