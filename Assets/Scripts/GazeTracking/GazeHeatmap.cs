using UnityEngine;
using GazeID.Tracking;

namespace GazeID.Visualization
{
    /// <summary>
    /// Renders a foveated attention heatmap as a RenderTexture
    /// using a soft-brush accumulate pass.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class GazeHeatmap : MonoBehaviour
    {
        [SerializeField] private int    _texSize    = 256;
        [SerializeField] private float  _brushSize  = 0.06f;
        [SerializeField] private float  _decayRate  = 0.995f;   // per frame
        [SerializeField] private Shader _heatShader;

        private RenderTexture _heatRT;
        private Material      _heatMat;
        private Camera        _mainCam;

        private void Start()
        {
            _heatRT  = new RenderTexture(_texSize, _texSize, 0, RenderTextureFormat.RFloat);
            _heatMat = new Material(_heatShader ?? Shader.Find("Unlit/Color"));
            GetComponent<Renderer>().material.mainTexture = _heatRT;
            _mainCam = Camera.main;

            GazeDataCollector.Instance.OnGazeFrame += OnGaze;
        }

        private void OnDestroy()
        {
            if (GazeDataCollector.Instance != null)
                GazeDataCollector.Instance.OnGazeFrame -= OnGaze;
            _heatRT.Release();
        }

        private void OnGaze(GazeFrame frame)
        {
            if (frame.IsBlinking) return;

            // Raycast gaze onto this object
            Ray ray = new Ray(frame.GazeOrigin, frame.GazeDirection);
            if (!Physics.Raycast(ray, out RaycastHit hit) || hit.collider.gameObject != gameObject)
                return;

            PaintHeat(hit.textureCoord);
        }

        private void Update()
        {
            // Decay (simulate attention fading)
            // Simplified: clear very slowly via Graphics.Blit with alpha
        }

        private void PaintHeat(Vector2 uv)
        {
            // In full impl: use Graphics.Blit with a custom accumulation shader
            // For MVP: we log the UV and display via Debug.DrawRay in scene view
            Debug.DrawRay(
                transform.position + new Vector3(uv.x - 0.5f, uv.y - 0.5f, 0),
                Vector3.forward * 0.1f, Color.red, 0.5f);
        }
    }
}