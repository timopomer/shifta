from __future__ import annotations

from typing import TYPE_CHECKING, Annotated

from pydantic import BaseModel, ConfigDict, Field

from scheduling.types import PreferenceType


class BasePreference(BaseModel):
    model_config = ConfigDict(frozen=True)

    type: PreferenceType
    is_hard: bool = False


if TYPE_CHECKING:
    from scheduling.models.preferences.prefer_period import PreferPeriodPreference
    from scheduling.models.preferences.prefer_shift import PreferShiftPreference
    from scheduling.models.preferences.unavailable import UnavailablePeriodPreference

    Preference = Annotated[
        UnavailablePeriodPreference | PreferShiftPreference | PreferPeriodPreference,
        Field(discriminator="type"),
    ]
else:

    def _get_preference_type():
        from scheduling.models.preferences.prefer_period import PreferPeriodPreference
        from scheduling.models.preferences.prefer_shift import PreferShiftPreference
        from scheduling.models.preferences.unavailable import UnavailablePeriodPreference

        return Annotated[
            UnavailablePeriodPreference | PreferShiftPreference | PreferPeriodPreference,
            Field(discriminator="type"),
        ]

    Preference = _get_preference_type()
