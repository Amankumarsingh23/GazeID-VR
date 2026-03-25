<div align="center">

<a href="https://github.com/Amankumarsingh23/GazeID-VR">
  <img src="https://readme-typing-svg.demolab.com/?font=Inter&weight=800&size=48&pause=1000&color=302B63&center=true&vCenter=true&width=800&lines=GazeID+VR;Gaze-Based+Player+ID+System" alt="Typing SVG" />
</a>



<p>
  <a href="https://github.com/Amankumarsingh23/GazeID-VR/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/Amankumarsingh23/GazeID-VR/ci.yml?style=for-the-badge&label=CI&color=4caf50"/>
  </a>
  <img src="https://img.shields.io/badge/Unity-2022.3%20LTS-black?style=for-the-badge&logo=unity"/>
  <img src="https://img.shields.io/badge/Python-3.11-blue?style=for-the-badge&logo=python&logoColor=white"/>
  <img src="https://img.shields.io/badge/FastAPI-0.100+-green?style=for-the-badge&logo=fastapi&logoColor=white"/>
  <img src="https://img.shields.io/github/license/Amankumarsingh23/GazeID-VR?style=for-the-badge&color=0077cc"/>
</p>

</div>

---

## 📌 What Is This?

> **GazeID VR** is a virtual reality application built in Unity (C#) that identifies users by their unique gaze patterns and computes real-time brain fatigue scores. Directly mirroring the core technology behind **NeuralPort's ZEN EYE Pro** system, it collects eye-tracking data to build unique profiles and persists player sessions via a FastAPI Python backend—all without requiring manual login or physical input.

---

## ✨ Features

| Feature | Description |
|---|---|
| 👁️ **Gaze-based ID** | Identifies users from pupil diameter and blink interval patterns |
| 🧠 **Real-time Fatigue** | Computes a `0.0–1.0` brain fatigue score using biological heuristics |
| 🔌 **Hardware-Agnostic** | SRanipal-compatible mock interface ready for Tobii or PICO integration |
| 🔥 **Attention Heatmap** | Raycasts gaze onto scene objects and logs UV coordinates for visualization |
| ⚡ **REST API Backend** | FastAPI server with SQLite persistence for multi-session tracking |
| 🚀 **GC-Optimised** | Struct-based frames, object pooling, and zero per-frame heap allocations |

---

## 🏗️ Architecture

```text
┌─────────────────────────┐        ┌─────────────────────────┐
│       UNITY VR UI       │        │     PYTHON BACKEND      │
│                         │        │                         │
│  [VR Headset Data]      │        │  [FastAPI Endpoints]    │
│          │              │        │           │             │
│          ▼              │ POST   │           ▼             │
│  [GazeDataCollector] ───┼───────►│  [Player Identifier]    │
│  (60fps Collection)     │        │  (Matches Gaze Profile) │
│          │              │        │           │             │
│          ▼              │        │           ▼             │
│  [PlayerIdentifier]  ◄──┼────────┤  [Fatigue Scoring]      │
│  (Profile Extraction)   │ JSON   │  (Pupil & Blink Math)   │
└─────────────────────────┘        └─────────────────────────┘

```



## 🔄 The Flow

Data calibrates over ~300 frames (5s) → `POST /identify` to Python → Matches player → Returns fatigue score → Unity Console displays:

```
[PlayerID] Identified as: PLAYER_3319, Fatigue: 0.35
```

---

## ⚙️ Getting Started

### Prerequisites

* Unity 2022.3 LTS (with XR Interaction Toolkit & OpenXR Plugin)
* Python 3.11+
* Git

---

### 1. Run the Backend

```bash
cd python-backend
python -m venv venv
venv\Scripts\activate        # Windows
# source venv/bin/activate   # Linux/Mac

pip install fastapi uvicorn sqlalchemy pydantic
uvicorn main:app --reload --port 8000
```

---

### 2. Run the Unity Project

* Open **GazeID-VR** in Unity Hub
* Open `Assets/Scenes/SampleScene`
* Hit **Play**

Watch the Console. After ~5 seconds you will see:

```
[PlayerID] Calibration done. Pupil avg: 0.667, Blink interval: 5.00s
[API] Response: {"player_id":"21b62743","fatigue_score":0.351}
[PlayerID] Identified as: PLAYER_3319
```

---

### 3. Run the Tests

```bash
cd python-backend
pytest tests/ -v
```

---

## 📁 Project Structure

```
GazeID-VR/
├── Assets/
│   └── Scripts/
│       ├── GazeTracking/
│       │   ├── GazeDataCollector.cs   # Mock eye-tracking data source
│       │   └── GazeHeatmap.cs         # Foveated attention heatmap
│       ├── PlayerID/
│       │   └── PlayerIdentifier.cs    # Calibration + profile extraction
│       ├── Networking/
│       │   └── NetworkingManager.cs   # REST API client
│       └── Performance/               # GC tuning utilities
├── python-backend/
│   ├── main.py                        # FastAPI server + SQLite DB
│   └── tests/
│       └── test_api.py                # pytest test suite
└── .github/
    └── workflows/
        └── ci.yml                     # GitHub Actions CI
```

---

## 🧠 Fatigue Scoring Algorithm

The system calculates brain fatigue based on standard pupillometry heuristics:

```
fatigue = (pupil_score × 0.6) + (blink_score × 0.4)

pupil_score  = 1.0 - normalized_pupil_diameter
               (smaller pupil → more fatigue)

blink_score  = sigmoid(blink_interval - 4.0)
               (longer interval between blinks → alert state)
```

Score ranges from:

* `0.0` → Fully alert
* `1.0` → Highly fatigued

---

## 🔌 Replacing Mock Data with Hardware

The entire eye-tracking layer is isolated in `GazeDataCollector.cs`.

To implement a real headset SDK, replace:

```csharp
// Current: mock data
private GazeFrame GetMockGaze() { ... }
```

With:

```csharp
// SRanipal
// SRanipal_Eye_API.GetEyeData(ref eyeData);

// Tobii
// TobiiAPI.GetGazePoint();

// PICO
// PXR_EyeTracking.GetCombineEyeGazePoint(out gazePoint);
```

---

## 🗺️ Roadmap

* [ ] World-space VR UI panel showing live fatigue score
* [ ] Dwell-time gaze selection for touchless menu navigation
* [ ] Real eye-tracking SDK integration (PICO / Tobii)
* [ ] Heatmap RenderTexture visualization on quad surface
* [ ] WebSocket streaming for real-time fatigue updates

---

## 📄 License

Distributed under the MIT License.

---

<div align="center">

### Built with 🔥 by Aman Kumar Singh — IIT Kanpur

<a href="https://linkedin.com/in/aman-singh-iitkanpur">
<img src="https://img.shields.io/badge/LinkedIn-0077B5?style=flat-square&logo=linkedin&logoColor=white"/>
</a>
&nbsp;
<a href="mailto:amansingh23@iitk.ac.in">
<img src="https://img.shields.io/badge/Email-D14836?style=flat-square&logo=gmail&logoColor=white"/>
</a>
&nbsp;
<a href="https://medium.com/@logiclord67">
<img src="https://img.shields.io/badge/Medium-000000?style=flat-square&logo=medium&logoColor=white"/>
</a>

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:24243e,50:302b63,100:0f0c29&height=100&section=footer" width="100%"/>

</div>

---

   
