from pydantic import BaseModel, ConfigDict, Field

from scheduling.models.preferences.base import Preference
from scheduling.types import Ability, EmployeeId


class Employee(BaseModel):
    model_config = ConfigDict(frozen=True)

    id: EmployeeId
    name: str
    abilities: list[Ability] = Field(default_factory=list)
    preferences: list[Preference] = Field(default_factory=list)

    def has_abilities(self, required: list[Ability]) -> bool:
        return all(ability in self.abilities for ability in required)
