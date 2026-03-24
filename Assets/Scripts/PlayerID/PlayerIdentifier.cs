using System.Collections.Generic;
using UnityEngine;
using GazeID.Tracking;

namespace GazeID.Player
{
    [System.Serializable]
    public class GazeProfile
    {
        public string PlayerID;
        public string DisplayName;
        public float  AvgPupilDiameter;
        public float  AvgBlinkIntervalSec;
        public long   LastSeenMs;
    }

    public class PlayerIdentifier : MonoBehaviour
    {
        [Header("Identification Thresholds")]
        [SerializeField] private float _pupilMatchTolerance  = 0.08f;
        [SerializeField] private float _blinkMatchTolerance  = 0.9f;
        [SerializeField] private int   _calibrationFrames    = 300; // 5 sec @ 60fps

        public string  CurrentPlayerID   { get; private set; } = "Unknown";
        public bool    IsCalibrating      => _frameCount < _calibrationFrames;
        public float   CalibrationProgress => (float)_frameCount / _calibrationFrames;

        // Object-pooled list to avoid allocs — GC tuning
        private readonly List<float> _pupilSamples = new List<float>(512);
        private int   _blinkCount;
        private float _sessionStartSec;
        private int   _frameCount;

        private void OnEnable()
        {
            _sessionStartSec = Time.realtimeSinceStartup;
            GazeDataCollector.Instance.OnGazeFrame += HandleFrame;
        }

        private void OnDisable()
        {
            if (GazeDataCollector.Instance != null)
                GazeDataCollector.Instance.OnGazeFrame -= HandleFrame;
        }

        private bool _wasBlinking;

        private void HandleFrame(GazeFrame frame)
        {
            _frameCount++;
            _pupilSamples.Add(frame.PupilDiameter);

            // Count blink transitions (false→true)
            if (frame.IsBlinking && !_wasBlinking) _blinkCount++;
            _wasBlinking = frame.IsBlinking;

            if (_frameCount == _calibrationFrames)
                FinalizeCalibration();
        }

        private void FinalizeCalibration()
        {
            float avgPupil = ComputeAverage(_pupilSamples);
            float sessionDur = Time.realtimeSinceStartup - _sessionStartSec;
            float avgBlinkInterval = _blinkCount > 0 ? sessionDur / _blinkCount : 5f;

            var profile = new GazeProfile
            {
                AvgPupilDiameter    = avgPupil,
                AvgBlinkIntervalSec = avgBlinkInterval,
                LastSeenMs          = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // Send to API for DB match / register
            NetworkingManager.Instance.IdentifyOrRegister(profile, id =>
            {
                CurrentPlayerID = id;
                Debug.Log($"[PlayerID] Identified as: {id}");
            });
        }

        private float ComputeAverage(List<float> samples)
        {
            if (samples.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < samples.Count; i++) sum += samples[i];
            return sum / samples.Count;
        }
    }
}