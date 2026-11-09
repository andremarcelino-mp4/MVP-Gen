using System.IO;
using GenJutsu.Gen;
using GenJutsu.GenCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    [InitializeOnLoad]
    static class VerifyShots
    {
        const string Marker = "Temp/verify-shots.on";
        const string PhaseKey = "GenJutsu.VfPhase";
        const string TimeKey = "GenJutsu.VfT";

        static VerifyShots()
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
                    if (Elapsed() < 1f)
                        return;
                    gate.Ingest("{\"session\":{\"id\":\"ZEN001\",\"session_code\":\"ZEN001\",\"display_name\":\"André\",\"profile_id\":\"Zen\",\"status\":\"active\"}}", 200, true);
                    if (dojo != null)
                        dojo.TryBeginVisit();
                    SetPhase(2);
                    break;
                case 2:
                    if (director.Current != ProfileDemoDirector.State.InHouse)
                    {
                        if (dojo != null)
                            dojo.TryBeginVisit();
                        if (Elapsed() < 12f)
                            return;
                    }
                    SpawnShot(director, "artifacts/24-spawn-zen.png");
                    director.SwitchVisualDojo(ProfileId.Cyber);
                    SetPhase(3);
                    break;
                case 3:
                    if (Elapsed() < 2.2f)
                        return;
                    SpawnShot(director, "artifacts/25-spawn-cyber.png");
                    director.SwitchVisualDojo(ProfileId.Excentrico);
                    SetPhase(4);
                    break;
                case 4:
                    if (Elapsed() < 2.2f)
                        return;
                    SpawnShot(director, "artifacts/26-spawn-excentrico.png");
                    director.SwitchVisualDojo(ProfileId.Acolhedor);
                    SetPhase(5);
                    break;
                case 5:
                    if (Elapsed() < 2.2f)
                        return;
                    SpawnShot(director, "artifacts/27-spawn-acolhedor.png");
                    var view = Object.FindAnyObjectByType<GenStatusView>();
                    if (view != null)
                    {
                        view.FollowHead = false;
                        view.Show(GenVoicePhase.Idle, "Beleza — tô tocando Enchanted Valley na sala.");
                        view.transform.position = CameraLook(director) + Vector3.up * 0.2f;
                        view.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    }
                    Capture(CameraLook(director) + new Vector3(0f, 0.15f, -1.4f), CameraLook(director) + Vector3.up * 0.15f, 32f, "artifacts/28-gen-crow.png");
                    SetPhase(6);
                    break;
                case 6:
                    if (Elapsed() < 0.4f)
                        return;
                    var tv = GameObject.Find("TvStand");
                    var look = tv != null ? tv.transform.position + Vector3.up * 0.5f : CameraLook(director);
                    Capture(look + new Vector3(0.4f, 0.35f, -1.4f), look, 40f, "artifacts/29-music-tv.png");
                    Finish();
                    break;
            }
        }

        static Vector3 CameraLook(ProfileDemoDirector director)
        {
            var house = director.VisualHouse != null ? director.VisualHouse.transform : director.transform;
            return house.TransformPoint(HouseSpawnLogic.LocalFeet) + Vector3.up * 1.4f;
        }

        static void SpawnShot(ProfileDemoDirector director, string path)
        {
            var house = director.VisualHouse != null ? director.VisualHouse.transform : director.transform;
            HouseSpawn.Align(house);
            var feet = house.TransformPoint(HouseSpawnLogic.LocalFeet);
            var look = house.TransformPoint(HouseSpawnLogic.LocalLook);
            Capture(feet + Vector3.up * 1.5f, look + Vector3.up * 0.4f, 55f, path);
        }

        static void Capture(Vector3 from, Vector3 look, float fov, string relative)
        {
            Directory.CreateDirectory("artifacts");
            var go = new GameObject("VfCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
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
            File.WriteAllBytes(relative, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Debug.Log("[VerifyShots] " + relative);
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
