from rich.console import Console
from rich.panel import Panel
from rich.table import Table

from scheduling.models.employee import Employee
from scheduling.models.shift import Shift
from scheduling.models.solution import Solution


def print_solutions(
    solutions: list[Solution],
    employees: list[Employee],
    shifts: list[Shift],
    console: Console | None = None,
) -> None:
    console = console or Console()
    employee_map = {e.id: e for e in employees}
    shift_map = {s.id: s for s in shifts}

    if not solutions:
        console.print(Panel("[red bold]No solutions found![/]", title="Result"))
        return

    console.print(f"\n[bold green]Found {len(solutions)} solution(s)[/]\n")

    for i, solution in enumerate(solutions, 1):
        table = Table(title=f"Solution {i}", show_header=True, header_style="bold cyan")
        table.add_column("Shift", style="white")
        table.add_column("Time", style="dim")
        table.add_column("Assigned To", style="green")
        table.add_column("Abilities", style="yellow")

        for shift_id, employee_id in sorted(solution.assignments.items()):
            shift = shift_map.get(shift_id)
            employee = employee_map.get(employee_id)

            shift_name = shift.name if shift else shift_id
            time_range = (
                f"{shift.start_time.strftime('%H:%M')} - {shift.end_time.strftime('%H:%M')}"
                if shift
                else "?"
            )
            employee_name = employee.name if employee else employee_id
            abilities = ", ".join(employee.abilities) if employee else ""

            table.add_row(shift_name, time_range, employee_name, abilities)

        console.print(table)

        metrics = solution.metrics
        metrics_table = Table(show_header=False, box=None, padding=(0, 2))
        metrics_table.add_column("Metric", style="bold")
        metrics_table.add_column("Value")

        score_color = "green" if metrics.soft_preference_score > 0 else "white"
        metrics_table.add_row(
            "Soft Preference Score", f"[{score_color}]{metrics.soft_preference_score}[/]"
        )

        fairness_color = "green" if metrics.fairness_score == 0 else "yellow"
        metrics_table.add_row(
            "Fairness Score", f"[{fairness_color}]{metrics.fairness_score:.2f}[/]"
        )

        metrics_table.add_row("Total Shifts Assigned", str(metrics.total_shifts_assigned))

        if metrics.preferences_satisfied:
            prefs = ", ".join(f"{k}: {v}" for k, v in metrics.preferences_satisfied.items())
            metrics_table.add_row("Preferences Satisfied", prefs)

        console.print(Panel(metrics_table, title="Metrics", border_style="blue"))
        console.print()


def print_input_summary(
    employees: list[Employee],
    shifts: list[Shift],
    console: Console | None = None,
) -> None:
    console = console or Console()

    emp_table = Table(title="Employees", show_header=True, header_style="bold magenta")
    emp_table.add_column("ID", style="dim")
    emp_table.add_column("Name", style="white")
    emp_table.add_column("Abilities", style="yellow")
    emp_table.add_column("Preferences", style="cyan")

    for emp in employees:
        prefs = []
        for p in emp.preferences:
            pref_str = p.type
            if p.is_hard:
                pref_str += " [red](hard)[/]"
            prefs.append(pref_str)

        emp_table.add_row(
            str(emp.id),
            emp.name,
            ", ".join(emp.abilities) or "-",
            ", ".join(prefs) or "-",
        )

    console.print(emp_table)
    console.print()

    shift_table = Table(title="Shifts", show_header=True, header_style="bold magenta")
    shift_table.add_column("ID", style="dim")
    shift_table.add_column("Name", style="white")
    shift_table.add_column("Date", style="cyan")
    shift_table.add_column("Time", style="cyan")
    shift_table.add_column("Required Abilities", style="yellow")

    for shift in shifts:
        shift_table.add_row(
            str(shift.id),
            shift.name,
            shift.start_time.strftime("%Y-%m-%d"),
            f"{shift.start_time.strftime('%H:%M')} - {shift.end_time.strftime('%H:%M')}",
            ", ".join(shift.required_abilities) or "-",
        )

    console.print(shift_table)
    console.print()
