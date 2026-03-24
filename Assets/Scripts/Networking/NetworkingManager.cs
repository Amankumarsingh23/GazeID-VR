using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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

    public void IdentifyOrRegister(float avgPupil, float avgBlink, Action<string> onComplete)
    {
        StartCoroutine(PostIdentify(avgPupil, avgBlink, onComplete));
    }

    private IEnumerator PostIdentify(float avgPupil, float avgBlink, Action<string> onComplete)
    {
        string json = $"{{\"avg_pupil_diameter\":{avgPupil},\"avg_blink_interval\":{avgBlink},\"timestamp_ms\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var www = new UnityWebRequest($"{_baseUrl}/identify", "POST");
        www.uploadHandler   = new UploadHandlerRaw(body);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[API] Response: {www.downloadHandler.text}");
            onComplete?.Invoke("PLAYER_" + UnityEngine.Random.Range(1000,9999));
        }
        else
        {
            Debug.LogWarning($"[API] Failed: {www.error}. Using fallback.");
            onComplete?.Invoke("LOCAL_" + SystemInfo.deviceUniqueIdentifier.Substring(0,8));
        }
    }
}