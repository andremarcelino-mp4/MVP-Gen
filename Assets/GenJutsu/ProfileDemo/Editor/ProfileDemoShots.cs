using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// Edit/Play capture of the white hub, the welcome card, the four dojos and the return after session end.
    /// Armed by a marker file so the state survives the play-mode domain reload.
    /// </summary>
    [InitializeOnLoad]
    public static class ProfileDemoShots
    {
        const string Marker = "Temp/dojo-shots.marker";
        const string PhaseKey = "GenJutsu.ShotPhase";
        const string TimeKey = "GenJutsu.ShotT";
        const string FolderKey = "GenJutsu.ShotFolder";

        static ProfileDemoShots()
        {
            if (File.Exists(Marker))
                EditorApplication.update += Tick;
        }

        public static void Capture()
        {
            var folder = "/opt/cursor/artifacts";
            Directory.CreateDirectory(folder);
            EditorPrefs.SetString(FolderKey, folder);
            EditorPrefs.SetInt(PhaseKey, 1);
            EditorPrefs.SetFloat(TimeKey, 0f);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(Marker, folder);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Debug.Log("[DojoShots] " + ProfileDemoBuilder.Build());
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            var phase = EditorPrefs.GetInt(PhaseKey, 0);
            if (phase <= 0)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return;
            if (EditorApplication.isPaused)
                return;

            var director = Object.FindFirstObjectByType<ProfileDemoDirector>();
            var gate = Object.FindFirstObjectByType<SessionGate>();
            if (director == null || gate == null)
            {
                if (Elapsed() > 20f)
                    Fail("director or gate missing");
                return;
            }

            switch (phase)
            {
                case 1:
                    gate.suppressNetworkPoll = true;
                    SetPhase(2);
                    break;
                case 2:
                    SaveRoom(director, "hub-branco.png", true);
                    SetPhase(3);
                    break;
                case 3:
                    gate.Ingest(Body("ZEN001", "André", "Zen"), 200, true);
                    SetPhase(4);
                    break;
                case 4:
                    SetPhase(5);
                    break;
                case 5:
                    if (!WaitHouse(director, "dojo-z.png"))
                        return;
                    SaveCard(gate, "boas-vindas.png");
                    SetPhase(6);
                    break;
                case 6:
                    gate.Ingest("{\"session\":null}", 200, true);
                    SetPhase(7);
                    break;
                case 7:
                    if (!WaitWhite(director, "volta-branco.png", true))
                        return;
                    SetPhase(8);
                    break;
                case 8:
                    gate.Ingest(Body("CYB002", "André", "Cyber"), 200, true);
                    SetPhase(9);
                    break;
                case 9:
                    if (!WaitHouse(director, "dojo-c.png"))
                        return;
                    SetPhase(10);
                    break;
                case 10:
                    gate.Ingest(Body("EXC003", "André", "Excentrico"), 200, true);
                    SetPhase(11);
                    break;
                case 11:
                    if (!WaitHouse(director, "dojo-e.png"))
                        return;
                    SetPhase(12);
                    break;
                case 12:
                    gate.Ingest(Body("ACO004", "André", "Acolhedor"), 200, true);
                    SetPhase(13);
                    break;
                case 13:
                    if (!WaitHouse(director, "dojo-a.png"))
                        return;
                    Finish();
                    break;
            }
        }

        static bool WaitHouse(ProfileDemoDirector director, string file)
        {
            if (director.Current == ProfileDemoDirector.State.InHouse && Elapsed() > 0.4f)
            {
                SaveRoom(director, file, false);
                return true;
            }

            if (Elapsed() > 14f)
            {
                Debug.LogWarning("[DojoShots] timeout before " + file + " state=" + director.Current);
                SaveRoom(director, file, false);
                return true;
            }

            return false;
        }

        static bool WaitWhite(ProfileDemoDirector director, string file, bool showCard)
        {
            if (director.Current == ProfileDemoDirector.State.WhiteSpace && Elapsed() > 0.45f)
            {
                SaveRoom(director, file, showCard);
                return true;
            }

            if (Elapsed() > 14f)
            {
                Debug.LogWarning("[DojoShots] timeout before " + file + " state=" + director.Current);
                SaveRoom(director, file, showCard);
                return true;
            }

            return false;
        }

        static void SaveRoom(ProfileDemoDirector director, string file, bool showCard)
        {
            var gate = Object.FindFirstObjectByType<SessionGate>();
            if (gate != null && gate.group != null)
                gate.group.alpha = showCard ? 1f : 0f;
            var focus = director.transform.root.position + Vector3.up * 0.7f;
            var from = focus + new Vector3(5.1f, 4.35f, -5.1f);
            Save(from, focus, 36f, file);
            if (gate != null && gate.group != null)
                gate.group.alpha = 1f;
        }

        static void SaveCard(SessionGate gate, string file)
        {
            if (gate == null)
                return;
            if (gate.group != null)
                gate.group.alpha = 1f;
            var card = gate.transform;
            var from = card.position + card.forward * 1.15f + Vector3.up * 0.02f;
            Save(from, card.position, 32f, file);
        }

        static void Save(Vector3 from, Vector3 look, float fov, string file)
        {
            var folder = EditorPrefs.GetString(FolderKey, "/opt/cursor/artifacts");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, file);
            var go = new GameObject("DojoShotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.44f, 0.47f);
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
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
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = prev;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Debug.Log("[DojoShots] wrote " + path + " (" + new FileInfo(path).Length + " bytes)");
        }

        static string Body(string code, string name, string profile)
        {
            return "{\"session\":{\"id\":\"" + code + "\",\"session_code\":\"" + code + "\",\"display_name\":\"" + name + "\",\"profile_id\":\"" + profile + "\",\"status\":\"active\"}}";
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

        static void Fail(string why)
        {
            Debug.LogError("[DojoShots] " + why);
            Finish();
        }

        static void Finish()
        {
            EditorPrefs.SetInt(PhaseKey, 0);
            EditorApplication.update -= Tick;
            if (File.Exists(Marker))
                File.Delete(Marker);
            Debug.Log("[DojoShots] done");
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        }
    }
}
