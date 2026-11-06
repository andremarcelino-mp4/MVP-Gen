using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo
{
    [Serializable]
    public class ActivityResult
    {
        public string id;
        public string displayName;
        public string unit;
        public bool higherIsBetter = true;
        public float best;
        public float last;
        public int attempts;
        public string detail;
    }

    [Serializable]
    public class ProfileSummary
    {
        public string profile;
        public int visits;
        public float totalSeconds;
        public List<ActivityResult> activities = new List<ActivityResult>();
        public List<string> captures = new List<string>();
    }

    [Serializable]
    public class SessionEvent
    {
        public float time;
        public string type;
        public string profile;
        public string activity;
        public float value;
        public string note;
    }

    [Serializable]
    public class SessionData
    {
        public string sessionId;
        public string startedAt;
        public string device;
        public string app;
        public List<ProfileSummary> profiles = new List<ProfileSummary>();
        public List<SessionEvent> events = new List<SessionEvent>();
    }

    /// <summary>
    /// Writes the visitor's journey (visits, scores, captures) to persistentDataPath for the post-experience.
    /// </summary>
    public class DemoSessionRecorder : MonoBehaviour
    {
        public static DemoSessionRecorder Instance { get; private set; }

        public int viewCaptureWidth = 1280;
        public int viewCaptureHeight = 720;

        public event Action Changed;

        public string SessionId => data.sessionId;
        public string Folder { get; private set; }

        SessionData data;
        string jsonPath;
        bool inProfile;
        ProfileId activeProfile;
        float enteredAt;
        Camera viewCamera;

        void Awake()
        {
            Instance = this;
            data = new SessionData
            {
                sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                startedAt = DateTime.Now.ToString("o"),
                device = SystemInfo.deviceModel,
                app = Application.productName + " " + Application.version
            };
            for (var i = 0; i < Profiles.Count; i++)
                data.profiles.Add(new ProfileSummary { profile = ((ProfileId)i).ToString() });

            Folder = Path.Combine(Application.persistentDataPath, "GenJutsuSessions", data.sessionId);
            Directory.CreateDirectory(Folder);
            jsonPath = Path.Combine(Folder, "session.json");
            Log("session_start", null, null, 0f, data.device);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Save();
        }

        public ProfileSummary GetSummary(ProfileId id)
        {
            return data.profiles[(int)id];
        }

        public void BeginProfile(ProfileId id)
        {
            if (inProfile)
                EndProfile();
            inProfile = true;
            activeProfile = id;
            enteredAt = Time.time;
            GetSummary(id).visits++;
            Log("enter_house", id.ToString(), null, 0f, null);
        }

        public void EndProfile()
        {
            if (!inProfile)
                return;
            var seconds = Time.time - enteredAt;
            GetSummary(activeProfile).totalSeconds += seconds;
            inProfile = false;
            Log("exit_house", activeProfile.ToString(), null, seconds, null);
        }

        public void ReportScore(ProfileId profile, string id, string label, float value, string unit, bool higherIsBetter, string detail)
        {
            var summary = GetSummary(profile);
            var result = summary.activities.Find(a => a.id == id);
            if (result == null)
            {
                result = new ActivityResult { id = id, displayName = label, unit = unit, higherIsBetter = higherIsBetter, best = value };
                summary.activities.Add(result);
            }
            else if (higherIsBetter ? value > result.best : value < result.best)
            {
                result.best = value;
            }

            result.last = value;
            result.attempts++;
            if (!string.IsNullOrEmpty(detail))
                result.detail = detail;
            Log("score", profile.ToString(), id, value, detail);
        }

        public string SaveCapture(ProfileId profile, string label, Texture2D texture)
        {
            if (texture == null)
                return null;
            var summary = GetSummary(profile);
            var fileName = profile + "_" + label + "_" + (summary.captures.Count + 1).ToString("00") + ".png";
            var path = Path.Combine(Folder, fileName);
            var png = texture.EncodeToPNG();
            Task.Run(() => File.WriteAllBytes(path, png));
            summary.captures.Add(fileName);
            Log("capture", profile.ToString(), label, summary.captures.Count, fileName);
            return fileName;
        }

        public void CaptureView(ProfileId profile, string label)
        {
            var main = Camera.main;
            if (main == null)
                return;
            if (viewCamera == null)
            {
                var go = new GameObject("SessionViewCamera");
                go.transform.SetParent(transform, false);
                viewCamera = go.AddComponent<Camera>();
                viewCamera.enabled = false;
                UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(viewCamera).renderPostProcessing = true;
                viewCamera.fieldOfView = 80f;
                viewCamera.nearClipPlane = 0.05f;
            }

            viewCamera.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            viewCamera.cullingMask = main.cullingMask;
            viewCamera.clearFlags = main.clearFlags;
            viewCamera.backgroundColor = main.backgroundColor;
            var texture = DemoCapture.Render(viewCamera, viewCaptureWidth, viewCaptureHeight);
            SaveCapture(profile, label, texture);
            Destroy(texture);
        }

        void Log(string type, string profile, string activity, float value, string note)
        {
            data.events.Add(new SessionEvent { time = Time.time, type = type, profile = profile, activity = activity, value = value, note = note });
            Save();
            Changed?.Invoke();
        }

        void Save()
        {
            try
            {
                File.WriteAllText(jsonPath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GenJutsu] Could not save session: " + e.Message);
            }
        }
    }

    public static class DemoCapture
    {
        public static Texture2D Render(Camera camera, int width, int height)
        {
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 2 };
            var rt = RenderTexture.GetTemporary(descriptor);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.targetTexture = rt;
                camera.Render();
                camera.targetTexture = null;
            }

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return texture;
        }
    }
}
