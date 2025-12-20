from datetime import datetime

from rich.console import Console

from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.preferences.unavailable import UnavailablePeriodPreference
from scheduling.models.shift import Shift
from scheduling.printer import print_input_summary, print_solutions
from scheduling.solver.scheduler import Scheduler


def main():
    console = Console()
    console.print("\n[bold blue]Bar Employee Scheduler[/]\n")

    employees = [
        Employee(
            id="alice",
            name="Alice",
            abilities=["bartender", "waiter"],
            preferences=[PreferShiftPreference(shift_id="evening", is_hard=False)],
        ),
        Employee(
            id="bob",
            name="Bob",
            abilities=["waiter", "kitchen"],
            preferences=[
                PreferPeriodPreference(
                    start=datetime(2024, 12, 29, 0, 0),
                    end=datetime(2024, 12, 30, 0, 0),
                    is_hard=False,
                )
            ],  # Prefers Sunday Dec 29
        ),
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
        Employee(
            id="dave",
            name="Dave",
            abilities=["waiter"],
        ),
    ]

    shifts = [
        Shift(
            id="morning",
            name="Morning Shift",
            start_time=datetime(2024, 12, 25, 8, 0),
            end_time=datetime(2024, 12, 25, 14, 0),
            required_abilities=["waiter"],
        ),
        Shift(
            id="afternoon",
            name="Afternoon Shift",
            start_time=datetime(2024, 12, 25, 14, 0),
            end_time=datetime(2024, 12, 25, 20, 0),
            required_abilities=["waiter"],
        ),
        Shift(
            id="evening",
            name="Evening Shift",
            start_time=datetime(2024, 12, 25, 20, 0),
            end_time=datetime(2024, 12, 26, 2, 0),
            required_abilities=["bartender"],
        ),
        Shift(
            id="sunday_brunch",
            name="Sunday Brunch",
            start_time=datetime(2024, 12, 29, 10, 0),
            end_time=datetime(2024, 12, 29, 15, 0),
            required_abilities=["waiter"],
        ),
    ]

    print_input_summary(employees, shifts, console)

    console.print("[bold]Solving...[/]\n")
    scheduler = Scheduler(employees=employees, shifts=shifts)
    solutions = scheduler.solve(max_solutions=5)

    print_solutions(solutions, employees, shifts, console)


if __name__ == "__main__":
    main()
