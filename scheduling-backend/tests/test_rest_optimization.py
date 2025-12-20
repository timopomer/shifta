"""Tests for rest time optimization between shifts.

When multiple employees can cover shifts, the scheduler should prefer
assignments that maximize rest time between consecutive shifts for each employee.
"""

from datetime import datetime

from scheduling.models.employee import Employee
from scheduling.models.shift import Shift
from scheduling.solver.scheduler import Scheduler


class TestRestOptimization:
    def test_prefers_more_rest_between_late_night_and_next_day_shifts(self):
        """When assigning shifts, prefer giving employees more rest time.
        
        Scenario: Two employees, two days of shifts.
        Day 1: Late shift ending at 2am (technically Day 2)
        Day 2: Morning shift at 10am, Evening shift at 6pm
        
        If Alice works the late shift (ends 2am), she should get the evening shift (6pm)
        rather than the morning shift (10am) to maximize rest (16h vs 8h).
        """
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="day1_late",
                name="Day 1 Late Night",
                start_time=datetime(2024, 12, 25, 20, 0),
                end_time=datetime(2024, 12, 26, 2, 0),  # Ends 2am on Dec 26
                required_abilities=["waiter"],
            ),
            Shift(
                id="day2_morning",
                name="Day 2 Morning",
                start_time=datetime(2024, 12, 26, 10, 0),  # 8 hours after late shift
                end_time=datetime(2024, 12, 26, 16, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="day2_evening",
                name="Day 2 Evening",
                start_time=datetime(2024, 12, 26, 18, 0),  # 16 hours after late shift
                end_time=datetime(2024, 12, 26, 23, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        # The best solution should maximize rest time
        best_solution = solutions[0]

        # If alice works day1_late, she should get day2_evening (more rest)
        if best_solution.assignments["day1_late"] == "alice":
            assert best_solution.assignments["day2_evening"] == "alice"
            assert best_solution.assignments["day2_morning"] == "bob"
        else:
            # If bob works day1_late, he should get day2_evening
            assert best_solution.assignments["day1_late"] == "bob"
            assert best_solution.assignments["day2_evening"] == "bob"
            assert best_solution.assignments["day2_morning"] == "alice"

    def test_rest_optimization_with_single_employee(self):
        """Single employee should still get valid assignments even with tight schedule."""
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
        ]
        shifts = [
            Shift(
                id="late_night",
                name="Late Night",
                start_time=datetime(2024, 12, 25, 22, 0),
                end_time=datetime(2024, 12, 26, 2, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="next_morning",
                name="Next Morning",
                start_time=datetime(2024, 12, 26, 10, 0),
                end_time=datetime(2024, 12, 26, 14, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()

        # Should still work - no minimum rest constraint
        assert len(solutions) == 1
        assert solutions[0].assignments["late_night"] == "alice"
        assert solutions[0].assignments["next_morning"] == "alice"

    def test_rest_optimization_multiple_employees_complex(self):
        """Complex scenario with multiple employees and overlapping possibilities.
        
        The scheduler should prefer assignments that give everyone more rest.
        """
        employees = [
            Employee(id="alice", name="Alice", abilities=["waiter"]),
            Employee(id="bob", name="Bob", abilities=["waiter"]),
            Employee(id="carol", name="Carol", abilities=["waiter"]),
        ]
        shifts = [
            # Day 1 shifts
            Shift(
                id="d1_morning",
                name="Day 1 Morning",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="d1_late",
                name="Day 1 Late",
                start_time=datetime(2024, 12, 25, 22, 0),
                end_time=datetime(2024, 12, 26, 2, 0),
                required_abilities=["waiter"],
            ),
            # Day 2 shifts  
            Shift(
                id="d2_morning",
                name="Day 2 Morning",
                start_time=datetime(2024, 12, 26, 8, 0),
                end_time=datetime(2024, 12, 26, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="d2_evening",
                name="Day 2 Evening",
                start_time=datetime(2024, 12, 26, 18, 0),
                end_time=datetime(2024, 12, 26, 23, 0),
                required_abilities=["waiter"],
            ),
        ]

        solutions = Scheduler(employees=employees, shifts=shifts).solve()
        best_solution = solutions[0]

        # The person who worked d1_late should NOT work d2_morning
        late_worker = best_solution.assignments["d1_late"]
        morning_worker = best_solution.assignments["d2_morning"]
        assert late_worker != morning_worker, \
            f"Employee {late_worker} worked late night and early morning - should maximize rest"
