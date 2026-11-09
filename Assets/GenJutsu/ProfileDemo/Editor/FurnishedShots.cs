using System.IO;
using GenJutsu.HouseState;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    [InitializeOnLoad]
    static class FurnishedShots
    {
        const string Marker = "Temp/furnished-shots.on";
        const string PhaseKey = "GenJutsu.FurnPhase";
        const string TimeKey = "GenJutsu.FurnT";

        static FurnishedShots()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }

        static void OnPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !File.Exists(Marker))
                return;
            EditorPrefs.SetInt(PhaseKey, 1);
            EditorPrefs.SetFloat(TimeKey, (float)EditorApplication.timeSinceStartup);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Marker) || !EditorApplication.isPlaying)
                return;

            var phase = EditorPrefs.GetInt(PhaseKey, 1);
            var director = Object.FindFirstObjectByType<ProfileDemoDirector>();
            var gate = Object.FindFirstObjectByType<SessionGate>();
            var dojo = Object.FindFirstObjectByType<DojoDirector>();
            if (director == null || gate == null)
                return;
            gate.suppressNetworkPoll = true;
            if (gate.group != null)
                gate.group.alpha = 0f;

            switch (phase)
            {
                case 1:
                    if (Elapsed() < 1.4f)
                        return;
                    HideHud();
                    gate.Ingest("{\"session\":{\"id\":\"SHOT\",\"session_code\":\"SHOT\",\"display_name\":\"André\",\"profile_id\":\"Acolhedor\",\"status\":\"active\"}}", 200, true);
                    PlaqueShot("artifacts/12-white-welcome.png", false);
                    SetPhase(2);
                    break;
                case 2:
                    if (Elapsed() < 0.4f)
                        return;
                    var plaque = Object.FindAnyObjectByType<StartPlaque>();
                    if (plaque != null)
                        plaque.PreviewGlow(true);
                    PlaqueShot("artifacts/13-plaque-glow.png", true);
                    SetPhase(3);
                    break;
                case 3:
                    ScrollShot("artifacts/14-scroll-paper.png");
                    gate.Ingest("{\"session\":{\"id\":\"ZEN001\",\"session_code\":\"ZEN001\",\"display_name\":\"André\",\"profile_id\":\"Zen\",\"status\":\"active\"}}", 200, true);
                    SetPhase(4);
                    break;
                case 4:
                    if (dojo != null)
                        dojo.TryBeginVisit();
                    if (!InHouse(director))
                        return;
                    Iso(director, "artifacts/15-dojo-z.png");
                    director.SwitchVisualDojo(ProfileId.Cyber);
                    SetPhase(5);
                    break;
                case 5:
                    if (!InHouse(director) || director.Active != ProfileId.Cyber)
                        return;
                    Iso(director, "artifacts/16-dojo-c.png");
                    director.SwitchVisualDojo(ProfileId.Excentrico);
                    SetPhase(6);
                    break;
                case 6:
                    if (!InHouse(director) || director.Active != ProfileId.Excentrico)
                        return;
                    Iso(director, "artifacts/17-dojo-e.png");
                    director.SwitchVisualDojo(ProfileId.Acolhedor);
                    SetPhase(7);
                    break;
                case 7:
                    if (!InHouse(director) || director.Active != ProfileId.Acolhedor)
                        return;
                    Iso(director, "artifacts/18-dojo-a.png");
                    var listener = Object.FindFirstObjectByType<HouseStateListener>();
                    if (listener != null)
                        listener.Ingest("{\"state\":{\"session_id\":\"ZEN001\",\"version\":11,\"devices\":{\"sala_luz\":{\"on\":true,\"color\":\"#FF1A1A\",\"intensity\":1}},\"lights\":{\"main\":{\"color\":\"#FF1A1A\",\"intensity\":0.9},\"lanterns\":{\"color\":\"#FF1A1A\",\"intensity\":1}}}}");
                    SetPhase(8);
                    break;
                case 8:
                    if (Elapsed() < 1.4f)
                        return;
                    Iso(director, "artifacts/19-dojo-a-sala-red.png");
                    Finish();
                    break;
            }
        }

        static bool InHouse(ProfileDemoDirector director)
        {
            if (director.Current == ProfileDemoDirector.State.InHouse && Elapsed() > 3.2f)
                return true;
            if (Elapsed() > 14f)
                return true;
            return false;
        }

        static void PlaqueShot(string path, bool glow)
        {
            HideHud();
            var plaque = Object.FindAnyObjectByType<StartPlaque>();
            if (plaque == null)
                return;
            if (glow)
                plaque.PreviewGlow(true);
            var look = plaque.transform.position + Vector3.up * 0.02f;
            var from = look + plaque.transform.forward * 0.72f + Vector3.up * 0.12f;
            Capture(from, look, 42f, path);
        }

        static void ScrollShot(string path)
        {
            var paper = GameObject.Find("Paper");
            if (paper == null)
                return;
            var look = paper.transform.position;
            var from = look + new Vector3(0.05f, 0.42f, -0.38f);
            Capture(from, look, 40f, path);
        }

        static void Iso(ProfileDemoDirector director, string path)
        {
            HideHud();
            var focus = director.transform.root.position + Vector3.up * 0.85f;
            var from = focus + new Vector3(5.1f, 4.35f, -5.1f);
            Capture(from, focus, 36f, path);
        }

        static void Capture(Vector3 from, Vector3 look, float fov, string relative)
        {
            Directory.CreateDirectory("artifacts");
            var go = new GameObject("FurnShotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.46f, 0.48f);
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
            File.WriteAllBytes(relative, tex.EncodeToPNG());
            RenderTexture.active = prev;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Debug.Log("[FurnishedShots] " + relative);
        }

        static void HideHud()
        {
            var voice = Object.FindAnyObjectByType<Gen.GenStatusView>();
            if (voice != null)
                voice.gameObject.SetActive(false);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t.name.Contains("Controller") || t.name.Contains("Hand"))
                    t.gameObject.SetActive(false);
            }
        }

        static float Elapsed()
        {
            return (float)EditorApplication.timeSinceStartup - EditorPrefs.GetFloat(TimeKey, 0f);
        }

        static void SetPhase(int phase)
        {
            EditorPrefs.SetInt(PhaseKey, phase);
            EditorPrefs.SetFloat(TimeKey, (float)EditorApplication.timeSinceStartup);
        }

        static void Finish()
        {
            EditorPrefs.SetInt(PhaseKey, 0);
            EditorApplication.update -= Tick;
            if (File.Exists(Marker))
                File.Delete(Marker);
            EditorApplication.isPlaying = false;
        }
    }
}
