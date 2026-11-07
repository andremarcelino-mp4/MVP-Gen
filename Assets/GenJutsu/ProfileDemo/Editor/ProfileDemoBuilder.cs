using System.Collections.Generic;
using GenJutsu.TableProps.EditorTools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// Builds the four-profile demo around the white table: tokens, houses, UI, post-processing and the director.
    /// Legacy table props are only deactivated, never deleted.
    /// </summary>
    public static partial class ProfileDemoBuilder
    {
        const string RootName = "ProfileDemo";
        const string TableSetName = "JapaneseTableSet";
        const string Folder = "Assets/GenJutsu/ProfileDemo";
        const string MaterialFolder = Folder + "/Materials";
        const string VolumeFolder = Folder + "/Volumes";

        static readonly string[] LegacyProps = { "Teapot", "TeaCup_1", "TeaCup_2", "Shuriken", "Brush", "JadeDragon", "FloatingTarget" };

        // House-local frame: the table sits at the origin and the user stands at about (0, -0.77) facing +Z.
        static readonly Vector3 Viewer = new Vector3(0f, 1.5f, -0.6f);
        static readonly Vector3 StationLeft = new Vector3(-1.9f, 0f, 0.3f);
        static readonly Vector3 StationRight = new Vector3(1.9f, 0f, 0.3f);
        static readonly Vector3 StationFront = new Vector3(0f, 0f, 2.2f);
        const float HalfExtent = 4.5f;

        static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        static TMP_FontAsset font;
        static Texture2D softDot;

        [MenuItem("GenJutsu/Profile Demo/Build Demo")]
        static void MenuBuild()
        {
            Debug.Log("[ProfileDemo] " + Build());
        }

        [MenuItem("GenJutsu/Profile Demo/Show Legacy Props")]
        static void MenuShowLegacy()
        {
            Debug.Log("[ProfileDemo] " + SetLegacyProps(true));
        }

        [MenuItem("GenJutsu/Profile Demo/Hide Legacy Props")]
        static void MenuHideLegacy()
        {
            Debug.Log("[ProfileDemo] " + SetLegacyProps(false));
        }

        public static string SetLegacyProps(bool visible)
        {
            var scene = SceneManager.GetActiveScene();
            var count = 0;
            foreach (var name in LegacyProps)
            {
                var go = FindInScene(scene, name);
                if (go == null || go.activeSelf == visible)
                    continue;
                go.SetActive(visible);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return (visible ? "Shown " : "Hidden ") + count + " legacy props";
        }

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Exit Play mode before building the demo.";

            var scene = SceneManager.GetActiveScene();
            var tableSet = FindInScene(scene, TableSetName);
            if (tableSet == null)
                return "JapaneseTableSet not found in " + scene.name + ". Run GenJutsu/Rebuild Japanese Table Set first.";

            MaterialCache.Clear();
            EnsureFolder(Folder, "Materials");
            EnsureFolder(Folder, "Volumes");
            font = LoadFont();
            softDot = LoadSoftDot();

            var existing = FindInScene(scene, RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            SetLegacyPropsNoSave(scene, false);

            var root = new GameObject(RootName);
            root.transform.SetPositionAndRotation(tableSet.transform.position, Quaternion.identity);

            var recorder = new GameObject("SessionRecorder").AddComponent<DemoSessionRecorder>();
            recorder.transform.SetParent(root.transform, false);
            var music = new GameObject("Music").AddComponent<ProceduralMusic>();
            music.transform.SetParent(root.transform, false);

            var results = BuildResultsBoard(root.transform);
            results.gameObject.SetActive(false);
            var volumes = BuildVolumes(root.transform);
            var gate = BuildSessionGate(root.transform);

            var shellGo = new GameObject("DojoShell");
            shellGo.transform.SetParent(root.transform, false);
            var shell = shellGo.AddComponent<DojoShell>();
            var looks = new ProfileLook[Profiles.Count];
            var houses = new HouseTransition[Profiles.Count];
            houses[(int)ProfileId.Zen] = BuildZenHouse(shellGo.transform);
            houses[(int)ProfileId.Cyber] = BuildCyberHouse(shellGo.transform);
            houses[(int)ProfileId.Excentrico] = BuildEccentricHouse(shellGo.transform);
            houses[(int)ProfileId.Acolhedor] = BuildCozyHouse(shellGo.transform);
            for (var i = 0; i < Profiles.Count; i++)
            {
                var house = houses[i];
                house.gameObject.SetActive(false);
                house.comfortEase = true;
                var look = house.gameObject.AddComponent<ProfileLook>();
                look.profile = (ProfileId)i;
                look.transition = house;
                look.volume = volumes[i];
                looks[i] = look;
            }

            shell.looks = looks;

            var directorGo = new GameObject("Director");
            directorGo.transform.SetParent(root.transform, false);
            var director = directorGo.AddComponent<ProfileDemoDirector>();
            var morph = directorGo.AddComponent<ProfileMorph>();
            morph.duration = 2.6f;
            var api = directorGo.AddComponent<SessionApiClient>();
            gate.client = api;
            var hub = directorGo.AddComponent<HubPropsDissolve>();
            hub.duration = 1.35f;
            hub.roots = CollectHubRoots(scene, tableSet);
            hub.sparkles = BuildHubSparkles(root.transform);
            var gaze = directorGo.AddComponent<GazePublisher>();
            var dojo = directorGo.AddComponent<DojoDirector>();
            dojo.demo = director;
            dojo.gate = gate;
            dojo.shell = shell;
            dojo.morph = morph;
            dojo.hubDissolve = hub;
            dojo.gaze = gaze;

            director.houses = houses;
            director.volumes = volumes;
            director.shell = shell;
            director.morph = morph;
            director.hubDissolve = hub;
            director.sessionGate = gate;
            director.music = music;
            director.caption = null;
            director.results = results;
            director.recorder = recorder;
            director.whiteSpaceRenderers = CollectWhiteSpaceRenderers(scene);
            director.whiteSpaceOnly = System.Array.Empty<GameObject>();
            director.mainLight = FindMainLight();
            var tableGroup = tableSet.transform.Find("TableGroup");
            if (tableGroup != null)
            {
                director.tableBody = tableGroup.GetComponent<Rigidbody>();
                director.tableGrab = tableGroup.GetComponent<XRGrabInteractable>();
            }

            var cameraNote = PrepareMainCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Built ProfileDemo ZCEA hub: SessionGate + DojoShell + 4 looks, mesa/pergaminho dissolve, volumes " +
                   volumes.Length + ", white-space renderers " +
                   director.whiteSpaceRenderers.Length + ", main light " + (director.mainLight != null ? director.mainLight.name : "none") +
                   ", " + cameraNote;
        }

        static void SetLegacyPropsNoSave(Scene scene, bool visible)
        {
            foreach (var name in LegacyProps)
            {
                var go = FindInScene(scene, name);
                if (go != null)
                    go.SetActive(visible);
            }
        }

        static GameObject[] CollectHubRoots(Scene scene, GameObject tableSet)
        {
            var list = new List<GameObject>();
            if (tableSet != null)
                list.Add(tableSet);
            var scroll = FindInScene(scene, "Scroll");
            if (scroll != null)
            {
                scroll.SetActive(true);
                list.Add(scroll);
            }

            var hanging = FindInScene(scene, "HangingScroll");
            if (hanging != null)
                list.Add(hanging);
            return list.ToArray();
        }

        static ParticleSystem BuildHubSparkles(Transform root)
        {
            var mat = ParticleMat("HubDissolveSpark", new Color(0.95f, 0.92f, 0.8f, 0.9f), true);
            var ps = Particles(root, "HubDissolveVfx", new Vector3(0f, 0.7f, 0f), Quaternion.Euler(-90f, 0f, 0f), mat);
            var main = ps.main;
            main.duration = 1.4f;
            main.loop = false;
            main.startLifetime = 0.9f;
            main.startSpeed = 0.25f;
            main.startSize = 0.04f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.9f, 0.2f, 0.6f);
            return ps;
        }

        static SessionGate BuildSessionGate(Transform root)
        {
            var canvas = WorldCanvas("SessionGate", root, new Vector2(920f, 360f), 0.00115f);
            canvas.transform.localPosition = new Vector3(0f, 1.15f, 0.55f);
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var xrRay = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRay != null)
                canvas.gameObject.AddComponent(xrRay);

            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var rect = (RectTransform)canvas.transform;
            UIImage(rect, "Background", new Color(0.97f, 0.97f, 0.98f, 0.94f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var title = UIText(rect, "Title", "Bem-vindo", 48f, new Color(0.16f, 0.18f, 0.22f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -100f), new Vector2(-36f, -24f), TextAlignmentOptions.BottomLeft);
            title.fontStyle = FontStyles.Bold;
            var status = UIText(rect, "Status", "", 28f, new Color(0.35f, 0.38f, 0.44f), new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(36f, 28f), new Vector2(-36f, -8f), TextAlignmentOptions.TopLeft);
            status.textWrappingMode = TextWrappingModes.Normal;
            status.raycastTarget = false;

            var gate = canvas.gameObject.AddComponent<SessionGate>();
            gate.title = title;
            gate.status = status;
            gate.group = group;
            return gate;
        }

        static Renderer[] CollectWhiteSpaceRenderers(Scene scene)
        {
            var list = new List<Renderer>();
            var ground = FindInScene(scene, "BackdropGround");
            if (ground != null)
                list.AddRange(ground.GetComponentsInChildren<Renderer>(true));
            var environment = FindInScene(scene, "Environment");
            var grid = environment != null ? environment.transform.Find("Grid") : null;
            if (grid != null)
                list.AddRange(grid.GetComponentsInChildren<Renderer>(true));
            return list.ToArray();
        }

        static Light FindMainLight()
        {
            if (RenderSettings.sun != null)
                return RenderSettings.sun;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                    return light;
            }

            return null;
        }

        static string PrepareMainCamera()
        {
            var cam = Camera.main;
            if (cam == null)
                return "no Main Camera found";
            var data = cam.GetUniversalAdditionalCameraData();
            data.volumeLayerMask |= 1 << 0;
            EditorUtility.SetDirty(data);
            return "camera " + cam.name + " volume mask includes Default";
        }

        // ---------------------------------------------------------------- tokens on the table

        static void BuildTokens(Transform root)
        {
            var tokens = new GameObject("TableTokens").transform;
            tokens.SetParent(root, false);
            var xs = new[] { -0.39f, -0.13f, 0.13f, 0.39f };
            for (var i = 0; i < Profiles.Count; i++)
            {
                var id = (ProfileId)i;
                var info = Profiles.Get(id);
                var token = new GameObject("Token_" + info.Key);
                token.transform.SetParent(tokens, false);
                token.transform.localPosition = new Vector3(xs[i], 0.655f, -0.08f);
                switch (id)
                {
                    case ProfileId.Zen:
                        BuildZenToken(token.transform);
                        break;
                    case ProfileId.Cyber:
                        BuildCyberToken(token.transform);
                        break;
                    case ProfileId.Excentrico:
                        BuildEccentricToken(token.transform);
                        break;
                    default:
                        BuildCozyToken(token.transform);
                        break;
                }

                StripColliders(token);
                var box = token.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.045f, 0f);
                box.size = new Vector3(0.1f, 0.09f, 0.1f);
                JapaneseTableBuilder.MakeGrabbable(token, 0.3f);
                var grab = token.GetComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
                var body = token.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                var tokenComp = token.AddComponent<ProfileToken>();
                tokenComp.profile = id;
                tokenComp.opensHouse = false;

                var plaque = Prim(PrimitiveType.Cube, "Plaque_" + info.Letter, tokens, new Vector3(xs[i], 0.665f, -0.24f), new Vector3(0.09f, 0.012f, 0.045f), Mat("PlaqueWhite", new Color(0.98f, 0.98f, 0.98f), 0.6f));
                plaque.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                StripColliders(plaque);
                var letter = Label3D(plaque.transform.parent, info.Letter + "  <size=45%>" + info.Title + "</size>", new Vector3(xs[i], 0.6725f, -0.244f), 0.22f, info.Accent * 0.75f, new Vector2(0.09f, 0.04f));
                letter.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
                letter.fontStyle = FontStyles.Bold;
            }
        }

        static void BuildZenToken(Transform t)
        {
            var sand = Mat("ZenSandToken", new Color(0.88f, 0.82f, 0.7f), 0.1f);
            Prim(PrimitiveType.Cylinder, "Sand", t, new Vector3(0f, 0.006f, 0f), new Vector3(0.095f, 0.006f, 0.095f), sand);
            var stone = Prim(PrimitiveType.Sphere, "Stone", t, new Vector3(0.018f, 0.024f, 0.012f), new Vector3(0.045f, 0.026f, 0.038f), Mat("ZenStone", new Color(0.32f, 0.33f, 0.34f), 0.55f));
            stone.transform.localRotation = Quaternion.Euler(0f, 30f, 6f);
            Prim(PrimitiveType.Sphere, "StoneTop", t, new Vector3(0.016f, 0.043f, 0.012f), new Vector3(0.028f, 0.016f, 0.024f), Mat("ZenStoneLight", new Color(0.55f, 0.55f, 0.53f), 0.5f));
            var trunk = Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(-0.022f, 0.035f, -0.015f), new Vector3(0.008f, 0.028f, 0.008f), Mat("ZenBark", new Color(0.3f, 0.2f, 0.13f), 0.2f));
            trunk.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            var leaf = Mat("ZenLeaf", new Color(0.24f, 0.45f, 0.24f), 0.25f);
            Prim(PrimitiveType.Sphere, "Foliage", t, new Vector3(-0.03f, 0.07f, -0.015f), new Vector3(0.045f, 0.026f, 0.04f), leaf);
            Prim(PrimitiveType.Sphere, "Foliage2", t, new Vector3(-0.008f, 0.06f, -0.02f), new Vector3(0.032f, 0.02f, 0.03f), leaf);
        }

        static void BuildCyberToken(Transform t)
        {
            var dark = Mat("CyberChip", new Color(0.05f, 0.06f, 0.09f), 0.85f, 0.6f);
            var glow = Glow("CyberGlow", new Color(0.1f, 0.85f, 1f), 2.2f);
            var chip = Prim(PrimitiveType.Cube, "Chip", t, new Vector3(0f, 0.04f, 0f), new Vector3(0.06f, 0.06f, 0.06f), dark);
            chip.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Prim(PrimitiveType.Cube, "Core", t, new Vector3(0f, 0.071f, 0f), new Vector3(0.03f, 0.003f, 0.03f), glow).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            for (var i = 0; i < 4; i++)
            {
                var a = i * 90f + 45f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                var strip = Prim(PrimitiveType.Cube, "Trace" + i, t, new Vector3(0f, 0.04f, 0f) + dir * 0.0305f, new Vector3(0.04f, 0.004f, 0.002f), glow);
                strip.transform.localRotation = Quaternion.Euler(0f, a, 0f);
            }

            Prim(PrimitiveType.Cylinder, "Base", t, new Vector3(0f, 0.004f, 0f), new Vector3(0.09f, 0.004f, 0.09f), dark);
        }

        static void BuildEccentricToken(Transform t)
        {
            var colors = Palette();
            for (var i = 0; i < 9; i++)
            {
                var a = i * 40f;
                var r = 0.028f;
                var pos = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, 0.012f + i * 0.0085f, Mathf.Sin(a * Mathf.Deg2Rad) * r);
                var cube = Prim(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere, "Twist" + i, t, pos, Vector3.one * (0.026f - i * 0.0012f), Mat("Ecc" + (i % colors.Length), colors[i % colors.Length], 0.55f));
                cube.transform.localRotation = Quaternion.Euler(a * 0.6f, a, a * 0.3f);
            }

            Prim(PrimitiveType.Sphere, "Top", t, new Vector3(0f, 0.095f, 0f), Vector3.one * 0.018f, Glow("EccGlow", new Color(1f, 0.3f, 0.75f), 1.6f));
        }

        static void BuildCozyToken(Transform t)
        {
            var ceramic = Mat("CozyMug", new Color(0.86f, 0.42f, 0.28f), 0.7f);
            Prim(PrimitiveType.Cylinder, "Mug", t, new Vector3(0f, 0.03f, 0f), new Vector3(0.055f, 0.03f, 0.055f), ceramic);
            var handle = Prim(PrimitiveType.Capsule, "Handle", t, new Vector3(0.034f, 0.032f, 0f), new Vector3(0.012f, 0.018f, 0.012f), ceramic);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Prim(PrimitiveType.Cylinder, "Wax", t, new Vector3(0f, 0.062f, 0f), new Vector3(0.042f, 0.004f, 0.042f), Mat("CozyWax", new Color(0.98f, 0.93f, 0.8f), 0.3f));
            Prim(PrimitiveType.Cylinder, "Wick", t, new Vector3(0f, 0.07f, 0f), new Vector3(0.002f, 0.006f, 0.002f), Mat("CozyWick", new Color(0.1f, 0.08f, 0.06f)));
            var flame = Prim(PrimitiveType.Sphere, "Flame", t, new Vector3(0f, 0.085f, 0f), new Vector3(0.012f, 0.022f, 0.012f), Glow("CozyFlame", new Color(1f, 0.62f, 0.2f), 3f));
            var flicker = flame.AddComponent<FloatSpin>();
            flicker.bobHeight = 0.0015f;
            flicker.bobSpeed = 3f;
            flicker.spinSpeed = 40f;
        }

        // ---------------------------------------------------------------- UI

        static ProfileCaption BuildCaption(Transform root)
        {
            var canvas = WorldCanvas("ProfileCaption", root, new Vector2(1000f, 300f), 0.0011f);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var rect = (RectTransform)canvas.transform;
            UIImage(rect, "Background", new Color(0.06f, 0.07f, 0.09f, 0.82f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bar = UIImage(rect, "AccentBar", Color.white, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(16f, 0f));
            var title = UIText(rect, "Title", "Perfil", 64f, Color.white, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(48f, 0f), new Vector2(-32f, -18f), TextAlignmentOptions.BottomLeft);
            title.fontStyle = FontStyles.Bold;
            var body = UIText(rect, "Body", "", 34f, new Color(0.88f, 0.9f, 0.93f), new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(48f, 20f), new Vector2(-32f, -6f), TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;

            var caption = canvas.gameObject.AddComponent<ProfileCaption>();
            caption.group = group;
            caption.title = title;
            caption.body = body;
            caption.accentBar = bar;
            canvas.transform.localPosition = new Vector3(0f, 1.4f, 0.8f);
            return caption;
        }

        static ResultsBoard BuildResultsBoard(Transform root)
        {
            var canvas = WorldCanvas("ResultsBoard", root, new Vector2(900f, 1100f), 0.0011f);
            var pos = new Vector3(-1.25f, 1.35f, 0.15f);
            canvas.transform.localPosition = pos;
            canvas.transform.localRotation = ReadableFrom(pos, Viewer);
            var rect = (RectTransform)canvas.transform;
            UIImage(rect, "Background", new Color(0.98f, 0.98f, 0.99f, 0.94f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIImage(rect, "TopBar", new Color(0.2f, 0.24f, 0.3f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), Vector2.zero);
            var title = UIText(rect, "Title", "Sua jornada começa aqui", 56f, new Color(0.14f, 0.16f, 0.2f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -120f), new Vector2(-44f, -24f), TextAlignmentOptions.BottomLeft);
            title.fontStyle = FontStyles.Bold;
            var hint = UIText(rect, "Hint", "A mesa e o pergaminho orientam. Digite o código da sessão — o dojo assume o look Z, C, E ou A. Cubo branco: voltar ao neutro.", 28f, new Color(0.4f, 0.43f, 0.48f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -200f), new Vector2(-44f, -124f), TextAlignmentOptions.TopLeft);
            hint.textWrappingMode = TextWrappingModes.Normal;
            var body = UIText(rect, "Body", "", 34f, new Color(0.18f, 0.2f, 0.24f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(44f, 80f), new Vector2(-44f, -215f), TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.richText = true;
            var footer = UIText(rect, "Footer", "", 24f, new Color(0.5f, 0.52f, 0.56f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(44f, 20f), new Vector2(-44f, 70f), TextAlignmentOptions.BottomLeft);

            var stand = Prim(PrimitiveType.Cylinder, "Stand", root, new Vector3(pos.x, 0.35f, pos.z + 0.02f), new Vector3(0.05f, 0.35f, 0.05f), Mat("PlaqueWhite", Color.white, 0.6f));
            StripColliders(stand);
            stand.transform.SetParent(canvas.transform, true);

            var board = canvas.gameObject.AddComponent<ResultsBoard>();
            board.title = title;
            board.body = body;
            board.footer = footer;
            return board;
        }

        static void BuildHousePlaque(Transform parent, ProfileInfo info, Vector3 position, string[] activities)
        {
            var canvas = WorldCanvas("HousePlaque", parent, new Vector2(1300f, 460f), 0.0022f);
            canvas.transform.localPosition = position;
            canvas.transform.localRotation = ReadableFrom(position, Viewer);
            var rect = (RectTransform)canvas.transform;
            UIImage(rect, "Background", new Color(0.05f, 0.05f, 0.07f, 0.72f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIImage(rect, "Accent", info.Accent, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 12f));
            var title = UIText(rect, "Title", "<color=#" + ColorUtility.ToHtmlStringRGB(info.Accent) + ">" + info.Letter + "</color>  " + info.Title + " / " + info.Subtitle,
                76f, Color.white, new Vector2(0f, 0.62f), new Vector2(1f, 1f), new Vector2(40f, 0f), new Vector2(-40f, -16f), TextAlignmentOptions.BottomLeft);
            title.fontStyle = FontStyles.Bold;
            var caption = UIText(rect, "Caption", info.Caption, 32f, new Color(0.86f, 0.88f, 0.9f), new Vector2(0f, 0.36f), new Vector2(1f, 0.62f), new Vector2(40f, 0f), new Vector2(-40f, -6f), TextAlignmentOptions.TopLeft);
            caption.textWrappingMode = TextWrappingModes.Normal;
            var list = UIText(rect, "Activities", "• " + string.Join("     • ", activities) + "\n<size=80%><alpha=#AA>Câmera instantânea: pegue e espere 3 s ou aperte o gatilho  •  Cubo branco: voltar<alpha=#FF></size>",
                30f, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.36f), new Vector2(40f, 24f), new Vector2(-40f, 0f), TextAlignmentOptions.TopLeft);
            list.textWrappingMode = TextWrappingModes.Normal;
        }

        static Canvas WorldCanvas(string name, Transform parent, Vector2 size, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * scale;
            return canvas;
        }

        static Image UIImage(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI UIText(RectTransform parent, string name, string text, float size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        static TextMeshPro Label3D(Transform parent, string text, Vector3 localPos, float size, Color color, Vector2 box)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tmp = go.AddComponent<TextMeshPro>();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = box;
            return tmp;
        }

        /// <summary>Label readable from the viewer, placed in a station frame whose +Z faces the viewer.</summary>
        static TextMeshPro StationLabel(Transform station, string text, Vector3 localPos, float size, Color color)
        {
            var label = Label3D(station, text, localPos, size, color, new Vector2(1.2f, 0.2f));
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return label;
        }

        // ---------------------------------------------------------------- post-processing

        static Volume[] BuildVolumes(Transform root)
        {
            var parent = new GameObject("ProfileVolumes").transform;
            parent.SetParent(root, false);
            var volumes = new Volume[Profiles.Count];
            for (var i = 0; i < Profiles.Count; i++)
            {
                var id = (ProfileId)i;
                var info = Profiles.Get(id);
                var path = VolumeFolder + "/" + info.Key + "Volume.asset";
                AssetDatabase.DeleteAsset(path);
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = info.Key + "Volume";
                AssetDatabase.CreateAsset(profile, path);
                ConfigureProfile(id, profile);
                foreach (var component in profile.components)
                {
                    component.name = component.GetType().Name;
                    AssetDatabase.AddObjectToAsset(component, profile);
                }

                EditorUtility.SetDirty(profile);

                var go = new GameObject("Volume_" + info.Key);
                go.transform.SetParent(parent, false);
                go.layer = 0;
                var volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10f + i;
                volume.weight = 0f;
                volume.sharedProfile = profile;
                volumes[i] = volume;
            }

            AssetDatabase.SaveAssets();
            return volumes;
        }

        static void ConfigureProfile(ProfileId id, VolumeProfile profile)
        {
            switch (id)
            {
                case ProfileId.Zen:
                {
                    var color = profile.Add<ColorAdjustments>(true);
                    color.saturation.value = -18f;
                    color.contrast.value = -6f;
                    color.postExposure.value = 0.1f;
                    color.colorFilter.value = new Color(1f, 0.99f, 0.95f);
                    var vignette = profile.Add<Vignette>(true);
                    vignette.intensity.value = 0.28f;
                    vignette.smoothness.value = 0.5f;
                    vignette.color.value = new Color(0.12f, 0.16f, 0.12f);
                    vignette.center.value = new Vector2(0.5f, 0.5f);
                    vignette.rounded.value = false;
                    break;
                }
                case ProfileId.Cyber:
                {
                    var bloom = profile.Add<Bloom>(true);
                    bloom.threshold.value = 0.75f;
                    bloom.intensity.value = 1.3f;
                    bloom.scatter.value = 0.6f;
                    bloom.tint.value = new Color(0.7f, 0.95f, 1f);
                    var color = profile.Add<ColorAdjustments>(true);
                    color.contrast.value = 16f;
                    color.saturation.value = 8f;
                    color.postExposure.value = 0f;
                    color.colorFilter.value = new Color(0.86f, 0.96f, 1f);
                    var balance = profile.Add<WhiteBalance>(true);
                    balance.temperature.value = -28f;
                    balance.tint.value = 6f;
                    break;
                }
                case ProfileId.Excentrico:
                {
                    var color = profile.Add<ColorAdjustments>(true);
                    color.saturation.value = 38f;
                    color.contrast.value = 12f;
                    color.postExposure.value = 0.05f;
                    color.colorFilter.value = Color.white;
                    var bloom = profile.Add<Bloom>(true);
                    bloom.threshold.value = 0.9f;
                    bloom.intensity.value = 0.6f;
                    bloom.scatter.value = 0.65f;
                    bloom.tint.value = new Color(1f, 0.85f, 0.95f);
                    break;
                }
                default:
                {
                    var balance = profile.Add<WhiteBalance>(true);
                    balance.temperature.value = 32f;
                    balance.tint.value = 4f;
                    var bloom = profile.Add<Bloom>(true);
                    bloom.threshold.value = 0.82f;
                    bloom.intensity.value = 0.75f;
                    bloom.scatter.value = 0.78f;
                    bloom.tint.value = new Color(1f, 0.8f, 0.55f);
                    var vignette = profile.Add<Vignette>(true);
                    vignette.intensity.value = 0.22f;
                    vignette.smoothness.value = 0.55f;
                    vignette.color.value = new Color(0.22f, 0.1f, 0.04f);
                    vignette.center.value = new Vector2(0.5f, 0.5f);
                    vignette.rounded.value = false;
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- house scaffolding

        sealed class House
        {
            public ProfileId id;
            public ProfileInfo info;
            public HouseTransition transition;
            public Transform pieces;
            public Transform interactives;
            public Color labelColor = Color.white;
        }

        static House NewHouse(Transform parent, ProfileId id)
        {
            var info = Profiles.Get(id);
            var root = new GameObject("House_" + info.Key);
            root.transform.SetParent(parent, false);
            var pieces = new GameObject("Pieces").transform;
            pieces.SetParent(root.transform, false);
            var interactives = new GameObject("Interactives").transform;
            interactives.SetParent(root.transform, false);
            var transition = root.AddComponent<HouseTransition>();
            transition.profile = id;
            transition.piecesRoot = pieces;
            transition.interactivesRoot = interactives.gameObject;
            return new House { id = id, info = info, transition = transition, pieces = pieces, interactives = interactives };
        }

        static Transform Piece(House house, string name, Vector3 position, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(house.pieces, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        static Transform Group(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            return go.transform;
        }

        /// <summary>Rotation whose +Z points (flat) from the station toward the viewer.</summary>
        static Quaternion FacingViewer(Vector3 position)
        {
            var dir = Viewer - position;
            dir.y = 0f;
            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        /// <summary>Rotation for canvases and quads so their visible side faces the viewer.</summary>
        static Quaternion ReadableFrom(Vector3 position, Vector3 viewer)
        {
            var dir = position - viewer;
            dir.y = 0f;
            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        static void Floor(House house, Material a, Material b)
        {
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    var tile = Piece(house, "Floor_" + (x + 1) + (z + 1), new Vector3(x * 3f, 0f, z * 3f));
                    var mat = (x + z) % 2 == 0 ? a : b;
                    StripColliders(Prim(PrimitiveType.Cube, "Tile", tile, new Vector3(0f, -0.008f, 0f), new Vector3(3f, 0.02f, 3f), mat));
                }
            }
        }

        delegate void WallDressing(Transform segment, int side, int index, float height);

        static void Walls(House house, Material wall, float height, WallDressing dressing)
        {
            for (var side = 0; side < 4; side++)
            {
                var yaw = side * 90f;
                for (var i = -1; i <= 1; i++)
                {
                    var local = new Vector3(i * 3f, 0f, HalfExtent);
                    var pos = Quaternion.Euler(0f, yaw, 0f) * local;
                    var segment = Piece(house, "Wall_" + side + "_" + (i + 1), pos, yaw);
                    Prim(PrimitiveType.Cube, "Slab", segment, new Vector3(0f, height * 0.5f, 0.06f), new Vector3(3f, height, 0.12f), wall);
                    dressing?.Invoke(segment, side, i, height);
                }
            }
        }

        static void ReturnStation(House house)
        {
            var pos = new Vector3(1.05f, 0f, -0.7f);
            var frame = Group(house.interactives, "ReturnStation", pos, FacingViewer(pos));
            var white = Mat("ReturnWhite", new Color(0.97f, 0.97f, 0.98f), 0.7f);
            Prim(PrimitiveType.Cylinder, "Pedestal", frame, new Vector3(0f, 0.45f, 0f), new Vector3(0.28f, 0.45f, 0.28f), white);
            Prim(PrimitiveType.Cylinder, "Cap", frame, new Vector3(0f, 0.91f, 0f), new Vector3(0.34f, 0.015f, 0.34f), white);
            var cube = Prim(PrimitiveType.Cube, "ReturnCube", frame, new Vector3(0f, 1.03f, 0f), Vector3.one * 0.12f, Glow("ReturnCubeGlow", new Color(0.95f, 0.96f, 1f), 0.6f));
            JapaneseTableBuilder.MakeGrabbable(cube, 0.3f);
            var grab = cube.GetComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            var body = cube.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            cube.AddComponent<ReturnCube>();
            StationLabel(frame, "Voltar ao branco", new Vector3(0f, 1.2f, 0f), 0.55f, house.labelColor).fontStyle = FontStyles.Bold;
        }

        static void CameraStation(House house)
        {
            var pos = new Vector3(-1.05f, 0f, -0.7f);
            var frame = Group(house.interactives, "CameraStation", pos, FacingViewer(pos));
            var white = Mat("ReturnWhite", new Color(0.97f, 0.97f, 0.98f), 0.7f);
            Prim(PrimitiveType.Cylinder, "Pedestal", frame, new Vector3(0f, 0.45f, 0f), new Vector3(0.28f, 0.45f, 0.28f), white);
            Prim(PrimitiveType.Cylinder, "Cap", frame, new Vector3(0f, 0.91f, 0f), new Vector3(0.36f, 0.015f, 0.36f), white);

            var cam = new GameObject("InstantCamera");
            cam.transform.SetParent(frame, false);
            cam.transform.localPosition = new Vector3(0f, 0.975f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var shell = Mat("CameraShell", new Color(0.93f, 0.9f, 0.84f), 0.55f);
            var dark = Mat("CameraDark", new Color(0.08f, 0.08f, 0.09f), 0.8f);
            Prim(PrimitiveType.Cube, "Body", cam.transform, Vector3.zero, new Vector3(0.15f, 0.11f, 0.09f), shell);
            Prim(PrimitiveType.Cube, "Stripe", cam.transform, new Vector3(0f, 0.02f, 0.046f), new Vector3(0.152f, 0.012f, 0.004f), Mat("CameraStripe", house.info.Accent, 0.5f));
            var lensBarrel = Prim(PrimitiveType.Cylinder, "Lens", cam.transform, new Vector3(0f, -0.005f, 0.055f), new Vector3(0.06f, 0.012f, 0.06f), dark);
            lensBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim(PrimitiveType.Cube, "Flash", cam.transform, new Vector3(0.05f, 0.04f, 0.046f), new Vector3(0.025f, 0.015f, 0.004f), Glow("CameraFlash", new Color(1f, 1f, 0.95f), 0.4f));
            Prim(PrimitiveType.Cube, "Slot", cam.transform, new Vector3(0f, 0.056f, -0.01f), new Vector3(0.11f, 0.004f, 0.012f), dark);
            StripColliders(cam);
            var box = cam.AddComponent<BoxCollider>();
            box.size = new Vector3(0.15f, 0.11f, 0.11f);
            box.center = new Vector3(0f, 0f, 0.01f);
            JapaneseTableBuilder.MakeGrabbable(cam, 0.35f);
            cam.AddComponent<ResettablePose>();

            var lensGo = new GameObject("LensCamera");
            lensGo.transform.SetParent(cam.transform, false);
            lensGo.transform.localPosition = new Vector3(0f, -0.005f, 0.07f);
            var lens = lensGo.AddComponent<Camera>();
            lens.enabled = false;
            lens.fieldOfView = 62f;
            lens.nearClipPlane = 0.05f;
            lens.farClipPlane = 40f;
            lens.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var slot = new GameObject("PhotoSlot");
            slot.transform.SetParent(cam.transform, false);
            slot.transform.localPosition = new Vector3(0f, 0.12f, -0.01f);

            var instant = cam.AddComponent<InstantCamera>();
            instant.profile = house.id;
            instant.lens = lens;
            instant.photoSlot = slot.transform;
            instant.photoTemplate = UnlitMat("PhotoTemplate", Color.white, false);
            instant.frameTemplate = Mat("PolaroidFrame", new Color(0.97f, 0.96f, 0.93f), 0.3f);
            StationLabel(frame, "Câmera instantânea", new Vector3(0f, 1.18f, 0f), 0.5f, house.labelColor).fontStyle = FontStyles.Bold;
        }

        static Transform StationTable(House house, string name, Vector3 position, Vector3 size, Material top, Material legs)
        {
            var piece = Piece(house, name, position);
            piece.localRotation = FacingViewer(position);
            Prim(PrimitiveType.Cube, "Top", piece, new Vector3(0f, size.y - 0.025f, 0f), new Vector3(size.x, 0.05f, size.z), top);
            var lx = size.x * 0.5f - 0.05f;
            var lz = size.z * 0.5f - 0.05f;
            foreach (var p in new[] { new Vector3(-lx, 0f, -lz), new Vector3(lx, 0f, -lz), new Vector3(-lx, 0f, lz), new Vector3(lx, 0f, lz) })
                StripColliders(Prim(PrimitiveType.Cube, "Leg", piece, new Vector3(p.x, (size.y - 0.05f) * 0.5f, p.z), new Vector3(0.05f, size.y - 0.05f, 0.05f), legs));
            return piece;
        }

        static GameObject GrabProp(GameObject go, float mass, float autoReturn = 0f)
        {
            JapaneseTableBuilder.MakeGrabbable(go, mass);
            var pose = go.AddComponent<ResettablePose>();
            pose.autoReturnDelay = autoReturn;
            return go;
        }

        static void AddPoke(GameObject go)
        {
            if (go.GetComponent<XRSimpleInteractable>() == null)
                go.AddComponent<XRSimpleInteractable>();
            var filter = go.GetComponent<XRPokeFilter>();
            if (filter == null)
                filter = go.AddComponent<XRPokeFilter>();
            var config = new PokeThresholdData
            {
                pokeDirection = PokeAxis.None,
                enablePokeAngleThreshold = false,
                interactionDepthOffset = 0f
            };
            filter.pokeConfiguration = new PokeThresholdDatumProperty(config);
        }

        static ParticleSystem Particles(Transform parent, string name, Vector3 localPos, Quaternion localRot, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return ps;
        }

        static void Floating(GameObject go, float bob, float speed, float spin, float phase)
        {
            var f = go.AddComponent<FloatSpin>();
            f.bobHeight = bob;
            f.bobSpeed = speed;
            f.spinSpeed = spin;
            f.phase = phase;
        }

        // ---------------------------------------------------------------- primitives and assets

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null)
                renderer.sharedMaterial = mat;
            return go;
        }

        static GameObject Deco(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Vector3 euler = default)
        {
            var go = Prim(type, name, parent, localPos, localScale, mat);
            go.transform.localRotation = Quaternion.Euler(euler);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            StripColliders(go);
            return go;
        }

        static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
        }

        static void BoxOnly(GameObject go)
        {
            StripColliders(go);
            go.AddComponent<BoxCollider>();
        }

        static void ConvexOnly(GameObject go)
        {
            StripColliders(go);
            var mesh = go.AddComponent<MeshCollider>();
            mesh.convex = true;
        }

        static Material Mat(string name, Color color, float smoothness = 0.35f, float metallic = 0f)
        {
            return LitMat(name, color, smoothness, metallic, false, Color.black);
        }

        static Material Glow(string name, Color color, float intensity)
        {
            return LitMat(name, color, 0.5f, 0f, true, color * intensity);
        }

        static Material LitMat(string name, Color color, float smoothness, float metallic, bool emission, Color emissionColor)
        {
            if (MaterialCache.TryGetValue(name, out var cached))
                return cached;
            var path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.enableInstancing = true;
            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            MaterialCache[name] = mat;
            return mat;
        }

        static Material UnlitMat(string name, Color color, bool transparent)
        {
            if (MaterialCache.TryGetValue(name, out var cached))
                return cached;
            var path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            if (transparent)
                MakeTransparent(mat, false);
            EditorUtility.SetDirty(mat);
            MaterialCache[name] = mat;
            return mat;
        }

        static Material ParticleMat(string name, Color color, bool additive)
        {
            if (MaterialCache.TryGetValue(name, out var cached))
                return cached;
            var path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", softDot);
            mat.mainTexture = softDot;
            MakeTransparent(mat, additive);
            EditorUtility.SetDirty(mat);
            MaterialCache[name] = mat;
            return mat;
        }

        static void MakeTransparent(Material mat, bool additive)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static Material TransparentGlow(string name, Color color, float alpha)
        {
            var mat = UnlitMat(name, new Color(color.r, color.g, color.b, alpha), true);
            return mat;
        }

        static Texture2D LoadSoftDot()
        {
            var path = MaterialFolder + "/SoftDot.asset";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
                return tex;
            tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "SoftDot", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
                    var a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            tex.Apply();
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        static TMP_FontAsset LoadFont()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            return asset != null ? asset : TMP_Settings.defaultFontAsset;
        }

        static PhysicsMaterial Friction(string name, float friction, float bounce)
        {
            var path = MaterialFolder + "/" + name + ".physicMaterial";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (mat == null)
            {
                mat = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.staticFriction = friction;
            mat.dynamicFriction = friction * 0.9f;
            mat.bounciness = bounce;
            mat.frictionCombine = PhysicsMaterialCombine.Maximum;
            mat.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Color[] Palette()
        {
            return new[]
            {
                new Color(1f, 0.3f, 0.75f),
                new Color(1f, 0.82f, 0.15f),
                new Color(0.15f, 0.8f, 0.75f),
                new Color(0.45f, 0.35f, 1f),
                new Color(1f, 0.45f, 0.15f),
                new Color(0.35f, 0.85f, 0.3f)
            };
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
                var found = FindRecursive(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        static Transform FindRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var found = FindRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
