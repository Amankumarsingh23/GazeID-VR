using System.Collections.Generic;
using UnityEngine;

public class PlayerIdentifier : MonoBehaviour
{
    [SerializeField] private int _calibrationFrames = 300;

    public string CurrentPlayerID { get; private set; } = "Unknown";
    public float CalibrationProgress => (float)_frameCount / _calibrationFrames;

    private readonly List<float> _pupilSamples = new List<float>(512);
    private int   _blinkCount;
    private float _sessionStartSec;
    private int   _frameCount;
    private bool  _wasBlinking;

    private void Start()
{
    _sessionStartSec = Time.realtimeSinceStartup;
    if (GazeDataCollector.Instance != null)
        GazeDataCollector.Instance.OnGazeFrame += HandleFrame;
    else
        Debug.LogError("[PlayerID] GazeDataCollector instance not found!");
}

    private void OnDisable()
    {
        if (GazeDataCollector.Instance != null)
            GazeDataCollector.Instance.OnGazeFrame -= HandleFrame;
    }

    private void HandleFrame(GazeFrame frame)
    {
        _frameCount++;
        _pupilSamples.Add(frame.PupilDiameter);

        if (frame.IsBlinking && !_wasBlinking) _blinkCount++;
        _wasBlinking = frame.IsBlinking;

        if (_frameCount == _calibrationFrames)
            FinalizeCalibration();
    }

    private void FinalizeCalibration()
    {
        float avgPupil = 0f;
        for (int i = 0; i < _pupilSamples.Count; i++) avgPupil += _pupilSamples[i];
        avgPupil /= _pupilSamples.Count;

        float sessionDur = Time.realtimeSinceStartup - _sessionStartSec;
        float avgBlink = _blinkCount > 0 ? sessionDur / _blinkCount : 5f;

        Debug.Log($"[PlayerID] Calibration done. Pupil: {avgPupil:F3}, Blink interval: {avgBlink:F2}s");

        if (NetworkingManager.Instance != null)
            NetworkingManager.Instance.IdentifyOrRegister(avgPupil, avgBlink, id =>
            {
                CurrentPlayerID = id;
                Debug.Log($"[PlayerID] Identified as: {id}");
            });
    }
}