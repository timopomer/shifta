"""Tests for input validation.

These tests verify that invalid inputs are rejected at model creation time
or scheduler initialization time, rather than silently producing wrong results.
"""

from datetime import datetime

import pytest

from scheduling.models.employee import Employee
from scheduling.models.preferences.prefer_period import PreferPeriodPreference
from scheduling.models.preferences.prefer_shift import PreferShiftPreference
from scheduling.models.preferences.unavailable import UnavailablePeriodPreference
from scheduling.models.shift import Shift
from scheduling.solver.scheduler import Scheduler


class TestShiftValidation:
    def test_shift_end_before_start_raises_error(self):
        """A shift cannot end before it starts."""
        with pytest.raises(ValueError, match="end_time must be after start_time"):
            Shift(
                id="backwards",
                name="Backwards Shift",
                start_time=datetime(2024, 12, 25, 14, 0),
                end_time=datetime(2024, 12, 25, 8, 0),
                required_abilities=["waiter"],
            )

    def test_shift_end_equals_start_raises_error(self):
        """A shift cannot have zero duration."""
        with pytest.raises(ValueError, match="end_time must be after start_time"):
            Shift(
                id="zero_duration",
                name="Zero Duration Shift",
                start_time=datetime(2024, 12, 25, 14, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            )

    def test_valid_shift_accepted(self):
        """Normal shifts should work fine."""
        shift = Shift(
            id="normal",
            name="Normal Shift",
            start_time=datetime(2024, 12, 25, 8, 0),
            end_time=datetime(2024, 12, 25, 14, 0),
            required_abilities=["waiter"],
        )
        assert shift.start_time < shift.end_time


class TestUnavailabilityValidation:
    def test_unavailability_end_before_start_raises_error(self):
        """Unavailability period cannot end before it starts."""
        with pytest.raises(ValueError, match="end must be after start"):
            UnavailablePeriodPreference(
                start=datetime(2024, 12, 25, 20, 0),
                end=datetime(2024, 12, 25, 8, 0),
                is_hard=True,
            )

    def test_unavailability_end_equals_start_raises_error(self):
        """Unavailability period cannot have zero duration."""
        with pytest.raises(ValueError, match="end must be after start"):
            UnavailablePeriodPreference(
                start=datetime(2024, 12, 25, 14, 0),
                end=datetime(2024, 12, 25, 14, 0),
                is_hard=True,
            )

    def test_valid_unavailability_accepted(self):
        """Normal unavailability periods should work fine."""
        pref = UnavailablePeriodPreference(
            start=datetime(2024, 12, 25, 8, 0),
            end=datetime(2024, 12, 25, 20, 0),
            is_hard=True,
        )
        assert pref.start < pref.end


class TestPreferPeriodValidation:
    def test_prefer_period_end_before_start_raises_error(self):
        """Prefer period cannot end before it starts."""
        with pytest.raises(ValueError, match="end must be after start"):
            PreferPeriodPreference(
                start=datetime(2024, 12, 25, 20, 0),
                end=datetime(2024, 12, 25, 8, 0),
                is_hard=False,
            )

    def test_prefer_period_end_equals_start_raises_error(self):
        """Prefer period cannot have zero duration."""
        with pytest.raises(ValueError, match="end must be after start"):
            PreferPeriodPreference(
                start=datetime(2024, 12, 25, 14, 0),
                end=datetime(2024, 12, 25, 14, 0),
                is_hard=False,
            )

    def test_valid_prefer_period_accepted(self):
        """Normal prefer periods should work fine."""
        pref = PreferPeriodPreference(
            start=datetime(2024, 12, 25, 8, 0),
            end=datetime(2024, 12, 25, 20, 0),
            is_hard=False,
        )
        assert pref.start < pref.end


class TestPreferShiftValidation:
    def test_prefer_nonexistent_shift_hard_raises_error(self):
        """Hard preference for non-existent shift should cause scheduler to fail."""
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="ghost_shift", is_hard=True)],
            )
        ]
        shifts = [
            Shift(
                id="real_shift",
                name="Real Shift",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
        ]

        with pytest.raises(ValueError, match="shift_id 'ghost_shift' does not exist"):
            Scheduler(employees=employees, shifts=shifts)

    def test_prefer_nonexistent_shift_soft_raises_error(self):
        """Soft preference for non-existent shift should also fail - likely a typo."""
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="typo_shift", is_hard=False)],
            )
        ]
        shifts = [
            Shift(
                id="real_shift",
                name="Real Shift",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
        ]

        with pytest.raises(ValueError, match="shift_id 'typo_shift' does not exist"):
            Scheduler(employees=employees, shifts=shifts)

    def test_valid_shift_preference_accepted(self):
        """Preference for existing shift should work."""
        employees = [
            Employee(
                id="alice",
                name="Alice",
                abilities=["waiter"],
                preferences=[PreferShiftPreference(shift_id="real_shift", is_hard=False)],
            )
        ]
        shifts = [
            Shift(
                id="real_shift",
                name="Real Shift",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
        ]

        scheduler = Scheduler(employees=employees, shifts=shifts)
        solutions = scheduler.solve()
        assert len(solutions) == 1


class TestDuplicateIdValidation:
    def test_duplicate_shift_ids_raises_error(self):
        """Two shifts with the same ID should fail."""
        employees = [Employee(id="alice", name="Alice", abilities=["waiter"])]
        shifts = [
            Shift(
                id="same_id",
                name="Morning Shift",
                start_time=datetime(2024, 12, 25, 8, 0),
                end_time=datetime(2024, 12, 25, 14, 0),
                required_abilities=["waiter"],
            ),
            Shift(
                id="same_id",
                name="Evening Shift",
                start_time=datetime(2024, 12, 25, 18, 0),
                end_time=datetime(2024, 12, 25, 22, 0),
                required_abilities=["waiter"],
            ),
        ]

        with pytest.raises(ValueError, match="Duplicate shift id"):
            Scheduler(employees=employees, shifts=shifts)

    def test_duplicate_employee_ids_raises_error(self):
        """Two employees with the same ID should fail."""
        employees = [
            Employee(id="same_id", name="Alice", abilities=["waiter"]),
            Employee(id="same_id", name="Bob", abilities=["waiter"]),
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

        with pytest.raises(ValueError, match="Duplicate employee id"):
            Scheduler(employees=employees, shifts=shifts)

    def test_unique_ids_accepted(self):
        """Unique IDs should work fine."""
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

        scheduler = Scheduler(employees=employees, shifts=shifts)
        solutions = scheduler.solve()
        assert len(solutions) >= 1
