import os
import pytest
from fastapi.testclient import TestClient

os.environ["DATABASE_URL"] = "sqlite:///./test_gazeid.db"

from main import app

client = TestClient(app)

def teardown_module(module):
    try:
        from main import engine
        engine.dispose()
        import time
        time.sleep(0.1)
        if os.path.exists("test_gazeid.db"):
            os.remove("test_gazeid.db")
    except Exception:
        pass
def test_health():
    r = client.get("/health")
    assert r.status_code == 200

def test_identify_new_player():
    r = client.post("/identify", json={
        "avg_pupil_diameter": 0.65,
        "avg_blink_interval": 4.5,
        "timestamp_ms": 1700000000000
    })
    assert r.status_code == 200
    data = r.json()
    assert "player_id" in data
    assert data["is_new_player"] == True
    assert 0.0 <= data["fatigue_score"] <= 1.0

def test_identify_returning_player():
    client.post("/identify", json={
        "avg_pupil_diameter": 0.72,
        "avg_blink_interval": 3.8,
        "timestamp_ms": 1700000001000
    })
    r = client.post("/identify", json={
        "avg_pupil_diameter": 0.73,
        "avg_blink_interval": 3.9,
        "timestamp_ms": 1700000002000
    })
    data = r.json()
    assert data["is_new_player"] == False