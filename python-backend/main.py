from fastapi import FastAPI
from pydantic import BaseModel
from sqlalchemy import create_engine, Column, String, Float, Integer
from sqlalchemy.orm import declarative_base, sessionmaker
import uuid, math

app = FastAPI(title="GazeID Backend")
import os
DB_URL = os.environ.get("DATABASE_URL", "sqlite:///./gazeid.db")
engine = create_engine(DB_URL)
Base = declarative_base()
Session = sessionmaker(bind=engine)

class PlayerRecord(Base):
    __tablename__ = "players"
    id                  = Column(String, primary_key=True, default=lambda: str(uuid.uuid4())[:8])
    display_name        = Column(String, default="Player")
    avg_pupil_diameter  = Column(Float)
    avg_blink_interval  = Column(Float)
    session_count       = Column(Integer, default=1)

Base.metadata.create_all(engine)

class IdentifyRequest(BaseModel):
    avg_pupil_diameter: float
    avg_blink_interval: float
    timestamp_ms: int

class IdentifyResponse(BaseModel):
    player_id:     str
    display_name:  str
    is_new_player: bool
    fatigue_score: float

def compute_fatigue(pupil: float, blink_interval: float) -> float:
    """
    Simple heuristic:
    - Smaller pupil → more fatigue
    - Longer blink interval → less fatigue (alert state)
    Returns 0.0 (fresh) to 1.0 (very fatigued)
    """
    pupil_score = 1.0 - min(max(pupil, 0.0), 1.0)
    blink_score = 1.0 / (1.0 + math.exp((blink_interval - 4.0) * 0.5))
    return round((pupil_score * 0.6 + blink_score * 0.4), 3)

MATCH_TOLERANCE = 0.12

@app.post("/identify", response_model=IdentifyResponse)
def identify_player(req: IdentifyRequest):
    db = Session()
    try:
        players = db.query(PlayerRecord).all()

        best_match = None
        best_score = float("inf")

        for p in players:
            diff = abs(p.avg_pupil_diameter - req.avg_pupil_diameter)
            if diff < MATCH_TOLERANCE and diff < best_score:
                best_score = diff
                best_match = p

        if best_match:
            best_match.session_count += 1
            db.commit()
            # Read all values BEFORE closing session
            pid   = best_match.id
            pname = best_match.display_name
            fatigue = compute_fatigue(req.avg_pupil_diameter, req.avg_blink_interval)
            db.close()
            return IdentifyResponse(
                player_id=pid,
                display_name=pname,
                is_new_player=False,
                fatigue_score=fatigue
            )

        # New player
        new_player = PlayerRecord(
            avg_pupil_diameter=req.avg_pupil_diameter,
            avg_blink_interval=req.avg_blink_interval
        )
        db.add(new_player)
        db.commit()
        db.refresh(new_player)
        # Read all values BEFORE closing session
        pid   = new_player.id
        pname = new_player.display_name
        fatigue = compute_fatigue(req.avg_pupil_diameter, req.avg_blink_interval)
        db.close()
        return IdentifyResponse(
            player_id=pid,
            display_name=pname,
            is_new_player=True,
            fatigue_score=fatigue
        )
    except Exception as e:
        db.close()
        raise e

@app.get("/players")
def list_players():
    db = Session()
    players = db.query(PlayerRecord).all()
    db.close()
    return [{"id": p.id, "name": p.display_name, "sessions": p.session_count} for p in players]

@app.get("/health")
def health(): return {"status": "ok"}
