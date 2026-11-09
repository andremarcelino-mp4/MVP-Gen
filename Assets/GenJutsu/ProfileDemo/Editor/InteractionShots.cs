using System.IO;
using GenJutsu.Gen;
using GenJutsu.GenCore;
using GenJutsu.HouseState;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    [InitializeOnLoad]
    static class InteractionShots
    {
        const string Marker = "Temp/interaction-shots.on";
        const string PhaseKey = "GenJutsu.IxPhase";
        const string TimeKey = "GenJutsu.IxT";

        static InteractionShots()
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
                    var view = Object.FindAnyObjectByType<GenStatusView>();
                    if (view != null)
                    {
                        view.FollowHead = false;
                        view.Show(GenVoicePhase.Idle, "Pronto — pintei a Luz Sala de vermelho. Quer que eu toque uma música zen?");
                    }
                    PanelShot(director, "artifacts/20-gen-panel.png");
                    var listener = Object.FindAnyObjectByType<HouseStateListener>();
                    if (listener != null)
                        listener.Ingest("{\"state\":{\"session_id\":\"ACO004\",\"version\":12,\"media\":{\"type\":\"video\",\"url\":\"https://example.com/forest.mp4\",\"title\":\"vídeo pedido\",\"playing\":true,\"device\":\"tv\"},\"devices\":{\"tv\":{\"on\":true,\"color\":\"#FFFFFF\",\"intensity\":1}}}}");
                    SetPhase(3);
                    break;
                case 3:
                    if (Elapsed() < 0.8f)
                        return;
                    var tv = Object.FindAnyObjectByType<TvSet>();
                    if (tv != null)
                        tv.PlayMiniGame();
                    TvShot(director, "artifacts/21-tv-playing.png");
                    BallShot(director, "artifacts/22-grab-ball.png");
                    Finish();
                    break;
            }
        }

        static void PanelShot(ProfileDemoDirector director, string path)
        {
            var view = Object.FindAnyObjectByType<GenStatusView>();
            var look = director.transform.root.position + new Vector3(0.4f, 1.35f, 0.2f);
            if (view != null)
            {
                view.transform.position = look;
                view.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                look = view.transform.position;
            }
            Capture(look + new Vector3(0f, 0.05f, -1.6f), look, 32f, path);
        }

        static void TvShot(ProfileDemoDirector director, string path)
        {
            var tv = GameObject.Find("TvStand");
            var look = tv != null ? tv.transform.position + Vector3.up * 0.5f : director.transform.root.position + Vector3.up * 0.8f;
            Capture(look + new Vector3(0.4f, 0.35f, -1.4f), look, 40f, path);
        }

        static void BallShot(ProfileDemoDirector director, string path)
        {
            var ball = GameObject.Find("Ball");
            var look = ball != null ? ball.transform.position : director.transform.root.position + new Vector3(0.2f, 0.3f, -0.3f);
            Capture(look + new Vector3(0.4f, 0.45f, -0.7f), look, 45f, path);
        }

        static void Capture(Vector3 from, Vector3 look, float fov, string relative)
        {
            Directory.CreateDirectory("artifacts");
            var go = new GameObject("IxCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
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
            File.WriteAllBytes(relative, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Debug.Log("[IxShots] " + relative);
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
