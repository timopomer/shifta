"""Tests for the FastAPI optimization endpoint."""

from datetime import datetime

import pytest
from fastapi.testclient import TestClient

from scheduling.api.app import app


@pytest.fixture
def client():
    return TestClient(app)


class TestHealthEndpoint:
    def test_health_returns_healthy(self, client: TestClient):
        response = client.get("/api/health")
        assert response.status_code == 200
        assert response.json() == {"status": "healthy"}


class TestOptimizeEndpoint:
    def test_optimize_simple_schedule(self, client: TestClient):
        """Test basic optimization with one employee and one shift."""
        request = {
            "employees": [
                {
                    "id": "emp1",
                    "name": "Alice",
                    "abilities": ["bartender"],
                    "preferences": [],
                }
            ],
            "shifts": [
                {
                    "id": "shift1",
                    "name": "Morning",
                    "start_time": "2024-12-25T08:00:00",
                    "end_time": "2024-12-25T14:00:00",
                    "required_abilities": ["bartender"],
                }
            ],
            "max_solutions": 1,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        assert len(data["solutions"]) == 1
        assert data["solutions"][0]["assignments"]["shift1"] == "emp1"

    def test_optimize_with_preferences(self, client: TestClient):
        """Test optimization respects employee preferences."""
        request = {
            "employees": [
                {
                    "id": "alice",
                    "name": "Alice",
                    "abilities": ["bartender", "waiter"],
                    "preferences": [
                        {"type": "prefer_shift", "shift_id": "evening", "is_hard": False}
                    ],
                },
                {
                    "id": "bob",
                    "name": "Bob",
                    "abilities": ["bartender", "waiter"],
                    "preferences": [],
                },
            ],
            "shifts": [
                {
                    "id": "morning",
                    "name": "Morning",
                    "start_time": "2024-12-25T08:00:00",
                    "end_time": "2024-12-25T14:00:00",
                    "required_abilities": ["waiter"],
                },
                {
                    "id": "evening",
                    "name": "Evening",
                    "start_time": "2024-12-25T18:00:00",
                    "end_time": "2024-12-25T23:00:00",
                    "required_abilities": ["bartender"],
                },
            ],
            "max_solutions": 1,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        # Alice should get the evening shift (her preference)
        assert data["solutions"][0]["assignments"]["evening"] == "alice"

    def test_optimize_with_unavailable_period(self, client: TestClient):
        """Test optimization respects hard unavailability constraints."""
        request = {
            "employees": [
                {
                    "id": "alice",
                    "name": "Alice",
                    "abilities": ["waiter"],
                    "preferences": [
                        {
                            "type": "unavailable_period",
                            "start": "2024-12-25T00:00:00",
                            "end": "2024-12-25T23:59:59",
                            "is_hard": True,
                        }
                    ],
                },
                {
                    "id": "bob",
                    "name": "Bob",
                    "abilities": ["waiter"],
                    "preferences": [],
                },
            ],
            "shifts": [
                {
                    "id": "shift1",
                    "name": "Christmas Shift",
                    "start_time": "2024-12-25T08:00:00",
                    "end_time": "2024-12-25T14:00:00",
                    "required_abilities": ["waiter"],
                }
            ],
            "max_solutions": 1,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        # Alice is unavailable, so Bob gets the shift
        assert data["solutions"][0]["assignments"]["shift1"] == "bob"

    def test_optimize_no_qualified_employees(self, client: TestClient):
        """Test optimization fails when no employees can fill a shift."""
        request = {
            "employees": [
                {
                    "id": "emp1",
                    "name": "Alice",
                    "abilities": ["waiter"],
                    "preferences": [],
                }
            ],
            "shifts": [
                {
                    "id": "shift1",
                    "name": "Bartender Shift",
                    "start_time": "2024-12-25T08:00:00",
                    "end_time": "2024-12-25T14:00:00",
                    "required_abilities": ["bartender"],
                }
            ],
            "max_solutions": 1,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        # No solution possible - employee doesn't have required ability
        assert data["success"] is True
        assert len(data["solutions"]) == 0

    def test_optimize_multiple_solutions(self, client: TestClient):
        """Test requesting multiple solutions."""
        request = {
            "employees": [
                {"id": "alice", "name": "Alice", "abilities": ["waiter"], "preferences": []},
                {"id": "bob", "name": "Bob", "abilities": ["waiter"], "preferences": []},
            ],
            "shifts": [
                {
                    "id": "shift1",
                    "name": "Morning",
                    "start_time": "2024-12-25T08:00:00",
                    "end_time": "2024-12-25T14:00:00",
                    "required_abilities": ["waiter"],
                }
            ],
            "max_solutions": 5,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        # Both Alice and Bob can fill the shift, so we should get 2 solutions
        assert len(data["solutions"]) == 2

    def test_optimize_prefer_period(self, client: TestClient):
        """Test optimization with prefer_period preference."""
        request = {
            "employees": [
                {
                    "id": "alice",
                    "name": "Alice",
                    "abilities": ["waiter"],
                    "preferences": [
                        {
                            "type": "prefer_period",
                            "start": "2024-12-29T00:00:00",
                            "end": "2024-12-30T00:00:00",
                            "is_hard": False,
                        }
                    ],
                },
                {"id": "bob", "name": "Bob", "abilities": ["waiter"], "preferences": []},
            ],
            "shifts": [
                {
                    "id": "christmas",
                    "name": "Christmas",
                    "start_time": "2024-12-25T10:00:00",
                    "end_time": "2024-12-25T18:00:00",
                    "required_abilities": ["waiter"],
                },
                {
                    "id": "sunday",
                    "name": "Sunday",
                    "start_time": "2024-12-29T10:00:00",
                    "end_time": "2024-12-29T18:00:00",
                    "required_abilities": ["waiter"],
                },
            ],
            "max_solutions": 1,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        # Alice prefers Sunday (Dec 29), so she should get that shift
        assert data["solutions"][0]["assignments"]["sunday"] == "alice"

    def test_optimize_empty_request(self, client: TestClient):
        """Test optimization with no employees or shifts."""
        request = {"employees": [], "shifts": [], "max_solutions": 1}

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        assert data["success"] is True
        assert len(data["solutions"]) == 1
        assert data["solutions"][0]["assignments"] == {}

    def test_optimize_invalid_max_solutions(self, client: TestClient):
        """Test validation rejects invalid max_solutions."""
        request = {"employees": [], "shifts": [], "max_solutions": 0}

        response = client.post("/api/optimize", json=request)

        # FastAPI validation error
        assert response.status_code == 422

    def test_optimize_returns_metrics(self, client: TestClient):
        """Test that solution includes metrics."""
        request = {
            "employees": [
                {"id": "alice", "name": "Alice", "abilities": ["waiter"], "preferences": []}
            ],
            "shifts": [
                {
                    "id": "shift1",
                    "name": "Morning",
                    "start_time": "2024-12-25T08:00:00",
                    "end_time": "2024-12-25T14:00:00",
                    "required_abilities": ["waiter"],
                }
            ],
            "max_solutions": 1,
        }

        response = client.post("/api/optimize", json=request)

        assert response.status_code == 200
        data = response.json()
        solution = data["solutions"][0]
        assert "metrics" in solution
        assert "soft_preference_score" in solution["metrics"]
        assert "total_shifts_assigned" in solution["metrics"]
        assert solution["metrics"]["total_shifts_assigned"] == 1
