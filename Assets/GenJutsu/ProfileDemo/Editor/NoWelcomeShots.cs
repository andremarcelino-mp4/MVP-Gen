using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    [InitializeOnLoad]
    static class NoWelcomeShots
    {
        const string Marker = "Temp/no-welcome-shots.on";

        static NoWelcomeShots()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !File.Exists(Marker))
                    return;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            };
        }

        static float started;

        static void Tick()
        {
            if (!File.Exists(Marker) || !EditorApplication.isPlaying)
                return;
            if (started < 1f)
                started = (float)EditorApplication.timeSinceStartup;
            var director = Object.FindAnyObjectByType<ProfileDemoDirector>();
            var gate = Object.FindAnyObjectByType<SessionGate>();
            if (director == null)
                return;
            if (gate != null)
            {
                gate.suppressNetworkPoll = true;
                gate.HideCardVisual();
            }

            if ((float)EditorApplication.timeSinceStartup - started < 1.2f)
                return;
            var look = director.transform.root.position + new Vector3(0f, 1.35f, 0.2f);
            Directory.CreateDirectory("artifacts");
            var go = new GameObject("NoWelcomeCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = look + new Vector3(0f, 0.1f, -1.7f);
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 30f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rt = new RenderTexture(1280, 960, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 960, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0);
            tex.Apply();
            File.WriteAllBytes("artifacts/30-no-welcome.png", tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            File.Delete(Marker);
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            started = 0f;
            Debug.Log("[NoWelcome] artifacts/30-no-welcome.png");
        }
    }
}
