using UnityEngine;
using UnityEngine.XR;

namespace GazeID.Tracking
{
    // Struct avoids heap allocation on every frame — GC tuning
    public struct GazeFrame
    {
        public Vector3 GazeDirection;
        public Vector3 GazeOrigin;
        public float   PupilDiameter;   // normalized 0–1
        public bool    IsBlinking;
        public long    TimestampMs;
    }

    /// <summary>
    /// SRanipal-compatible mock gaze source.
    /// Swap out GetMockGaze() with real SDK call when hardware is available.
    /// </summary>
    public class GazeDataCollector : MonoBehaviour
    {
        public static GazeDataCollector Instance { get; private set; }

        [Header("Mock Settings")]
        [SerializeField] private float _blinkIntervalSec = 4.5f;
        [SerializeField] private float _pupilBaseSize    = 0.65f;
        [SerializeField] private float _gazeNoiseAmount  = 0.02f;

        public event System.Action<GazeFrame> OnGazeFrame;

        private float _blinkTimer;
        private bool  _isBlinking;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // Blink simulation
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= _blinkIntervalSec)
            {
                _isBlinking = true;
                _blinkTimer  = 0f;
                Invoke(nameof(EndBlink), 0.15f);
            }

            GazeFrame frame = GetMockGaze();
            OnGazeFrame?.Invoke(frame);
        }

        private void EndBlink() => _isBlinking = false;

        private GazeFrame GetMockGaze()
        {
            // In real build: replace with SRanipal SDK call:
            //   SRanipal_Eye_API.GetEyeData(ref eyeData);
            Vector3 noise = new Vector3(
                Random.Range(-_gazeNoiseAmount, _gazeNoiseAmount),
                Random.Range(-_gazeNoiseAmount, _gazeNoiseAmount),
                0f);

            return new GazeFrame
            {
                GazeDirection = (Camera.main.transform.forward + noise).normalized,
                GazeOrigin    = Camera.main.transform.position,
                PupilDiameter = _pupilBaseSize + Mathf.Sin(Time.time * 0.3f) * 0.1f,
                IsBlinking    = _isBlinking,
                TimestampMs   = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}