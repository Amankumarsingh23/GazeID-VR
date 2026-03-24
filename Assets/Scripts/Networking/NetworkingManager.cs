using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using GazeID.Player;

namespace GazeID.Networking
{
    [System.Serializable]
    public class IdentifyRequest
    {
        public float avg_pupil_diameter;
        public float avg_blink_interval;
        public long  timestamp_ms;
    }

    [System.Serializable]
    public class IdentifyResponse
    {
        public string player_id;
        public string display_name;
        public bool   is_new_player;
        public float  fatigue_score;
    }

    public class NetworkingManager : MonoBehaviour
    {
        public static NetworkingManager Instance { get; private set; }

        [SerializeField] private string _baseUrl = "http://localhost:8000";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void IdentifyOrRegister(GazeProfile profile, Action<string> onComplete)
        {
            StartCoroutine(PostIdentify(profile, onComplete));
        }

        private IEnumerator PostIdentify(GazeProfile profile, Action<string> onComplete)
        {
            var req = new IdentifyRequest
            {
                avg_pupil_diameter  = profile.AvgPupilDiameter,
                avg_blink_interval  = profile.AvgBlinkIntervalSec,
                timestamp_ms        = profile.LastSeenMs
            };

            string json = JsonUtility.ToJson(req);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using var www = new UnityWebRequest($"{_baseUrl}/identify", "POST");
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<IdentifyResponse>(www.downloadHandler.text);
                onComplete?.Invoke(resp.player_id);
                Debug.Log($"[API] Player: {resp.player_id}, Fatigue: {resp.fatigue_score:F2}");
            }
            else
            {
                Debug.LogWarning($"[API] Request failed: {www.error}. Using local fallback.");
                onComplete?.Invoke("LOCAL_" + SystemInfo.deviceUniqueIdentifier[..8]);
            }
        }
    }
}
