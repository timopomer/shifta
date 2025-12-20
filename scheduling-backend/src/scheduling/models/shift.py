from datetime import datetime

from pydantic import BaseModel, ConfigDict, Field, model_validator

from scheduling.types import Ability, ShiftId


class Shift(BaseModel):
    model_config = ConfigDict(frozen=True)

    id: ShiftId
    name: str
    start_time: datetime
    end_time: datetime
    required_abilities: list[Ability] = Field(default_factory=list)

    @model_validator(mode="after")
    def validate_times(self) -> "Shift":
        if self.end_time <= self.start_time:
            raise ValueError("end_time must be after start_time")
        return self
