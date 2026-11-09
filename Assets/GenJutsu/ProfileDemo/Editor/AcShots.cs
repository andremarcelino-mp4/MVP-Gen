using System.IO;
using GenJutsu.HouseState;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    [InitializeOnLoad]
    static class AcShots
    {
        const string Marker = "Temp/ac-shots.on";
        const string PhaseKey = "GenJutsu.AcPhase";
        const string TimeKey = "GenJutsu.AcT";

        static AcShots()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !File.Exists(Marker))
                    return;
                EditorPrefs.SetInt(PhaseKey, 1);
                EditorPrefs.SetFloat(TimeKey, (float)EditorApplication.timeSinceStartup);
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            };
        }

        static void Tick()
        {
            if (!File.Exists(Marker) || !EditorApplication.isPlaying)
                return;
            var director = Object.FindAnyObjectByType<ProfileDemoDirector>();
            var gate = Object.FindAnyObjectByType<SessionGate>();
            var dojo = Object.FindAnyObjectByType<DojoDirector>();
            if (director == null || gate == null)
                return;
            gate.suppressNetworkPoll = true;
            var phase = EditorPrefs.GetInt(PhaseKey, 1);
            switch (phase)
            {
                case 1:
                    if (Elapsed() < 1.2f)
                        return;
                    gate.Ingest("{\"session\":{\"id\":\"ACO004\",\"session_code\":\"ACO004\",\"display_name\":\"André\",\"profile_id\":\"Acolhedor\",\"status\":\"active\"}}", 200, true);
                    if (dojo != null)
                        dojo.TryBeginVisit();
                    SetPhase(2);
                    break;
                case 2:
                    if (director.Current != ProfileDemoDirector.State.InHouse || Elapsed() < 3.2f)
                    {
                        if (dojo != null)
                            dojo.TryBeginVisit();
                        if (Elapsed() < 12f)
                            return;
                    }

                    var listener = Object.FindAnyObjectByType<HouseStateListener>();
                    if (listener != null)
                        listener.Ingest("{\"state\":{\"session_id\":\"ACO004\",\"version\":21,\"devices\":{\"ar_condicionado\":{\"on\":true,\"temp\":20,\"mode\":\"frio\",\"fan\":\"2\"}}}}");
                    var ac = Object.FindAnyObjectByType<AirConUnit>();
                    if (ac != null)
                        ac.Set(true, 20, "frio", "2", false);
                    SetPhase(3);
                    break;
                case 3:
                    if (Elapsed() < 0.6f)
                        return;
                    CaptureAc(director, "artifacts/23-ac-on.png");
                    Finish();
                    break;
            }
        }

        static void CaptureAc(ProfileDemoDirector director, string path)
        {
            var tv = GameObject.Find("TvStand");
            var look = tv != null ? tv.transform.position + Vector3.up * 0.85f : director.transform.root.position + Vector3.up * 0.8f;
            var from = look + new Vector3(0.4f, 0.35f, -1.4f);
            Directory.CreateDirectory("artifacts");
            var go = new GameObject("AcCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.1f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rt = new RenderTexture(1280, 960, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 960, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Debug.Log("[AcShots] " + path);
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
