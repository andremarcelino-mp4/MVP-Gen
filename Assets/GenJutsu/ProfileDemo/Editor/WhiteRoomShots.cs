using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>Headset-height captures of the table: idle plaque and amber engraving.</summary>
    [InitializeOnLoad]
    static class WhiteRoomShots
    {
        const string Marker = "Temp/white-room-shots.on";
        static int frame;
        static bool armed;

        static WhiteRoomShots()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }

        static void OnPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !File.Exists(Marker))
                return;
            frame = 0;
            armed = true;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!armed || !EditorApplication.isPlaying)
                return;
            frame++;
            if (frame == 50)
            {
                var gate = Object.FindFirstObjectByType<SessionGate>();
                if (gate != null)
                {
                    gate.suppressNetworkPoll = true;
                    if (gate.group != null)
                        gate.group.alpha = 0f;
                    gate.Ingest("{\"session\":{\"id\":\"SHOT\",\"session_code\":\"SHOT\",\"display_name\":\"André\",\"profile_id\":\"Acolhedor\",\"status\":\"active\"}}", 200, true);
                }

                Capture("artifacts/10-white-welcome.png");
            }

            if (frame == 80)
            {
                var plaque = Object.FindFirstObjectByType<StartPlaque>();
                if (plaque != null)
                    plaque.PreviewGlow(true);
                Capture("artifacts/11-plaque-glow.png");
            }

            if (frame == 100)
            {
                armed = false;
                EditorApplication.update -= Tick;
                File.Delete(Marker);
                EditorApplication.isPlaying = false;
            }
        }

        static void Capture(string relative)
        {
            var voice = Object.FindAnyObjectByType<GenJutsu.Gen.GenStatusView>();
            if (voice != null)
                voice.gameObject.SetActive(false);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t.name.Contains("Controller") || t.name.Contains("Hand"))
                    t.gameObject.SetActive(false);
            }
            var plaque = Object.FindAnyObjectByType<StartPlaque>();
            var table = GameObject.Find("JapaneseTableSet");
            Vector3 look;
            Vector3 from;
            if (plaque != null)
            {
                look = plaque.transform.position + Vector3.up * 0.04f;
                from = new Vector3(look.x, 1.28f, look.z - 1.2f);
            }
            else if (table != null)
            {
                var renderers = table.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                look = bounds.center + Vector3.up * 0.08f;
                from = new Vector3(look.x, 1.58f, look.z - 1.05f);
            }
            else
                return;
            var go = new GameObject("WhiteRoomShotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.94f, 0.96f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            const int w = 1280;
            const int h = 960;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("artifacts");
            File.WriteAllBytes(relative, tex.EncodeToPNG());
            RenderTexture.active = prev;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Debug.Log("[WhiteRoomShots] " + relative);
        }
    }
}
