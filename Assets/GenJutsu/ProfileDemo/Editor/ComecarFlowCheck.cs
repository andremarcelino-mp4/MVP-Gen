using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using GenJutsu.GenCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// QA v3.6 em PlayMode (batchmode): aperta o COMEÇAR de verdade (evento da placa),
    /// entra nas 4 casas, compara cada móvel pegável com a posição de projeto da cena,
    /// bagunça tudo e confere se volta ao lugar numa sessão nova. Sem rede: sessões falsas via Ingest.
    /// Uso: -executeMethod GenJutsu.ProfileDemo.EditorTools.ComecarFlowCheck.Run (sem -quit).
    /// </summary>
    [InitializeOnLoad]
    public static class ComecarFlowCheck
    {
        public const string Marker = "Temp/comecar-check.on";
        const string DesignFile = "Temp/comecar-design.txt";
        const string OutDir = "artifacts/v3.6";
        const float Tolerance = 0.03f;

        struct Step
        {
            public string session;
            public ProfileId profile;
            public bool shots;
        }

        static readonly Step[] Steps =
        {
            new Step { session = null, profile = ProfileId.Zen, shots = true },
            new Step { session = "QA36C", profile = ProfileId.Cyber, shots = true },
            new Step { session = "QA36E", profile = ProfileId.Excentrico, shots = true },
            new Step { session = "QA36A", profile = ProfileId.Acolhedor, shots = true },
            new Step { session = "QA36Z", profile = ProfileId.Zen, shots = false },
            new Step { session = "QA36C2", profile = ProfileId.Cyber, shots = false },
        };

        static int step;
        static int sub;
        static double t0;
        static bool retried;
        static bool failed;
        static readonly StringBuilder log = new StringBuilder();
        static Dictionary<string, Vector3> design;

        static ComecarFlowCheck()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Directory.CreateDirectory("Temp");
            Directory.CreateDirectory(OutDir);
            var sb = new StringBuilder();
            foreach (var look in Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include))
            {
                var house = HouseOf(look);
                foreach (var t in Tracked(house))
                {
                    var p = house.InverseTransformPoint(t.position);
                    sb.Append(look.profile).Append('|').Append(Key(house, t)).Append('|')
                        .Append(p.x.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(p.y.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(p.z.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                }
            }

            File.WriteAllText(DesignFile, sb.ToString());
            File.WriteAllText(Marker, "on");
            EditorApplication.isPlaying = true;
        }

        /// <summary>Grava a pose de projeto (agora serializada) de todo BoundsReset e salva a cena.</summary>
        public static void CaptureHomes()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var b in root.GetComponentsInChildren<BoundsReset>(true))
                {
                    b.Capture();
                    EditorUtility.SetDirty(b);
                    n++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(OutDir + "/capture-homes.txt", "boundsReset=" + n);
            Debug.Log("[ComecarCheck] capture-homes boundsReset=" + n);
        }

        static void OnPlay(PlayModeStateChange change)
        {
            if (!File.Exists(Marker))
                return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                step = 0;
                sub = -1;
                t0 = EditorApplication.timeSinceStartup;
                log.Clear();
                failed = false;
                design = LoadDesign();
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        static double Elapsed => EditorApplication.timeSinceStartup - t0;

        static void Mark()
        {
            t0 = EditorApplication.timeSinceStartup;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            var director = Object.FindFirstObjectByType<ProfileDemoDirector>();
            var gate = Object.FindFirstObjectByType<SessionGate>();
            var dojo = Object.FindFirstObjectByType<DojoDirector>();
            if (director == null || gate == null || dojo == null)
            {
                if (Elapsed > 30)
                    Finish("diretor/gate nao encontrado");
                return;
            }

            if (sub == -1)
            {
                if (Elapsed < 2.0)
                    return;
                gate.suppressNetworkPoll = true;
                Gen.GenHistoryClient.Muted = true;
                var ev = typeof(SessionGate).GetField("NamedWelcome", BindingFlags.Instance | BindingFlags.NonPublic);
                if (ev != null)
                    ev.SetValue(gate, null);
                GuestSession.Save("", "", null);
                Line("inicio: estado=" + director.Current + " prontoSemSessao=" + dojo.ReadyToStart + " temSessao=" + gate.HasSession);
                sub = 0;
                Mark();
                return;
            }

            if (step >= Steps.Length)
            {
                Finish(null);
                return;
            }

            var s = Steps[step];
            switch (sub)
            {
                case 0:
                    if (s.session != null)
                        gate.Ingest("{\"session\":{\"id\":\"" + s.session + "\",\"session_code\":\"" + s.session + "\",\"display_name\":\"QA\",\"profile_id\":\"" + Profiles.Get(s.profile).Key + "\",\"status\":\"active\"}}", 200, true);
                    sub = 1;
                    Mark();
                    break;
                case 1:
                    if (director.Current != ProfileDemoDirector.State.WhiteSpace)
                    {
                        if (director.Current == ProfileDemoDirector.State.InHouse && Elapsed > 2)
                            director.ReturnToWhiteSpace();
                        if (Elapsed > 20)
                        {
                            Fail("passo " + step + ": nao voltou ao branco");
                            step++;
                            sub = 0;
                        }

                        return;
                    }

                    if (Elapsed < 0.6)
                        return;
                    var pressed = PressComecar();
                    Line("passo " + step + " (" + (s.session ?? "sem sessao") + ", espera " + s.profile + "): COMECAR apertado, aceitou=" + pressed);
                    sub = 2;
                    Mark();
                    break;
                case 2:
                    if (director.Current == ProfileDemoDirector.State.InHouse)
                    {
                        if (Elapsed < 0.8)
                            return;
                        Line("passo " + step + ": entrou na casa " + director.Active + " em " + Elapsed.ToString("0.0") + "s, fonte=" + dojo.LastStartSourceSafe());
                        if (director.Active != s.profile)
                            Fail("passo " + step + ": casa errada " + director.Active + " (esperado " + s.profile + ")");
                        CheckHouse(director.Active, step);
                        if (s.shots)
                            Shots(director.Active);
                        Scramble(director.Active);
                        step++;
                        sub = 0;
                        retried = false;
                        Mark();
                        return;
                    }

                    if (Elapsed > 15)
                    {
                        Fail("passo " + step + ": COMECAR nao entrou na sala em 15s (estado=" + director.Current + ", prontoSessao=" + dojo.ReadyToStart + ")");
                        if (s.session == null && !retried)
                        {
                            retried = true;
                            Steps[step].session = "QA36Z0";
                            Line("passo " + step + ": repetindo com sessao Zen para medir os moveis");
                        }
                        else
                            step++;
                        sub = 0;
                        Mark();
                    }

                    break;
            }
        }

        static string LastStartSourceSafe(this DojoDirector dojo)
        {
            var p = typeof(DojoDirector).GetProperty("LastStartSource");
            return p != null ? (string)p.GetValue(dojo) : "(n/a)";
        }

        static bool PressComecar()
        {
            var plaque = Object.FindAnyObjectByType<StartPlaque>();
            if (plaque == null)
            {
                Fail("placa COMECAR nao encontrada");
                return false;
            }

            var simple = plaque.GetComponentInChildren<XRSimpleInteractable>(true);
            if (simple == null)
            {
                Fail("placa sem XRSimpleInteractable");
                return false;
            }

            var dojo = Object.FindFirstObjectByType<DojoDirector>();
            simple.selectEntered.Invoke(new SelectEnterEventArgs());
            return dojo != null;
        }

        static void CheckHouse(ProfileId id, int stepIndex)
        {
            var look = LookFor(id);
            if (look == null)
            {
                Fail("casa " + id + " nao encontrada");
                return;
            }

            var house = HouseOf(look);
            var items = Tracked(house);
            int compared = 0, bad = 0, extra = 0;
            var maxErr = 0f;
            var worst = new List<string>();
            var now = new List<KeyValuePair<string, Vector3>>();
            foreach (var t in items)
            {
                if (!t.gameObject.activeInHierarchy)
                    continue;
                var key = Key(house, t);
                var p = house.InverseTransformPoint(t.position);
                now.Add(new KeyValuePair<string, Vector3>(key, p));
                if (!design.TryGetValue(id + "|" + key, out var want))
                {
                    extra++;
                    continue;
                }

                compared++;
                var e = Vector3.Distance(p, want);
                if (e > maxErr)
                    maxErr = e;
                if (e > Tolerance)
                {
                    bad++;
                    if (worst.Count < 6)
                        worst.Add(t.name + " erro=" + e.ToString("0.000") + "m em " + p.ToString("F2") + " (projeto " + want.ToString("F2") + ")" + (worst.Count == 0 ? Chain(t, house) : ""));
                }
            }

            var piles = 0;
            for (var i = 0; i < now.Count; i++)
            {
                for (var j = i + 1; j < now.Count; j++)
                {
                    if (Vector3.Distance(now[i].Value, now[j].Value) >= 0.02f)
                        continue;
                    var di = design.TryGetValue(id + "|" + now[i].Key, out var a);
                    var dj = design.TryGetValue(id + "|" + now[j].Key, out var b);
                    if (di && dj && Vector3.Distance(a, b) < 0.02f)
                        continue;
                    piles++;
                }
            }

            var ok = bad == 0 && piles == 0 && compared > 0;
            Line("  " + id + ": pegaveis comparados=" + compared + " fora_do_lugar=" + bad + " erro_max=" + maxErr.ToString("0.0000") + "m amontoados(pares<2cm)=" + piles + " sem_projeto=" + extra + (ok ? " OK" : " FALHOU"));
            foreach (var w in worst)
                Line("    " + w);
            if (!ok)
                failed = true;
        }

        static string Chain(Transform t, Transform house)
        {
            var sb = new StringBuilder(" cadeia:");
            var pose = t.GetComponent<ResettablePose>();
            if (pose != null)
                sb.Append(" poseHome=" + house.InverseTransformPoint(pose.HomePosition).ToString("F2"));
            var br = t.GetComponent<BoundsReset>();
            if (br != null)
                sb.Append(" brCaptured=" + br.Captured + " brLocal=" + br.HomeLocal.ToString("F2"));
            for (var c = t; c != null && c != house.parent; c = c.parent)
                sb.Append(" | " + c.name + " lp=" + c.localPosition.ToString("F2") + " ls=" + c.localScale.ToString("F2") + " act=" + c.gameObject.activeSelf);
            return sb.ToString();
        }

        static void Scramble(ProfileId id)
        {
            var look = LookFor(id);
            if (look == null)
                return;
            var house = HouseOf(look);
            var pile = house.TransformPoint(new Vector3(0f, 0.9f, 0f));
            var n = 0;
            foreach (var t in Tracked(house))
            {
                t.position = pile + Random.insideUnitSphere * 0.05f;
                t.rotation = Random.rotation;
                n++;
            }

            Line("  " + id + ": baguncei " + n + " pegaveis num monte so (teste de reset na proxima sessao)");
        }

        static void Shots(ProfileId id)
        {
            var look = LookFor(id);
            if (look == null)
                return;
            HideRigHands();
            var h = HouseOf(look);
            var name = id.ToString().ToLowerInvariant();
            var feet = h.TransformPoint(HouseSpawnLogic.LocalFeet);
            var tv = h.TransformPoint(HouseSpawnLogic.LocalLook);
            Capture(feet + Vector3.up * 1.6f, tv + Vector3.up * 0.45f, 62f, OutDir + "/" + name + "-entrada.png");
            Capture(h.TransformPoint(new Vector3(1.85f, 2.15f, 0.3f)), h.TransformPoint(new Vector3(-0.3f, 0.45f, 0.55f)), 56f, OutDir + "/" + name + "-sala.png");
        }

        static void HideRigHands()
        {
            var origin = HouseSpawn.FindOrigin();
            if (origin == null)
                return;
            foreach (var t in origin.root.GetComponentsInChildren<Transform>(false))
            {
                if (t.name.Contains("Controller") || t.name.Contains("Hand"))
                    t.gameObject.SetActive(false);
            }

            var voice = Object.FindAnyObjectByType<Gen.GenStatusView>();
            if (voice != null)
                voice.gameObject.SetActive(false);
        }

        static void Capture(Vector3 from, Vector3 look, float fov, string path)
        {
            var go = new GameObject("ComecarShotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 40f;
            var main = Camera.main;
            if (main != null)
            {
                cam.clearFlags = main.clearFlags;
                cam.backgroundColor = main.backgroundColor;
            }

            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            const int w = 1600;
            const int hgt = 1000;
            var rt = new RenderTexture(w, hgt, 24);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, hgt, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, hgt), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = prev;
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
            Line("  print " + path);
        }

        static ProfileLook LookFor(ProfileId id)
        {
            foreach (var look in Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include))
            {
                if (look.profile == id)
                    return look;
            }

            return null;
        }

        static Transform HouseOf(ProfileLook look)
        {
            return look.transition != null ? look.transition.transform : look.transform;
        }

        static List<Transform> Tracked(Transform house)
        {
            var list = new List<Transform>();
            foreach (var t in house.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<BoundsReset>() != null || t.GetComponent<GrabRest>() != null || t.GetComponent<ResettablePose>() != null)
                    list.Add(t);
            }

            return list;
        }

        static string Key(Transform house, Transform t)
        {
            var parts = new List<string>();
            var cur = t;
            while (cur != null && cur != house)
            {
                parts.Add(cur.name + "#" + cur.GetSiblingIndex());
                cur = cur.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        static Dictionary<string, Vector3> LoadDesign()
        {
            var d = new Dictionary<string, Vector3>();
            if (!File.Exists(DesignFile))
                return d;
            foreach (var raw in File.ReadAllLines(DesignFile))
            {
                var f = raw.Split('|');
                if (f.Length != 5)
                    continue;
                d[f[0] + "|" + f[1]] = new Vector3(float.Parse(f[2], CultureInfo.InvariantCulture), float.Parse(f[3], CultureInfo.InvariantCulture), float.Parse(f[4], CultureInfo.InvariantCulture));
            }

            return d;
        }

        static void Line(string s)
        {
            log.Append(s).Append('\n');
            Debug.Log("[ComecarCheck] " + s);
        }

        static void Fail(string s)
        {
            failed = true;
            Line("FALHA: " + s);
        }

        static void Finish(string error)
        {
            EditorApplication.update -= Tick;
            if (error != null)
                Fail(error);
            Line(failed ? "RESULTADO: FALHOU" : "RESULTADO: OK");
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(OutDir + "/comecar-check.txt", log.ToString());
            if (File.Exists(Marker))
                File.Delete(Marker);
            var code = failed ? 1 : 0;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
}
