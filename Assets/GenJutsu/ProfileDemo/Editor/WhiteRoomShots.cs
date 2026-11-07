using System.IO;
using UnityEditor;
using UnityEngine;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>One-shot play-mode captures. Runs only when Temp/white-room-shots.on exists.</summary>
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
            if (frame == 40)
            {
                var gate = Object.FindFirstObjectByType<SessionGate>();
                if (gate != null)
                    gate.Ingest("{\"session\":{\"id\":\"SHOT\",\"session_code\":\"SHOT\",\"display_name\":\"André\",\"profile_id\":\"Acolhedor\",\"status\":\"active\"}}", 200, true);
                FrameTable();
            }

            if (frame == 80)
                ScreenCapture.CaptureScreenshot("artifacts/10-white-welcome.png");
            if (frame == 100)
            {
                var plaque = Object.FindFirstObjectByType<StartPlaque>();
                if (plaque != null)
                    plaque.PreviewGlow(true);
            }

            if (frame == 130)
                ScreenCapture.CaptureScreenshot("artifacts/11-plaque-glow.png");
            if (frame == 160)
            {
                armed = false;
                EditorApplication.update -= Tick;
                File.Delete(Marker);
                EditorApplication.isPlaying = false;
            }
        }

        static void FrameTable()
        {
            var table = GameObject.Find("JapaneseTableSet");
            var cam = Camera.main;
            if (table == null || cam == null)
                return;
            var renderers = table.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            var eye = bounds.center + new Vector3(0.15f, 0.55f, -1.25f);
            cam.transform.position = eye;
            cam.transform.LookAt(bounds.center + Vector3.up * 0.05f);
        }
    }
}
