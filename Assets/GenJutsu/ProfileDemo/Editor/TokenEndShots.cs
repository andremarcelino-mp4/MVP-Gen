using System.IO;
using GenJutsu.GenCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    static class TokenEndShots
    {
        [MenuItem("GenJutsu/QA/Token End Shot")]
        public static void Capture()
        {
            var folder = Directory.Exists("/opt/cursor/artifacts")
                ? "/opt/cursor/artifacts"
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "artifacts"));
            Directory.CreateDirectory(folder);

            var go = new GameObject("SessionTokenMeterShot");
            go.transform.position = new Vector3(0f, 1.18f, 0.55f);
            var meter = go.AddComponent<SessionTokenMeter>();
            meter.EnsureVisual();
            meter.BeginVisit();
            meter.NoteLocal(new LlmTokenUsage { In = 900, Out = 334, Total = 1234, Calls = 3 }, false);
            meter.ShowEndOfVisit();

            var camGo = new GameObject("TokenEndCam");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 1.18f, -0.35f);
            cam.transform.LookAt(go.transform.position, Vector3.up);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 12f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.07f);
            var urp = cam.GetUniversalAdditionalCameraData();
            if (urp != null)
                urp.renderPostProcessing = false;

            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            var path = Path.Combine(folder, "31-session-tokens.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(go);
            Debug.Log("[TokenEnd] " + path);
        }
    }
}
