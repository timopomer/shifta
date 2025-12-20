from datetime import datetime
from typing import Literal

from pydantic import model_validator

from scheduling.models.preferences.base import BasePreference


class UnavailablePeriodPreference(BasePreference):
    type: Literal["unavailable_period"] = "unavailable_period"
    start: datetime
    end: datetime
    is_hard: bool = True

    @model_validator(mode="after")
    def validate_times(self) -> "UnavailablePeriodPreference":
        if self.end <= self.start:
            raise ValueError("end must be after start")
        return self

    def overlaps_with(self, shift_start: datetime, shift_end: datetime) -> bool:
        return self.start < shift_end and self.end > shift_start
