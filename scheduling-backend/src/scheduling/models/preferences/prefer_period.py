from datetime import datetime
from typing import Literal

from pydantic import model_validator

from scheduling.models.preferences.base import BasePreference


class PreferPeriodPreference(BasePreference):
    """Preference for working during a specific time period.
    
    The employee prefers to be assigned to shifts that overlap with this period.
    """

    type: Literal["prefer_period"] = "prefer_period"
    start: datetime
    end: datetime
    is_hard: bool = False

    @model_validator(mode="after")
    def validate_times(self) -> "PreferPeriodPreference":
        if self.end <= self.start:
            raise ValueError("end must be after start")
        return self

    def overlaps_with(self, shift_start: datetime, shift_end: datetime) -> bool:
        """Check if a shift overlaps with this preferred period."""
        return self.start < shift_end and self.end > shift_start
