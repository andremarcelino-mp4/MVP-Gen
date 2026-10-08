using GenJutsu.TableProps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace GenJutsu.TableProps.EditorTools
{
    public static class JapaneseTableBuilder
    {
        const string RootName = "JapaneseTableSet";

        [MenuItem("GenJutsu/Rebuild Japanese Table Set")]
        static void MenuBuild()
        {
            Debug.Log("[GenJutsu] " + Build());
        }

        [MenuItem("GenJutsu/Rebuild Tea Set")]
        static void MenuRebuildTeaSet()
        {
            Debug.Log("[GenJutsu] " + RebuildTeaSet());
        }

        [MenuItem("GenJutsu/Rebuild Brush")]
        static void MenuRebuildBrush()
        {
            Debug.Log("[GenJutsu] " + RebuildBrush());
        }

        internal static string RebuildTeaSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var rootGo = GameObject.Find(RootName);
            if (rootGo == null)
                return "Root " + RootName + " not found — run GenJutsu/Rebuild Japanese Table Set first";

            foreach (var childName in new[] { "Teapot", "TeaCup_1", "TeaCup_2" })
            {
                var existing = rootGo.transform.Find(childName);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
            }

            var brown = MakeMat("TeaBrown", new Color(0.36f, 0.21f, 0.13f), smoothness: 0.55f);
            var teaLiquid = MakeMat("TeaLiquid", new Color(0.52f, 0.31f, 0.12f), smoothness: 0.8f);

            var teapot = BuildTeapot(rootGo.transform, new Vector3(-0.38f, 0.65f, 0.05f), brown, teaLiquid);
            MakeGrabbable(teapot, 0.25f);
            AddRespawn(teapot);

            var cup1 = BuildCup(rootGo.transform, "TeaCup_1", new Vector3(-0.38f, 0.65f, -0.12f), brown, teaLiquid);
            MakeGrabbable(cup1, 0.08f);
            AddRespawn(cup1);
            var cup2 = BuildCup(rootGo.transform, "TeaCup_2", new Vector3(-0.28f, 0.65f, -0.18f), brown, teaLiquid);
            MakeGrabbable(cup2, 0.08f);
            AddRespawn(cup2);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Rebuilt Teapot + TeaCup_1 + TeaCup_2 (flush on table at y=0.65)";
        }

        internal static string RebuildBrush()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var rootGo = GameObject.Find(RootName);
            if (rootGo == null)
                return "Root " + RootName + " not found — run GenJutsu/Rebuild Japanese Table Set first";

            var existing = rootGo.transform.Find("Brush");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var cord = MakeMat("BrushDark", new Color(0.09f, 0.06f, 0.05f), smoothness: 0.25f);
            var lacquer = MakeMat("BrushLacquer", new Color(0.15f, 0.07f, 0.05f), smoothness: 0.7f);
            var hair = MakeMat("BrushHair", new Color(0.88f, 0.85f, 0.75f), smoothness: 0.4f);
            var gold = MakeMat("ScrollGold", new Color(0.85f, 0.7f, 0.25f), metallic: 0.85f, smoothness: 0.5f);
            var ink = MakeMat("InkBlack", new Color(0.05f, 0.05f, 0.06f));

            var brush = BuildOrientalBrush(rootGo.transform, cord, lacquer, gold, hair, ink);
            AddRespawn(brush);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Rebuilt Brush (Japanese fude, always inked, velocity-tracking grab)";
        }

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var existing = GameObject.Find(RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            // Outside the room: room is roughly ±5 on XZ; place near outer floor.
            // Just outside the south wall of the template room (~z = -3).
            root.transform.position = new Vector3(0f, 0f, -5.5f);

            var white = MakeMat("TableWhite", new Color(0.96f, 0.96f, 0.96f));
            var paper = MakeMat("ScrollPaper", new Color(0.93f, 0.88f, 0.72f), smoothness: 0.1f);
            var paperRoll = MakeMat("ScrollPaperRoll", new Color(0.89f, 0.83f, 0.66f), smoothness: 0.12f);
            var red = MakeMat("ScrollRed", new Color(0.58f, 0.13f, 0.1f), smoothness: 0.5f);
            var gold = MakeMat("ScrollGold", new Color(0.85f, 0.7f, 0.25f), metallic: 0.85f, smoothness: 0.5f);
            var brown = MakeMat("TeaBrown", new Color(0.36f, 0.21f, 0.13f), smoothness: 0.55f);
            var wood = MakeMat("Wood", new Color(0.35f, 0.22f, 0.12f));
            var ink = MakeMat("InkBlack", new Color(0.05f, 0.05f, 0.06f));
            var stone = MakeMat("Stone", new Color(0.45f, 0.45f, 0.48f));
            var jade = MakeMat("Jade", new Color(0.25f, 0.75f, 0.45f), true, new Color(0.15f, 0.55f, 0.3f));
            var brushDark = MakeMat("BrushDark", new Color(0.09f, 0.06f, 0.05f), smoothness: 0.25f);
            var brushLacquer = MakeMat("BrushLacquer", new Color(0.15f, 0.07f, 0.05f), smoothness: 0.7f);
            var brushHair = MakeMat("BrushHair", new Color(0.88f, 0.85f, 0.75f), smoothness: 0.4f);
            var teaLiquid = MakeMat("TeaLiquid", new Color(0.52f, 0.31f, 0.12f), smoothness: 0.8f);

            // Table (static) — legs are siblings so parent scale does not squash them.
            // Top surface ~0.65 m (table center 0.62 + half thickness 0.03).
            var table = CreatePrimitive(PrimitiveType.Cube, "Table", root.transform, new Vector3(0f, 0.62f, 0f), new Vector3(1.2f, 0.06f, 0.7f), white);
            Object.DestroyImmediate(table.GetComponent<Collider>());
            table.AddComponent<BoxCollider>();
            foreach (var leg in new[] {
                new Vector3(-0.5f, 0.30f, -0.28f), new Vector3(0.5f, 0.30f, -0.28f),
                new Vector3(-0.5f, 0.30f, 0.28f), new Vector3(0.5f, 0.30f, 0.28f)
            })
            {
                var l = CreatePrimitive(PrimitiveType.Cube, "Leg", root.transform, leg, new Vector3(0.06f, 0.60f, 0.06f), white);
                Object.DestroyImmediate(l.GetComponent<Collider>());
                l.AddComponent<BoxCollider>();
            }

            // Scroll surface — vertical (kakemono): red rolls on ±Z
            var scrollRoot = new GameObject("Scroll");
            scrollRoot.transform.SetParent(root.transform, false);
            scrollRoot.transform.localPosition = new Vector3(0f, 0.66f, 0.02f);

            var scrollPaper = CreatePrimitive(PrimitiveType.Cube, "Paper", scrollRoot.transform, Vector3.zero, new Vector3(0.32f, 0.008f, 0.55f), paper);
            Object.DestroyImmediate(scrollPaper.GetComponent<Collider>());
            var paperCol = scrollPaper.AddComponent<BoxCollider>();
            paperCol.center = new Vector3(0f, -0.892f, 0.105f);
            paperCol.size = new Vector3(1f, 0.704f, 0.79f);
            var scrollCanvas = scrollPaper.AddComponent<ScrollCanvas>();
            var canvasSo = new SerializedObject(scrollCanvas);
            canvasSo.FindProperty("sheetRestLocalY").floatValue = -1.25f;
            canvasSo.ApplyModifiedPropertiesWithoutUndo();

            const float rollRadius = 0.036f;
            var rollY = rollRadius - 0.01f;
            for (var side = -1; side <= 1; side += 2)
            {
                var label = side > 0 ? "Top" : "Bottom";
                var z = side * 0.278f;

                var wrap = CreatePrimitive(PrimitiveType.Cylinder, label + "PaperRoll", scrollRoot.transform, new Vector3(0f, rollY, z), new Vector3(rollRadius * 2f, 0.15f, rollRadius * 2f), paperRoll);
                wrap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Object.DestroyImmediate(wrap.GetComponent<Collider>());

                var rod = CreatePrimitive(PrimitiveType.Cylinder, label + "Rod", scrollRoot.transform, new Vector3(0f, rollY, z), new Vector3(0.03f, 0.17f, 0.03f), red);
                rod.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Object.DestroyImmediate(rod.GetComponent<Collider>());

                for (var k = -1; k <= 1; k += 2)
                {
                    var knob = CreatePrimitive(PrimitiveType.Cylinder, label + (k > 0 ? "KnobR" : "KnobL"), scrollRoot.transform, new Vector3(k * 0.16f, rollY, z), new Vector3(rollRadius * 2f, 0.01f, rollRadius * 2f), gold);
                    knob.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Object.DestroyImmediate(knob.GetComponent<Collider>());
                }
            }

            var silk = MakeMat("ScrollSilk", new Color(0.34f, 0.07f, 0.09f), smoothness: 0.45f);
            var silkL = CreatePrimitive(PrimitiveType.Cube, "SilkL", scrollRoot.transform, new Vector3(-0.149f, -0.0069f, 0f), new Vector3(0.022f, 0.006f, 0.55f), silk);
            Object.DestroyImmediate(silkL.GetComponent<Collider>());
            var silkR = CreatePrimitive(PrimitiveType.Cube, "SilkR", scrollRoot.transform, new Vector3(0.149f, -0.0069f, 0f), new Vector3(0.022f, 0.006f, 0.55f), silk);
            Object.DestroyImmediate(silkR.GetComponent<Collider>());
            var silkB = CreatePrimitive(PrimitiveType.Cube, "SilkB", scrollRoot.transform, new Vector3(0f, -0.0069f, -0.233f), new Vector3(0.276f, 0.006f, 0.022f), silk);
            Object.DestroyImmediate(silkB.GetComponent<Collider>());
            var silkT = CreatePrimitive(PrimitiveType.Cube, "SilkT", scrollRoot.transform, new Vector3(0f, -0.0069f, 0.233f), new Vector3(0.276f, 0.006f, 0.022f), silk);
            Object.DestroyImmediate(silkT.GetComponent<Collider>());
            MakeGrabbable(scrollRoot, 0.2f);
            AddRespawn(scrollRoot);

            var rest = CreatePrimitive(PrimitiveType.Cube, "BrushRest", root.transform, new Vector3(0.25f, 0.66f, -0.18f), new Vector3(0.12f, 0.02f, 0.04f), stone);
            Object.DestroyImmediate(rest.GetComponent<Collider>());
            rest.AddComponent<BoxCollider>();

            // Oriental calligraphy brush (grabbable)
            var brush = BuildOrientalBrush(root.transform, brushDark, brushLacquer, gold, brushHair, ink);
            AddRespawn(brush);

            // Jade dragon (sculpted coil)
            JadeDragonBuilder.Build(root.transform, new Vector3(0f, 0.654f, 0.22f), jade, gold);

            // Tea set
            var teapot = BuildTeapot(root.transform, new Vector3(-0.38f, 0.65f, 0.05f), brown, teaLiquid);
            MakeGrabbable(teapot, 0.25f);
            AddRespawn(teapot);

            var cup1 = BuildCup(root.transform, "TeaCup_1", new Vector3(-0.38f, 0.65f, -0.12f), brown, teaLiquid);
            MakeGrabbable(cup1, 0.08f);
            AddRespawn(cup1);
            var cup2 = BuildCup(root.transform, "TeaCup_2", new Vector3(-0.28f, 0.65f, -0.18f), brown, teaLiquid);
            MakeGrabbable(cup2, 0.08f);
            AddRespawn(cup2);

            // Wooden case
            var caseGo = CreatePrimitive(PrimitiveType.Cube, "WoodenCase", root.transform, new Vector3(0.38f, 0.675f, -0.12f), new Vector3(0.22f, 0.03f, 0.06f), wood);
            Object.DestroyImmediate(caseGo.GetComponent<Collider>());
            MakeGrabbable(caseGo, 0.12f);
            AddRespawn(caseGo);
            caseGo.AddComponent<BoxCollider>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Built " + RootName + " at " + root.transform.position + " items=" + root.transform.childCount;
        }

        static GameObject BuildOrientalBrush(Transform parent, Material cordMat, Material lacquerMat, Material goldMat, Material hairMat, Material inkMat)
        {
            var brush = new GameObject("Brush");
            brush.transform.SetParent(parent, false);
            brush.transform.localPosition = new Vector3(0.25f, 0.678f, -0.18f);
            brush.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            CreatePrimitive(PrimitiveType.Cylinder, "HandleLow", brush.transform, new Vector3(0f, -0.008f, 0f), new Vector3(0.0104f, 0.026f, 0.0104f), lacquerMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Node1", brush.transform, new Vector3(0f, 0.018f, 0f), new Vector3(0.0116f, 0.002f, 0.0116f), cordMat);
            CreatePrimitive(PrimitiveType.Cylinder, "HandleMid", brush.transform, new Vector3(0f, 0.044f, 0f), new Vector3(0.0108f, 0.024f, 0.0108f), lacquerMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Node2", brush.transform, new Vector3(0f, 0.068f, 0f), new Vector3(0.012f, 0.002f, 0.012f), cordMat);
            CreatePrimitive(PrimitiveType.Cylinder, "HandleHigh", brush.transform, new Vector3(0f, 0.098f, 0f), new Vector3(0.0116f, 0.028f, 0.0116f), lacquerMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Node3", brush.transform, new Vector3(0f, 0.126f, 0f), new Vector3(0.0124f, 0.002f, 0.0124f), cordMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Butt", brush.transform, new Vector3(0f, 0.135f, 0f), new Vector3(0.013f, 0.007f, 0.013f), lacquerMat);
            var loop = CreatePrimitive(PrimitiveType.Cylinder, "CordLoop", brush.transform, new Vector3(0f, 0.1445f, 0f), new Vector3(0.011f, 0.0025f, 0.011f), cordMat);
            loop.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreatePrimitive(PrimitiveType.Cylinder, "FerruleTop", brush.transform, new Vector3(0f, -0.039f, 0f), new Vector3(0.0118f, 0.01f, 0.0118f), goldMat);
            CreatePrimitive(PrimitiveType.Cylinder, "FerruleBand", brush.transform, new Vector3(0f, -0.0465f, 0f), new Vector3(0.0106f, 0.005f, 0.0106f), cordMat);
            CreatePrimitive(PrimitiveType.Cylinder, "FerruleLow", brush.transform, new Vector3(0f, -0.053f, 0f), new Vector3(0.0112f, 0.008f, 0.0112f), goldMat);

            CreatePrimitive(PrimitiveType.Capsule, "Hair1", brush.transform, new Vector3(0f, -0.07f, 0f), new Vector3(0.0112f, 0.013f, 0.0112f), hairMat);
            CreatePrimitive(PrimitiveType.Capsule, "Hair2", brush.transform, new Vector3(0f, -0.092f, 0f), new Vector3(0.0084f, 0.015f, 0.0084f), hairMat);
            CreatePrimitive(PrimitiveType.Capsule, "Hair3", brush.transform, new Vector3(0f, -0.111f, 0f), new Vector3(0.0058f, 0.014f, 0.0058f), hairMat);
            CreatePrimitive(PrimitiveType.Capsule, "Tip", brush.transform, new Vector3(0f, -0.127f, 0f), new Vector3(0.0034f, 0.014f, 0.0034f), inkMat);

            foreach (var t in brush.GetComponentsInChildren<Transform>())
            {
                if (t != brush.transform)
                    Object.DestroyImmediate(t.GetComponent<Collider>());
            }

            var tipPoint = new GameObject("TipPoint");
            tipPoint.transform.SetParent(brush.transform, false);
            tipPoint.transform.localPosition = new Vector3(0f, -0.141f, 0f);
            tipPoint.transform.localRotation = Quaternion.identity;

            var grabCol = brush.AddComponent<CapsuleCollider>();
            grabCol.height = 0.31f;
            grabCol.radius = 0.008f;
            grabCol.center = new Vector3(0f, 0.0075f, 0f);

            MakeBrushGrabbable(brush, 0.08f);
            var brushScript = brush.AddComponent<InkBrush>();
            var so = new SerializedObject(brushScript);
            so.FindProperty("tip").objectReferenceValue = tipPoint.transform;
            so.FindProperty("tipRadius").floatValue = 0.005f;
            so.FindProperty("paintDistance").floatValue = 0.04f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return brush;
        }

        internal static void AddRespawn(GameObject go)
        {
            if (go.GetComponent<TablePropRespawn>() == null)
                go.AddComponent<TablePropRespawn>();
        }

        static GameObject BuildTeapot(Transform parent, Vector3 localPos, Material bodyMat, Material liquidMat)
        {
            var pot = new GameObject("Teapot");
            pot.transform.SetParent(parent, false);
            pot.transform.localPosition = localPos;

            CreatePrimitive(PrimitiveType.Cylinder, "Foot", pot.transform, new Vector3(0f, 0.005f, 0f), new Vector3(0.07f, 0.005f, 0.07f), bodyMat);
            CreatePrimitive(PrimitiveType.Sphere, "Body", pot.transform, new Vector3(0f, 0.065f, 0f), new Vector3(0.13f, 0.115f, 0.13f), bodyMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Neck", pot.transform, new Vector3(0f, 0.128f, 0f), new Vector3(0.05f, 0.009f, 0.05f), bodyMat);
            CreatePrimitive(PrimitiveType.Sphere, "Lid", pot.transform, new Vector3(0f, 0.142f, 0f), new Vector3(0.078f, 0.034f, 0.078f), bodyMat);
            CreatePrimitive(PrimitiveType.Cylinder, "KnobStem", pot.transform, new Vector3(0f, 0.162f, 0f), new Vector3(0.012f, 0.005f, 0.012f), bodyMat);
            CreatePrimitive(PrimitiveType.Sphere, "Knob", pot.transform, new Vector3(0f, 0.174f, 0f), new Vector3(0.024f, 0.022f, 0.024f), bodyMat);

            var spoutBase = CreatePrimitive(PrimitiveType.Capsule, "SpoutBase", pot.transform, new Vector3(0.062f, 0.075f, 0f), new Vector3(0.028f, 0.04f, 0.028f), bodyMat);
            spoutBase.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            var spoutTip = CreatePrimitive(PrimitiveType.Capsule, "SpoutTip", pot.transform, new Vector3(0.1f, 0.115f, 0f), new Vector3(0.02f, 0.03f, 0.02f), bodyMat);
            spoutTip.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);

            var handleUpper = CreatePrimitive(PrimitiveType.Capsule, "HandleUpper", pot.transform, new Vector3(-0.075f, 0.1f, 0f), new Vector3(0.016f, 0.026f, 0.016f), bodyMat);
            handleUpper.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreatePrimitive(PrimitiveType.Capsule, "HandleOuter", pot.transform, new Vector3(-0.098f, 0.083f, 0f), new Vector3(0.016f, 0.038f, 0.016f), bodyMat);
            var handleLower = CreatePrimitive(PrimitiveType.Capsule, "HandleLower", pot.transform, new Vector3(-0.076f, 0.048f, 0f), new Vector3(0.016f, 0.021f, 0.016f), bodyMat);
            handleLower.transform.localRotation = Quaternion.Euler(0f, 0f, -72f);

            foreach (var t in pot.GetComponentsInChildren<Transform>())
            {
                if (t != pot.transform)
                    Object.DestroyImmediate(t.GetComponent<Collider>());
            }

            var col = pot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.2f, 0.17f, 0.14f);
            col.center = new Vector3(0f, 0.085f, 0f);
            return pot;
        }

        static GameObject BuildCup(Transform parent, string name, Vector3 localPos, Material bodyMat, Material liquidMat)
        {
            var cup = new GameObject(name);
            cup.transform.SetParent(parent, false);
            cup.transform.localPosition = localPos;

            CreatePrimitive(PrimitiveType.Cylinder, "Foot", cup.transform, new Vector3(0f, 0.004f, 0f), new Vector3(0.03f, 0.004f, 0.03f), bodyMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Body", cup.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.046f, 0.027f, 0.046f), bodyMat);
            CreatePrimitive(PrimitiveType.Cylinder, "RimBand", cup.transform, new Vector3(0f, 0.061f, 0f), new Vector3(0.048f, 0.003f, 0.048f), bodyMat);
            var liquid = CreatePrimitive(PrimitiveType.Cylinder, "Liquid", cup.transform, new Vector3(0f, 0.066f, 0f), new Vector3(0.038f, 0.002f, 0.038f), liquidMat);

            foreach (var t in cup.GetComponentsInChildren<Transform>())
            {
                if (t != cup.transform)
                    Object.DestroyImmediate(t.GetComponent<Collider>());
            }

            var tea = cup.AddComponent<TeaCup>();
            var so = new SerializedObject(tea);
            so.FindProperty("liquid").objectReferenceValue = liquid.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            var col = cup.AddComponent<CapsuleCollider>();
            col.height = 0.068f;
            col.radius = 0.024f;
            col.center = new Vector3(0f, 0.034f, 0f);
            return cup;
        }

        internal static void MakeGrabbable(GameObject go, float mass)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
                grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.useDynamicAttach = true;

            if (go.GetComponent<XRGeneralGrabTransformer>() == null)
                go.AddComponent<XRGeneralGrabTransformer>();
        }

        /// <summary>
        /// Fixed writing attach for the brush: tip (-localY) points forward/down in hand.
        /// </summary>
        static void MakeBrushGrabbable(GameObject go, float mass)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var attachGo = new GameObject("Attach");
            attachGo.transform.SetParent(go.transform, false);
            attachGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            // Tip is -localY; Rx 75 aims tip forward/down relative to the controller attach.
            attachGo.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
                grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.useDynamicAttach = false;
            grab.attachTransform = attachGo.transform;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.snapToColliderVolume = false;

            if (go.GetComponent<XRGeneralGrabTransformer>() == null)
                go.AddComponent<XRGeneralGrabTransformer>();
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
            return go;
        }

        static Material MakeMat(string name, Color color, bool emission = false, Color emissionColor = default, float metallic = -1f, float smoothness = -1f)
        {
            var dir = "Assets/GenJutsu/TableProps/Materials";
            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "TableProps");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/GenJutsu/TableProps", "Materials");
            var path = dir + "/" + name + ".mat";

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor == default ? color * 0.6f : emissionColor);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (metallic >= 0f && mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (smoothness >= 0f && mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
