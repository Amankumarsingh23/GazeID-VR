using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GazeHeatmap : MonoBehaviour
{
    [SerializeField] private float _brushSize  = 0.06f;
    [SerializeField] private float _decayRate  = 0.995f;

    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
        if (GazeDataCollector.Instance != null)
            GazeDataCollector.Instance.OnGazeFrame += OnGaze;
    }

    private void OnDestroy()
    {
        if (GazeDataCollector.Instance != null)
            GazeDataCollector.Instance.OnGazeFrame -= OnGaze;
    }

    private void OnGaze(GazeFrame frame)
    {
        if (frame.IsBlinking) return;

        Ray ray = new Ray(frame.GazeOrigin, frame.GazeDirection);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.DrawRay(hit.point, Vector3.up * 0.1f, Color.red, 0.5f);
            Debug.Log($"[Heatmap] Gaze hit at UV: {hit.textureCoord}");
        }
    }
}