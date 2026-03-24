from fastapi import FastAPI, Depends
from pydantic import BaseModel
from sqlalchemy import create_engine, Column, String, Float, Integer
from sqlalchemy.orm import declarative_base, sessionmaker, Session
import uuid, math

app = FastAPI(title="GazeID Backend")

# Fix SQLite multithreading limitation for FastAPI
engine = create_engine("sqlite:///./gazeid.db", connect_args={"check_same_thread": False})
Base = declarative_base()
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

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

# Dependency to yield the database session securely
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

@app.post("/identify", response_model=IdentifyResponse)
def identify_player(req: IdentifyRequest, db: Session = Depends(get_db)):
    players = db.query(PlayerRecord).all()

    best_match = None
    best_score = float("inf")

    for p in players:
        # Prevent TypeError if DB contains nulls for older records
        if p.avg_pupil_diameter is None:
            continue
            
        diff = abs(p.avg_pupil_diameter - req.avg_pupil_diameter)
        if diff < MATCH_TOLERANCE and diff < best_score:
            best_score = diff
            best_match = p

    if best_match:
        best_match.session_count += 1
        db.commit()
        db.refresh(best_match)
        fatigue = compute_fatigue(req.avg_pupil_diameter, req.avg_blink_interval)
        return IdentifyResponse(
            player_id=best_match.id,
            display_name=best_match.display_name,
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
    fatigue = compute_fatigue(req.avg_pupil_diameter, req.avg_blink_interval)
    return IdentifyResponse(
        player_id=new_player.id,
        display_name=new_player.display_name,
        is_new_player=True,
        fatigue_score=fatigue
    )

@app.get("/players")
def list_players(db: Session = Depends(get_db)):
    players = db.query(PlayerRecord).all()
    return [{"id": p.id, "name": p.display_name, "sessions": p.session_count} for p in players]

@app.get("/health")
def health(): return {"status": "ok"}
