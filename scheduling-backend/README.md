# Bar Employee Scheduling Backend

A constraint-based scheduling system for bar employees using Google OR-Tools CP-SAT solver.

## Features

- **Constraint-based scheduling**: Assigns employees to shifts respecting hard constraints
- **Soft preference optimization**: Maximizes satisfaction of employee preferences
- **Extensible preference system**: Easy to add new preference types
- **Solution iteration**: Browse all valid solutions with metrics

## Installation

```bash
cd scheduling-backend
pip install -e ".[dev]"
```

## Usage

```python
from datetime import datetime
from scheduling import Employee, Shift, Scheduler
from scheduling.models.preferences import (
    UnavailablePeriodPreference,
    PreferShiftPreference,
)

# Create employees with abilities and preferences
alice = Employee(
    id="alice",
    name="Alice",
    abilities=["bartender", "waiter"],
    preferences=[
        PreferShiftPreference(shift_id="evening_shift", is_hard=False)
    ]
)

bob = Employee(
    id="bob",
    name="Bob",
    abilities=["waiter"],
    preferences=[
        UnavailablePeriodPreference(
            start=datetime(2024, 12, 25, 0, 0),
            end=datetime(2024, 12, 25, 23, 59),
            is_hard=True  # Hard constraint - must be respected
        )
    ]
)

# Create shifts with requirements
morning = Shift(
    id="morning_shift",
    name="Morning Shift",
    start_time=datetime(2024, 12, 25, 8, 0),
    end_time=datetime(2024, 12, 25, 14, 0),
    required_abilities=["waiter"]
)

evening = Shift(
    id="evening_shift",
    name="Evening Shift",
    start_time=datetime(2024, 12, 25, 18, 0),
    end_time=datetime(2024, 12, 26, 0, 0),
    required_abilities=["bartender"]
)

# Solve
scheduler = Scheduler(employees=[alice, bob], shifts=[morning, evening])
solutions = scheduler.solve()

for solution in solutions:
    print(f"Assignments: {solution.assignments}")
    print(f"Soft preference score: {solution.metrics.soft_preference_score}")
    print(f"Fairness score: {solution.metrics.fairness_score}")
```

## Running Tests

```bash
pytest
```

## Preference Types

### UnavailablePeriodPreference
Employee cannot work during a time range. Typically used as a hard constraint.

### PreferShiftPreference
Employee prefers to work a specific shift by ID. Typically soft.

### PreferPeriodPreference
Employee prefers working during a specific time period. Typically soft.

## Adding New Preference Types

1. Create a new class inheriting from `BasePreference`
2. Set a unique `type` literal
3. Add handler logic in `solver/handlers.py`
