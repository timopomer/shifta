from typing import Literal

from scheduling.models.preferences.base import BasePreference
from scheduling.types import ShiftId


class PreferShiftPreference(BasePreference):
    type: Literal["prefer_shift"] = "prefer_shift"
    shift_id: ShiftId
    is_hard: bool = False
