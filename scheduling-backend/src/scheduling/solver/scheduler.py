from ortools.sat.python import cp_model

from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.shift import Shift
from scheduling.models.solution import Solution
from scheduling.solver.handlers import apply_preference
from scheduling.solver.metrics import compute_metrics
from scheduling.types import EmployeeId, ShiftId


class SolutionCollector(cp_model.CpSolverSolutionCallback):
    def __init__(
        self,
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
        employees: list[Employee],
        shifts: list[Shift],
        objective_var: cp_model.IntVar | None,
        max_solutions: int = 0,
    ):
        super().__init__()
        self._assign_vars = assign_vars
        self._employees = employees
        self._shifts = shifts
        self._objective_var = objective_var
        self._max_solutions = max_solutions
        self._solutions: list[tuple[Solution, int]] = []  # (solution, objective_value)

    def on_solution_callback(self):
        assignments: dict[ShiftId, EmployeeId] = {}
        for (employee_id, shift_id), var in self._assign_vars.items():
            if self.Value(var) == 1:
                assignments[shift_id] = employee_id

        # compute_metrics calculates soft_preference_score as the count of satisfied
        # preferences, which is what we want to display. The internal objective value
        # includes weighted preferences and rest penalties for optimization purposes.
        metrics = compute_metrics(assignments, self._employees, self._shifts)
        solution = Solution(assignments=assignments, metrics=metrics)

        # Store objective value for sorting (higher is better)
        obj_value = self.Value(self._objective_var) if self._objective_var is not None else 0
        self._solutions.append((solution, obj_value))

        if self._max_solutions > 0 and len(self._solutions) >= self._max_solutions:
            self.StopSearch()

    @property
    def solutions(self) -> list[Solution]:
        # Sort by objective value (higher is better), which accounts for both
        # satisfied preferences and rest optimization
        return [
            sol
            for sol, _ in sorted(self._solutions, key=lambda x: x[1], reverse=True)
        ]


class Scheduler:
    def __init__(self, employees: list[Employee], shifts: list[Shift]):
        self._validate_unique_ids(employees, shifts)
        self._validate_shift_preferences(employees, shifts)
        self.employees = employees
        self.shifts = shifts

    @staticmethod
    def _validate_unique_ids(employees: list[Employee], shifts: list[Shift]) -> None:
        """Ensure all employee and shift IDs are unique."""
        employee_ids = [e.id for e in employees]
        if len(employee_ids) != len(set(employee_ids)):
            seen = set()
            for eid in employee_ids:
                if eid in seen:
                    raise ValueError(f"Duplicate employee id: '{eid}'")
                seen.add(eid)

        shift_ids = [s.id for s in shifts]
        if len(shift_ids) != len(set(shift_ids)):
            seen = set()
            for sid in shift_ids:
                if sid in seen:
                    raise ValueError(f"Duplicate shift id: '{sid}'")
                seen.add(sid)

    @staticmethod
    def _validate_shift_preferences(
        employees: list[Employee], shifts: list[Shift]
    ) -> None:
        """Ensure all PreferShiftPreference references existing shifts."""
        shift_ids = {s.id for s in shifts}
        for employee in employees:
            for pref in employee.preferences:
                if isinstance(pref, PreferShiftPreference) and pref.shift_id not in shift_ids:
                    raise ValueError(
                        f"Employee '{employee.id}' has preference for "
                        f"shift_id '{pref.shift_id}' does not exist"
                    )

    @staticmethod
    def _shifts_overlap(shift1: Shift, shift2: Shift) -> bool:
        """Check if two shifts overlap in time."""
        return shift1.start_time < shift2.end_time and shift2.start_time < shift1.end_time

    def _create_assignment_variables(
        self, model: cp_model.CpModel
    ) -> dict[tuple[EmployeeId, ShiftId], cp_model.IntVar]:
        """Create boolean variables for each valid employee-shift assignment.

        Only creates variables for employees who have the required abilities for a shift.
        """
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar] = {}

        for employee in self.employees:
            for shift in self.shifts:
                if employee.has_abilities(shift.required_abilities):
                    var = model.NewBoolVar(f"assign_{employee.id}_{shift.id}")
                    assign_vars[(employee.id, shift.id)] = var

        return assign_vars

    def _add_exactly_one_employee_per_shift_constraint(
        self,
        model: cp_model.CpModel,
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    ) -> None:
        """Ensure each shift is assigned to exactly one employee.

        If no qualified employees exist for a shift, the model becomes infeasible.
        """
        for shift in self.shifts:
            shift_vars = [
                assign_vars[(e.id, shift.id)]
                for e in self.employees
                if (e.id, shift.id) in assign_vars
            ]
            if shift_vars:
                model.Add(sum(shift_vars) == 1)
            else:
                model.Add(0 == 1)

    def _add_no_overlapping_shifts_constraint(
        self,
        model: cp_model.CpModel,
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    ) -> None:
        """Prevent employees from being assigned to overlapping shifts.

        An employee can work at most one shift during any overlapping time period.
        """
        for employee in self.employees:
            for i, shift1 in enumerate(self.shifts):
                for shift2 in self.shifts[i + 1 :]:
                    if self._shifts_overlap(shift1, shift2):
                        var1 = assign_vars.get((employee.id, shift1.id))
                        var2 = assign_vars.get((employee.id, shift2.id))
                        if var1 is not None and var2 is not None:
                            model.Add(var1 + var2 <= 1)

    def _collect_preference_indicators(
        self,
        model: cp_model.CpModel,
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    ) -> list[cp_model.IntVar]:
        """Collect soft preference indicator variables.

        Returns list of boolean variables that are 1 when a preference is satisfied.
        """
        soft_indicators: list[cp_model.IntVar] = []

        for employee in self.employees:
            for pref in employee.preferences:
                indicators = apply_preference(pref, employee, self.shifts, assign_vars, model)
                soft_indicators.extend(indicators)

        return soft_indicators

    def _collect_rest_penalties(
        self,
        model: cp_model.CpModel,
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    ) -> list[tuple[cp_model.IntVar, int]]:
        """Create penalty indicators for short rest between consecutive shifts.

        Returns list of (indicator_var, penalty_weight) tuples where:
        - indicator_var is 1 if employee works both shifts with short rest
        - penalty_weight is proportional to how short the rest is (higher = worse)

        This encourages the solver to avoid scheduling the same employee for
        shifts that are close together (e.g., late night shift ending at 2am
        followed by morning shift at 10am).
        """
        REST_THRESHOLD_HOURS = 12
        penalties: list[tuple[cp_model.IntVar, int]] = []

        # Sort shifts by end time to efficiently find consecutive pairs
        sorted_shifts = sorted(self.shifts, key=lambda s: s.end_time)

        for employee in self.employees:
            for i, shift1 in enumerate(sorted_shifts):
                for shift2 in sorted_shifts[i + 1 :]:
                    # Skip if shifts overlap (already handled by hard constraint)
                    if self._shifts_overlap(shift1, shift2):
                        continue

                    key1 = (employee.id, shift1.id)
                    key2 = (employee.id, shift2.id)
                    if key1 not in assign_vars or key2 not in assign_vars:
                        continue

                    # Calculate rest hours between shifts
                    rest_seconds = (shift2.start_time - shift1.end_time).total_seconds()
                    rest_hours = rest_seconds / 3600

                    if rest_hours >= REST_THRESHOLD_HOURS:
                        continue  # Enough rest, no penalty needed

                    # Create indicator: 1 if employee works BOTH shifts
                    both = model.NewBoolVar(f"both_{employee.id}_{shift1.id}_{shift2.id}")
                    # both = assign_vars[key1] AND assign_vars[key2]
                    model.AddMultiplicationEquality(both, [assign_vars[key1], assign_vars[key2]])

                    # Penalty proportional to how short the rest is
                    # Scale to integers (0-1200 range for 0-12 hours)
                    penalty = int((REST_THRESHOLD_HOURS - rest_hours) * 100)
                    penalties.append((both, penalty))

        return penalties

    def _build_objective(
        self,
        model: cp_model.CpModel,
        assign_vars: dict[tuple[EmployeeId, ShiftId], cp_model.IntVar],
    ) -> cp_model.IntVar | None:
        """Build combined objective from preferences and rest optimization.

        Preferences are weighted heavily (1000 points each) to ensure they
        take priority over rest optimization. Rest penalties are subtracted
        to encourage longer rest periods when multiple valid assignments exist.
        """
        PREFERENCE_WEIGHT = 1000

        pref_indicators = self._collect_preference_indicators(model, assign_vars)
        rest_penalties = self._collect_rest_penalties(model, assign_vars)

        if not pref_indicators and not rest_penalties:
            return None

        # Build weighted sum: +PREFERENCE_WEIGHT for each satisfied pref,
        # -penalty for each short rest pair
        all_vars: list[cp_model.IntVar] = []
        all_coeffs: list[int] = []

        for indicator in pref_indicators:
            all_vars.append(indicator)
            all_coeffs.append(PREFERENCE_WEIGHT)

        for indicator, penalty in rest_penalties:
            all_vars.append(indicator)
            all_coeffs.append(-penalty)  # Negative because it's a penalty

        # Calculate bounds for the objective variable
        max_positive = sum(c for c in all_coeffs if c > 0)
        max_negative = sum(c for c in all_coeffs if c < 0)

        objective_var = model.NewIntVar(max_negative, max_positive, "objective")
        model.Add(objective_var == cp_model.LinearExpr.WeightedSum(all_vars, all_coeffs))
        model.Maximize(objective_var)

        return objective_var

    def solve(self, max_solutions: int = 100) -> list[Solution]:
        model = cp_model.CpModel()

        assign_vars = self._create_assignment_variables(model)

        self._add_exactly_one_employee_per_shift_constraint(model, assign_vars)
        self._add_no_overlapping_shifts_constraint(model, assign_vars)
        objective_var = self._build_objective(model, assign_vars)

        solver = cp_model.CpSolver()
        solver.parameters.enumerate_all_solutions = True
        solver.parameters.max_time_in_seconds = 60.0

        collector = SolutionCollector(
            assign_vars, self.employees, self.shifts, objective_var, max_solutions
        )

        status = solver.Solve(model, collector)

        if status in (cp_model.OPTIMAL, cp_model.FEASIBLE):
            return collector.solutions
        return []
