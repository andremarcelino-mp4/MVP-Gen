using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public static partial class ProfileDemoBuilder
    {
        // ---------------------------------------------------------------- Zen / Equilibrado

        static HouseTransition BuildZenHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Zen);
            h.labelColor = new Color(0.2f, 0.16f, 0.12f);
            var tatami = Mat("ZenTatami", new Color(0.74f, 0.72f, 0.52f), 0.2f);
            var tatamiAlt = Mat("ZenTatamiAlt", new Color(0.69f, 0.68f, 0.47f), 0.2f);
            var wood = Mat("ZenWood", new Color(0.32f, 0.2f, 0.12f), 0.45f);
            var darkWood = Mat("ZenDarkWood", new Color(0.16f, 0.1f, 0.07f), 0.4f);
            var paper = Glow("ZenPaper", new Color(0.96f, 0.94f, 0.86f), 0.22f);
            var stoneA = Mat("ZenStone", new Color(0.32f, 0.33f, 0.34f), 0.55f);
            var stoneB = Mat("ZenStoneLight", new Color(0.55f, 0.55f, 0.53f), 0.5f);
            var stoneC = Mat("ZenStoneWarm", new Color(0.46f, 0.42f, 0.38f), 0.5f);
            var sand = Mat("ZenSandToken", new Color(0.88f, 0.82f, 0.7f), 0.1f);
            var leaf = Mat("ZenLeaf", new Color(0.24f, 0.45f, 0.24f), 0.25f);
            var bamboo = Mat("ZenBamboo", new Color(0.45f, 0.6f, 0.3f), 0.5f);
            var lanternGlow = Glow("ZenLanternGlow", new Color(1f, 0.78f, 0.45f), 1.8f);
            var petal = Glow("ZenPetal", new Color(1f, 0.78f, 0.84f), 0.3f);

            Floor(h, tatami, tatamiAlt);
            for (var x = -1; x <= 1; x++)
            {
                var seam = Piece(h, "TatamiSeam_" + (x + 1), new Vector3(x * 3f + 1.5f, 0f, 0f));
                Deco(PrimitiveType.Cube, "Seam", seam, new Vector3(0f, 0.004f, 0f), new Vector3(0.05f, 0.006f, 9f), darkWood);
            }

            Walls(h, paper, 2.8f, (seg, side, i, height) =>
            {
                for (var k = 0; k < 5; k++)
                    Deco(PrimitiveType.Cube, "Post", seg, new Vector3(-1.5f + k * 0.75f, height * 0.5f, -0.01f), new Vector3(k % 4 == 0 ? 0.1f : 0.035f, height, 0.05f), wood);
                foreach (var y in new[] { 0.1f, 0.85f, 1.6f, 2.15f, height - 0.05f })
                    Deco(PrimitiveType.Cube, "Rail", seg, new Vector3(0f, y, -0.01f), new Vector3(3f, y < 0.2f ? 0.2f : 0.035f, 0.05f), wood);
            });

            for (var b = 0; b < 4; b++)
            {
                var beam = Piece(h, "Beam_" + b, new Vector3(-3.375f + b * 2.25f, 0f, 0f));
                Deco(PrimitiveType.Cube, "Beam", beam, new Vector3(0f, 2.9f, 0f), new Vector3(0.16f, 0.2f, 9f), darkWood);
            }

            for (var c = 0; c < 3; c++)
            {
                var ceiling = Piece(h, "Ceiling_" + c, new Vector3((c - 1) * 3f, 0f, 0f));
                Deco(PrimitiveType.Cube, "Panel", ceiling, new Vector3(0f, 3.02f, 0f), new Vector3(3f, 0.03f, 9f), wood);
            }

            foreach (var corner in new[] { new Vector3(-3.6f, 0f, 3.6f), new Vector3(3.6f, 0f, 3.6f), new Vector3(-3.6f, 0f, -3.6f), new Vector3(3.6f, 0f, -3.6f) })
            {
                var lantern = Piece(h, "Lantern", corner);
                Deco(PrimitiveType.Cylinder, "Base", lantern, new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.08f, 0.42f), stoneA);
                Deco(PrimitiveType.Cylinder, "Post", lantern, new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.3f, 0.14f), stoneB);
                Deco(PrimitiveType.Cube, "Shelf", lantern, new Vector3(0f, 0.8f, 0f), new Vector3(0.42f, 0.06f, 0.42f), stoneA);
                Deco(PrimitiveType.Cube, "Light", lantern, new Vector3(0f, 0.97f, 0f), new Vector3(0.24f, 0.26f, 0.24f), lanternGlow);
                Deco(PrimitiveType.Cube, "Roof", lantern, new Vector3(0f, 1.16f, 0f), new Vector3(0.5f, 0.08f, 0.5f), stoneA, new Vector3(0f, 45f, 0f));
                Deco(PrimitiveType.Sphere, "Jewel", lantern, new Vector3(0f, 1.26f, 0f), new Vector3(0.12f, 0.12f, 0.12f), stoneB);
            }

            var grove = Piece(h, "BambooGrove", new Vector3(-3.5f, 0f, 1.6f));
            var rng = new System.Random(11);
            for (var s = 0; s < 9; s++)
            {
                var p = new Vector3((float)rng.NextDouble() * 0.9f - 0.45f, 0f, (float)rng.NextDouble() * 1.8f - 0.9f);
                var height = 2.4f + (float)rng.NextDouble() * 0.5f;
                Deco(PrimitiveType.Cylinder, "Stalk", grove, p + Vector3.up * (height * 0.5f), new Vector3(0.06f, height * 0.5f, 0.06f), bamboo);
                for (var n = 1; n < 5; n++)
                    Deco(PrimitiveType.Cylinder, "Node", grove, p + Vector3.up * (n * height / 5f), new Vector3(0.07f, 0.01f, 0.07f), leaf);
                Deco(PrimitiveType.Sphere, "Leaves", grove, p + Vector3.up * height, new Vector3(0.45f, 0.25f, 0.45f), leaf);
            }

            Deco(PrimitiveType.Cube, "GroveStones", grove, new Vector3(0f, 0.03f, 0f), new Vector3(1.2f, 0.06f, 2.1f), stoneB);

            var bonsai = Piece(h, "Bonsai", new Vector3(3.4f, 0f, 2.4f));
            Deco(PrimitiveType.Cube, "Stand", bonsai, new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.5f, 0.5f), darkWood);
            Deco(PrimitiveType.Cube, "Pot", bonsai, new Vector3(0f, 0.56f, 0f), new Vector3(0.5f, 0.12f, 0.32f), Mat("ZenPot", new Color(0.22f, 0.25f, 0.32f), 0.6f));
            Deco(PrimitiveType.Cylinder, "Trunk", bonsai, new Vector3(0.05f, 0.75f, 0f), new Vector3(0.06f, 0.18f, 0.06f), Mat("ZenBark", new Color(0.3f, 0.2f, 0.13f), 0.2f), new Vector3(0f, 0f, 18f));
            Deco(PrimitiveType.Sphere, "Crown", bonsai, new Vector3(-0.05f, 0.98f, 0f), new Vector3(0.55f, 0.22f, 0.38f), leaf);
            Deco(PrimitiveType.Sphere, "Crown2", bonsai, new Vector3(0.18f, 0.9f, 0.05f), new Vector3(0.3f, 0.16f, 0.26f), leaf);

            var scroll = Piece(h, "HangingScroll", new Vector3(2.3f, 0f, 4.42f));
            Deco(PrimitiveType.Cube, "Paper", scroll, new Vector3(0f, 1.5f, 0f), new Vector3(0.55f, 1.3f, 0.01f), Mat("ZenScroll", new Color(0.93f, 0.88f, 0.74f), 0.1f));
            var ink = Mat("InkBlackDemo", new Color(0.05f, 0.05f, 0.06f));
            Deco(PrimitiveType.Cube, "Stroke1", scroll, new Vector3(0f, 1.75f, -0.008f), new Vector3(0.04f, 0.45f, 0.004f), ink, new Vector3(0f, 0f, 8f));
            Deco(PrimitiveType.Cube, "Stroke2", scroll, new Vector3(0.02f, 1.35f, -0.008f), new Vector3(0.22f, 0.035f, 0.004f), ink, new Vector3(0f, 0f, -12f));
            Deco(PrimitiveType.Sphere, "Enso", scroll, new Vector3(0f, 1.08f, -0.008f), new Vector3(0.18f, 0.18f, 0.004f), ink);
            Deco(PrimitiveType.Cylinder, "RodTop", scroll, new Vector3(0f, 2.16f, 0f), new Vector3(0.03f, 0.32f, 0.03f), darkWood, new Vector3(0f, 0f, 90f));
            Deco(PrimitiveType.Cylinder, "RodBottom", scroll, new Vector3(0f, 0.84f, 0f), new Vector3(0.035f, 0.32f, 0.035f), darkWood, new Vector3(0f, 0f, 90f));

            var petals = Piece(h, "Petals", new Vector3(0f, 0f, 0.8f));
            for (var p = 0; p < 10; p++)
            {
                var a = p * 36f * Mathf.Deg2Rad;
                var r = 1.4f + (p % 3) * 0.6f;
                var leafGo = Deco(PrimitiveType.Sphere, "Petal", petals, new Vector3(Mathf.Cos(a) * r, 1.9f + (p % 4) * 0.22f, Mathf.Sin(a) * r), new Vector3(0.05f, 0.012f, 0.035f), petal, new Vector3(20f, p * 30f, 10f));
                Floating(leafGo, 0.08f, 0.12f + p * 0.01f, 35f, p * 0.7f);
            }

            // Stone stacking (left)
            StationTable(h, "StoneTable", StationLeft, new Vector3(0.9f, 0.7f, 0.6f), wood, darkWood);
            var stoneFrame = Group(h.interactives, "StoneStacking", StationLeft, FacingViewer(StationLeft));
            var disc = Prim(PrimitiveType.Cylinder, "SandDisc", stoneFrame, new Vector3(0f, 0.705f, -0.07f), new Vector3(0.38f, 0.005f, 0.38f), sand);
            ConvexOnly(disc);
            var baseTop = Group(stoneFrame, "BaseTop", new Vector3(0f, 0.71f, -0.07f), Quaternion.identity);
            var friction = Friction("StoneGrip", 1f, 0f);
            var stoneSizes = new[]
            {
                new Vector3(0.14f, 0.05f, 0.11f), new Vector3(0.12f, 0.045f, 0.1f), new Vector3(0.1f, 0.04f, 0.085f),
                new Vector3(0.085f, 0.036f, 0.07f), new Vector3(0.07f, 0.032f, 0.06f), new Vector3(0.055f, 0.03f, 0.048f)
            };
            var stoneMats = new[] { stoneA, stoneC, stoneB };
            var stones = new List<ResettablePose>();
            for (var i = 0; i < stoneSizes.Length; i++)
            {
                var size = stoneSizes[i];
                var x = -0.36f + i * 0.144f;
                var stone = Prim(PrimitiveType.Sphere, "Stone" + i, stoneFrame, new Vector3(x, 0.7f + size.y * 0.5f + 0.003f, 0.2f), size, stoneMats[i % stoneMats.Length]);
                stone.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
                ConvexOnly(stone);
                stone.GetComponent<Collider>().sharedMaterial = friction;
                GrabProp(stone, 0.25f + size.x * 3f);
                stones.Add(stone.GetComponent<ResettablePose>());
            }

            var stacking = stoneFrame.gameObject.AddComponent<ZenStoneStacking>();
            SetActivity(stacking, h.id, "zen_pedras", "Pilha de pedras");
            stacking.baseTop = baseTop;
            stacking.baseRadius = 0.2f;
            stacking.stones = stones.ToArray();
            StationLabel(stoneFrame, "Empilhe as pedras no disco de areia", new Vector3(0f, 1.05f, -0.1f), 1.0f, h.labelColor);

            // Sand garden (right)
            var garden = Piece(h, "SandGarden", StationRight);
            garden.localRotation = FacingViewer(StationRight);
            Prim(PrimitiveType.Cube, "Bed", garden, new Vector3(0f, 0.76f, 0f), new Vector3(1.24f, 0.04f, 0.84f), darkWood);
            Prim(PrimitiveType.Cube, "BorderBack", garden, new Vector3(0f, 0.81f, -0.39f), new Vector3(1.24f, 0.08f, 0.06f), wood);
            Prim(PrimitiveType.Cube, "BorderFront", garden, new Vector3(0f, 0.81f, 0.39f), new Vector3(1.24f, 0.08f, 0.06f), wood);
            Prim(PrimitiveType.Cube, "BorderLeft", garden, new Vector3(-0.59f, 0.81f, 0f), new Vector3(0.06f, 0.08f, 0.72f), wood);
            Prim(PrimitiveType.Cube, "BorderRight", garden, new Vector3(0.59f, 0.81f, 0f), new Vector3(0.06f, 0.08f, 0.72f), wood);
            foreach (var leg in new[] { new Vector3(-0.55f, 0f, -0.35f), new Vector3(0.55f, 0f, -0.35f), new Vector3(-0.55f, 0f, 0.35f), new Vector3(0.55f, 0f, 0.35f) })
                Deco(PrimitiveType.Cube, "Leg", garden, new Vector3(leg.x, 0.37f, leg.z), new Vector3(0.06f, 0.74f, 0.06f), darkWood);
            Deco(PrimitiveType.Sphere, "GardenRock", garden, new Vector3(0.38f, 0.8f, -0.2f), new Vector3(0.16f, 0.09f, 0.12f), stoneA);
            Deco(PrimitiveType.Sphere, "GardenRock2", garden, new Vector3(0.26f, 0.79f, -0.24f), new Vector3(0.08f, 0.05f, 0.07f), stoneB);

            var sandFrame = Group(h.interactives, "SandRake", StationRight, FacingViewer(StationRight));
            var sandQuad = Prim(PrimitiveType.Quad, "Sand", sandFrame, new Vector3(0f, 0.785f, 0f), new Vector3(1.12f, 0.72f, 1f), Mat("ZenSandCanvas", Color.white, 0.12f));
            sandQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            StripColliders(sandQuad);
            var sandSurface = Group(sandFrame, "SandSurface", new Vector3(0f, 0.785f, 0f), Quaternion.identity);

            var rake = new GameObject("Rake");
            rake.transform.SetParent(sandFrame, false);
            rake.transform.localPosition = new Vector3(-0.1f, 0.84f, 0.12f);
            var head = Prim(PrimitiveType.Cube, "Head", rake.transform, Vector3.zero, new Vector3(0.32f, 0.025f, 0.03f), wood);
            var tines = new List<Transform>();
            for (var t = 0; t < 5; t++)
            {
                var tx = -0.12f + t * 0.06f;
                Deco(PrimitiveType.Cylinder, "Tine", rake.transform, new Vector3(tx, -0.025f, 0f), new Vector3(0.012f, 0.02f, 0.012f), darkWood);
                tines.Add(Group(rake.transform, "TineTip" + t, new Vector3(tx, -0.045f, 0f), Quaternion.identity));
            }

            var handle = Prim(PrimitiveType.Cylinder, "Handle", rake.transform, new Vector3(0f, 0.075f, 0.35f), new Vector3(0.025f, 0.36f, 0.025f), bamboo);
            handle.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
            StripColliders(rake);
            head.AddComponent<BoxCollider>();
            handle.AddComponent<CapsuleCollider>();
            GrabProp(rake, 0.5f);

            var raking = sandFrame.gameObject.AddComponent<ZenSandRake>();
            SetActivity(raking, h.id, "zen_areia", "Jardim de areia");
            raking.sandRenderer = sandQuad.GetComponent<Renderer>();
            raking.sandSurface = sandSurface;
            raking.sandSize = new Vector2(1.12f, 0.72f);
            raking.tines = tines.ToArray();
            StationLabel(sandFrame, "Desenhe ondas com o rastelo", new Vector3(0f, 1.15f, -0.2f), 1.0f, h.labelColor);

            // Singing bowls (front)
            StationTable(h, "BowlTable", StationFront, new Vector3(1.3f, 0.62f, 0.55f), wood, darkWood);
            var bowlFrame = Group(h.interactives, "SingingBowls", StationFront, FacingViewer(StationFront));
            var bronze = Mat("ZenBronze", new Color(0.62f, 0.45f, 0.22f), 0.75f, 0.85f);
            var bowlInner = Mat("ZenBowlInner", new Color(0.24f, 0.17f, 0.09f), 0.6f, 0.7f);
            var cushion = Mat("ZenCushion", new Color(0.55f, 0.12f, 0.1f), 0.2f);
            var ringGlow = TransparentGlow("ZenRipple", new Color(1f, 0.85f, 0.5f), 0.45f);
            var rims = new List<Transform>();
            var ripples = new List<Transform>();
            var diameters = new[] { 0.26f, 0.22f, 0.18f };
            for (var i = 0; i < 3; i++)
            {
                var x = -0.38f + i * 0.38f;
                var d = diameters[i];
                var bh = 0.06f + d * 0.15f;
                Deco(PrimitiveType.Cylinder, "Cushion", bowlFrame, new Vector3(x, 0.632f, -0.02f), new Vector3(d + 0.06f, 0.012f, d + 0.06f), cushion);
                var bowl = Prim(PrimitiveType.Cylinder, "Bowl" + i, bowlFrame, new Vector3(x, 0.644f + bh * 0.5f, -0.02f), new Vector3(d, bh * 0.5f, d), bronze);
                BoxOnly(bowl);
                Deco(PrimitiveType.Cylinder, "Inner", bowlFrame, new Vector3(x, 0.645f + bh, -0.02f), new Vector3(d * 0.86f, 0.002f, d * 0.86f), bowlInner);
                rims.Add(Group(bowlFrame, "Rim" + i, new Vector3(x, 0.645f + bh, -0.02f), Quaternion.identity));
                var ripple = Deco(PrimitiveType.Cylinder, "Ripple" + i, bowlFrame, new Vector3(x, 0.65f + bh, -0.02f), new Vector3(d * 1.05f, 0.002f, d * 1.05f), ringGlow);
                ripple.SetActive(false);
                ripples.Add(ripple.transform);
            }

            var mallet = new GameObject("Mallet");
            mallet.transform.SetParent(bowlFrame, false);
            mallet.transform.localPosition = new Vector3(0f, 0.648f, 0.18f);
            var stick = Prim(PrimitiveType.Cylinder, "Stick", mallet.transform, Vector3.zero, new Vector3(0.022f, 0.15f, 0.022f), wood);
            stick.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var malletHead = Prim(PrimitiveType.Sphere, "Head", mallet.transform, new Vector3(0.165f, 0f, 0f), Vector3.one * 0.055f, Mat("ZenFelt", new Color(0.42f, 0.2f, 0.14f), 0.1f));
            StripColliders(mallet);
            stick.AddComponent<CapsuleCollider>();
            malletHead.AddComponent<SphereCollider>();
            GrabProp(mallet, 0.25f);

            var bowlsActivity = bowlFrame.gameObject.AddComponent<ZenSingingBowls>();
            SetActivity(bowlsActivity, h.id, "zen_tacas", "Taças tibetanas");
            bowlsActivity.bowls = rims.ToArray();
            bowlsActivity.ripples = ripples.ToArray();
            bowlsActivity.malletHead = malletHead.transform;
            StationLabel(bowlFrame, "Toque as taças com calma: 3 s entre toques", new Vector3(0f, 1.05f, -0.1f), 1.0f, h.labelColor);

            ReturnStation(h);
            CameraStation(h);
            BuildHousePlaque(h.interactives, h.info, new Vector3(0f, 2.3f, 4.38f), new[] { "Pilha de pedras", "Jardim de areia", "Taças tibetanas" });
            return h.transition;
        }

        // ---------------------------------------------------------------- Cyber / Estratégico

        static HouseTransition BuildCyberHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Cyber);
            h.labelColor = new Color(0.55f, 0.95f, 1f);
            var floorA = Mat("CyberFloor", new Color(0.03f, 0.035f, 0.06f), 0.88f, 0.3f);
            var floorB = Mat("CyberFloorAlt", new Color(0.04f, 0.048f, 0.08f), 0.88f, 0.3f);
            var panel = Mat("CyberPanel", new Color(0.05f, 0.06f, 0.09f), 0.75f, 0.4f);
            var dark = Mat("CyberChip", new Color(0.05f, 0.06f, 0.09f), 0.85f, 0.6f);
            var cyan = Glow("CyberGlow", new Color(0.1f, 0.85f, 1f), 2.2f);
            var cyanDim = Glow("CyberGlowDim", new Color(0.1f, 0.85f, 1f), 0.6f);
            var magenta = Glow("CyberMagenta", new Color(1f, 0.2f, 0.8f), 2f);
            var yellow = Glow("CyberYellow", new Color(1f, 0.85f, 0.2f), 1.8f);
            var green = Glow("CyberGreen", new Color(0.3f, 1f, 0.45f), 1.8f);
            var holo = TransparentGlow("CyberHolo", new Color(0.2f, 0.9f, 1f), 0.25f);

            Floor(h, floorA, floorB);
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    var tile = h.pieces.Find("Floor_" + (x + 1) + (z + 1));
                    Deco(PrimitiveType.Cube, "LineX", tile, new Vector3(0f, 0.004f, 1.49f), new Vector3(3f, 0.004f, 0.02f), cyanDim);
                    Deco(PrimitiveType.Cube, "LineZ", tile, new Vector3(1.49f, 0.004f, 0f), new Vector3(0.02f, 0.004f, 3f), cyanDim);
                    Deco(PrimitiveType.Cube, "LineMid", tile, new Vector3(0f, 0.004f, 0f), new Vector3(0.01f, 0.004f, 3f), cyanDim);
                }
            }

            Walls(h, panel, 3.2f, (seg, side, i, height) =>
            {
                Deco(PrimitiveType.Cube, "StripLow", seg, new Vector3(0f, 0.25f, -0.01f), new Vector3(3f, 0.025f, 0.02f), cyan);
                Deco(PrimitiveType.Cube, "StripHigh", seg, new Vector3(0f, 2.6f, -0.01f), new Vector3(3f, 0.025f, 0.02f), (side + i) % 2 == 0 ? magenta : cyan);
                Deco(PrimitiveType.Cube, "Seam", seg, new Vector3(1.48f, height * 0.5f, -0.01f), new Vector3(0.03f, height, 0.02f), cyanDim);
                if (i == 0 && (side == 1 || side == 3))
                {
                    Deco(PrimitiveType.Quad, "Screen", seg, new Vector3(0f, 1.6f, -0.02f), new Vector3(1.8f, 1f, 1f), holo);
                    for (var b = 0; b < 6; b++)
                    {
                        var bh = 0.15f + ((b * 37) % 7) * 0.09f;
                        Deco(PrimitiveType.Cube, "Bar", seg, new Vector3(-0.6f + b * 0.24f, 1.18f + bh * 0.5f, -0.03f), new Vector3(0.12f, bh, 0.01f), b % 2 == 0 ? cyan : magenta);
                    }
                }
            });

            for (var c = 0; c < 3; c++)
            {
                var ceiling = Piece(h, "Ceiling_" + c, new Vector3((c - 1) * 3f, 0f, 0f));
                Deco(PrimitiveType.Cube, "Panel", ceiling, new Vector3(0f, 3.22f, 0f), new Vector3(3f, 0.04f, 9f), panel);
                Deco(PrimitiveType.Cube, "Light", ceiling, new Vector3(0f, 3.19f, 0f), new Vector3(0.05f, 0.02f, 8.6f), cyan);
            }

            foreach (var corner in new[] { new Vector3(-3.7f, 0f, 3.7f), new Vector3(3.7f, 0f, 3.7f), new Vector3(-3.7f, 0f, -3.7f), new Vector3(3.7f, 0f, -3.7f) })
            {
                var column = Piece(h, "DataColumn", corner);
                Deco(PrimitiveType.Cube, "Core", column, new Vector3(0f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 0.4f), dark);
                Deco(PrimitiveType.Cube, "Glow", column, new Vector3(0f, 1.6f, 0f), new Vector3(0.42f, 3.0f, 0.06f), cyanDim);
                var rings = Group(column, "Rings", Vector3.zero, Quaternion.identity);
                for (var r = 0; r < 5; r++)
                    Deco(PrimitiveType.Cube, "Ring", rings, new Vector3(0f, 0.4f + r * 0.6f, 0f), new Vector3(0.62f, 0.025f, 0.62f), r % 2 == 0 ? cyan : magenta, new Vector3(0f, r * 18f, 0f));
                Floating(rings.gameObject, 0.06f, 0.15f, 30f, corner.x);
            }

            foreach (var rackPos in new[] { new Vector3(-2.4f, 0f, -3.9f), new Vector3(2.4f, 0f, -3.9f) })
            {
                var rack = Piece(h, "ServerRack", rackPos);
                Deco(PrimitiveType.Cube, "Body", rack, new Vector3(0f, 1f, 0f), new Vector3(1.1f, 2f, 0.6f), dark);
                for (var r = 0; r < 8; r++)
                {
                    Deco(PrimitiveType.Cube, "Unit", rack, new Vector3(0f, 0.25f + r * 0.22f, 0.305f), new Vector3(1f, 0.16f, 0.01f), panel);
                    Deco(PrimitiveType.Cube, "Led", rack, new Vector3(-0.4f + (r % 3) * 0.12f, 0.25f + r * 0.22f, 0.312f), new Vector3(0.04f, 0.02f, 0.005f), r % 3 == 0 ? magenta : green);
                }
            }

            var holos = Piece(h, "Holograms", new Vector3(0f, 0f, 0.5f));
            var rng = new System.Random(7);
            for (var i = 0; i < 12; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var r = 2.4f + (float)rng.NextDouble() * 1.4f;
                var s = 0.12f + (float)rng.NextDouble() * 0.2f;
                var cube = Deco(PrimitiveType.Cube, "Holo", holos, new Vector3(Mathf.Cos(a) * r, 2.1f + (float)rng.NextDouble() * 0.8f, Mathf.Sin(a) * r), Vector3.one * s, i % 3 == 0 ? cyan : holo, new Vector3(45f, i * 20f, 45f));
                Floating(cube, 0.1f, 0.1f + (float)rng.NextDouble() * 0.15f, 25f + i * 3f, i);
            }

            var lightPiece = Piece(h, "CeilingLight", new Vector3(0f, 0f, 0.8f));
            var light = new GameObject("PointLight").AddComponent<Light>();
            light.transform.SetParent(lightPiece, false);
            light.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.85f, 1f);
            light.intensity = 2.4f;
            light.range = 7f;
            light.shadows = LightShadows.None;

            // Sequence panel (left)
            var stand = Piece(h, "PanelStand", StationLeft);
            stand.localRotation = FacingViewer(StationLeft);
            Deco(PrimitiveType.Cylinder, "Base", stand, new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.02f, 0.5f), dark);
            Deco(PrimitiveType.Cube, "Pillar", stand, new Vector3(0f, 0.55f, -0.05f), new Vector3(0.12f, 1.1f, 0.12f), dark);
            Deco(PrimitiveType.Cube, "Edge", stand, new Vector3(0f, 0.55f, 0.012f), new Vector3(0.02f, 1.1f, 0.01f), cyan);

            var seqFrame = Group(h.interactives, "SequencePanel", StationLeft, FacingViewer(StationLeft));
            Deco(PrimitiveType.Cube, "Backplate", seqFrame, new Vector3(0f, 1.3f, -0.04f), new Vector3(0.8f, 0.62f, 0.03f), dark);
            Deco(PrimitiveType.Cube, "FrameTop", seqFrame, new Vector3(0f, 1.61f, -0.03f), new Vector3(0.82f, 0.015f, 0.035f), cyan);
            Deco(PrimitiveType.Cube, "FrameBottom", seqFrame, new Vector3(0f, 0.99f, -0.03f), new Vector3(0.82f, 0.015f, 0.035f), cyan);
            var nodeMat = LitMat("CyberNode", new Color(0.05f, 0.2f, 0.28f), 0.6f, 0f, true, Color.black);
            var nodes = new List<XRSimpleInteractable>();
            var nodeRenderers = new List<Renderer>();
            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var node = Prim(PrimitiveType.Sphere, "Node" + (row * 3 + col), seqFrame, new Vector3(-0.24f + col * 0.24f, 1.44f - row * 0.24f, 0.02f), Vector3.one * 0.1f, nodeMat);
                    AddPoke(node);
                    nodes.Add(node.GetComponent<XRSimpleInteractable>());
                    nodeRenderers.Add(node.GetComponent<Renderer>());
                }
            }

            var start = Prim(PrimitiveType.Cube, "StartButton", seqFrame, new Vector3(0f, 1.06f, 0.01f), new Vector3(0.18f, 0.06f, 0.05f), green);
            AddPoke(start);
            StationLabel(seqFrame, "INICIAR", new Vector3(0f, 1.06f, 0.04f), 0.35f, new Color(0.02f, 0.1f, 0.04f)).fontStyle = TMPro.FontStyles.Bold;
            var seqDisplay = StationLabel(seqFrame, "TOQUE INICIAR", new Vector3(0f, 1.7f, -0.02f), 0.9f, h.labelColor);
            seqDisplay.fontStyle = TMPro.FontStyles.Bold;
            StationLabel(seqFrame, "Memorize e repita a sequência", new Vector3(0f, 1.84f, -0.02f), 0.75f, Color.white);
            var sequence = seqFrame.gameObject.AddComponent<CyberSequencePanel>();
            SetActivity(sequence, h.id, "cyber_sequencia", "Sequência de memória");
            sequence.nodes = nodes.ToArray();
            sequence.nodeRenderers = nodeRenderers.ToArray();
            sequence.startButton = start.GetComponent<XRSimpleInteractable>();
            sequence.display = seqDisplay;

            // Data sockets (right)
            StationTable(h, "SocketConsole", StationRight, new Vector3(1.0f, 0.85f, 0.6f), dark, panel);
            var socketFrame = Group(h.interactives, "DataSockets", StationRight, FacingViewer(StationRight));
            var socketColors = new[] { cyan, magenta, yellow, green };
            var order = new[] { 2, 0, 3, 1 };
            var sockets = new List<CyberSocket>();
            var cubes = new List<ResettablePose>();
            for (var i = 0; i < 4; i++)
            {
                var sx = -0.33f + i * 0.22f;
                Deco(PrimitiveType.Cylinder, "Pad", socketFrame, new Vector3(sx, 0.853f, -0.15f), new Vector3(0.12f, 0.003f, 0.12f), panel);
                Deco(PrimitiveType.Cylinder, "Ring", socketFrame, new Vector3(sx, 0.856f, -0.15f), new Vector3(0.1f, 0.003f, 0.1f), socketColors[i]);
                var socketGo = new GameObject("Socket" + i);
                socketGo.transform.SetParent(socketFrame, false);
                socketGo.transform.localPosition = new Vector3(sx, 0.9f, -0.15f);
                var trigger = socketGo.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.07f;
                var socket = socketGo.AddComponent<CyberSocket>();

                var cx = -0.33f + order[i] * 0.22f;
                var cube = Prim(PrimitiveType.Cube, "DataCube" + i, socketFrame, new Vector3(cx, 0.888f, 0.15f), Vector3.one * 0.07f, socketColors[i]);
                GrabProp(cube, 0.2f);
                socket.accepts = cube.transform;
                sockets.Add(socket);
                cubes.Add(cube.GetComponent<ResettablePose>());
            }

            var socketDisplay = StationLabel(socketFrame, "CONECTE OS DADOS", new Vector3(0f, 1.25f, -0.2f), 0.9f, h.labelColor);
            socketDisplay.fontStyle = TMPro.FontStyles.Bold;
            StationLabel(socketFrame, "Cada cubo no anel da mesma cor, contra o relógio", new Vector3(0f, 1.38f, -0.2f), 0.75f, Color.white);
            var socketsActivity = socketFrame.gameObject.AddComponent<CyberDataSockets>();
            SetActivity(socketsActivity, h.id, "cyber_sockets", "Conexão de dados");
            socketsActivity.sockets = sockets.ToArray();
            socketsActivity.cubes = cubes.ToArray();
            socketsActivity.display = socketDisplay;

            // Drone targets (front)
            var rackCenter = new Vector3(0f, 0f, 1.35f);
            StationTable(h, "DiscRack", rackCenter, new Vector3(0.8f, 0.9f, 0.45f), dark, panel);
            var orbit = Group(h.interactives, "DroneTargets", new Vector3(0f, 1.8f, 3.0f), Quaternion.identity);
            var droneMats = new[] { magenta, cyan, yellow };
            var drones = new List<Transform>();
            for (var i = 0; i < 3; i++)
            {
                var drone = Group(h.interactives, "Drone" + i, new Vector3(0f, 1.8f, 3.0f + 0.7f + i * 0.3f), Quaternion.identity);
                Deco(PrimitiveType.Sphere, "Body", drone, Vector3.zero, Vector3.one * 0.14f, droneMats[i]);
                Deco(PrimitiveType.Cylinder, "Ring", drone, Vector3.zero, new Vector3(0.32f, 0.006f, 0.32f), dark);
                for (var r = 0; r < 4; r++)
                {
                    var a = r * 90f * Mathf.Deg2Rad;
                    Deco(PrimitiveType.Cylinder, "Rotor", drone, new Vector3(Mathf.Cos(a) * 0.15f, 0.02f, Mathf.Sin(a) * 0.15f), new Vector3(0.08f, 0.004f, 0.08f), cyanDim);
                }

                drones.Add(drone);
            }

            var discs = new List<ResettablePose>();
            var rackFrame = Group(h.interactives, "DiscRackItems", rackCenter, FacingViewer(rackCenter));
            for (var i = 0; i < 3; i++)
            {
                var disc = Prim(PrimitiveType.Cylinder, "Disc" + i, rackFrame, new Vector3(-0.24f + i * 0.24f, 0.912f, 0f), new Vector3(0.16f, 0.008f, 0.16f), i == 1 ? magenta : cyan);
                BoxOnly(disc);
                GrabProp(disc, 0.15f, 4f);
                discs.Add(disc.GetComponent<ResettablePose>());
            }

            StationLabel(rackFrame, "Arremesse os discos nos drones", new Vector3(0f, 1.2f, 0f), 1.0f, h.labelColor);
            var dronesActivity = orbit.gameObject.AddComponent<CyberDroneTargets>();
            SetActivity(dronesActivity, h.id, "cyber_drones", "Drones acertados");
            dronesActivity.orbitCenter = orbit;
            dronesActivity.drones = drones.ToArray();
            dronesActivity.discs = discs.ToArray();

            ReturnStation(h);
            CameraStation(h);
            BuildHousePlaque(h.interactives, h.info, new Vector3(0f, 2.55f, 4.38f), new[] { "Sequência de memória", "Conexão de dados", "Drones" });
            return h.transition;
        }

        // ---------------------------------------------------------------- Excêntrico / Criativo

        static HouseTransition BuildEccentricHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Excentrico);
            h.labelColor = new Color(0.18f, 0.08f, 0.3f);
            var colors = Palette();
            var floorA = Mat("EccFloorA", new Color(0.98f, 0.86f, 0.35f), 0.4f);
            var floorB = Mat("EccFloorB", new Color(0.35f, 0.82f, 0.8f), 0.4f);
            var wood = Mat("EccWood", new Color(0.62f, 0.42f, 0.26f), 0.35f);
            var white = Mat("EccWhite", new Color(0.97f, 0.96f, 0.94f), 0.3f);
            var wallMats = new Material[colors.Length];
            var shapeMats = new Material[colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                wallMats[i] = Mat("EccWall" + i, Color.Lerp(colors[i], Color.white, 0.35f), 0.3f);
                shapeMats[i] = Mat("Ecc" + i, colors[i], 0.55f);
            }

            Floor(h, floorA, floorB);
            Walls(h, wallMats[0], 3f, (seg, side, i, height) =>
            {
                var k = (side * 3 + i + 1) % colors.Length;
                seg.Find("Slab").GetComponent<Renderer>().sharedMaterial = wallMats[k];
                for (var d = 0; d < 4; d++)
                {
                    var dx = -1.1f + d * 0.72f + ((side + d) % 2) * 0.15f;
                    var dy = 0.7f + ((d * 5 + side * 3 + i) % 4) * 0.45f;
                    var size = 0.18f + ((d + side) % 3) * 0.1f;
                    Deco(PrimitiveType.Sphere, "Dot", seg, new Vector3(dx, dy, -0.005f), new Vector3(size, size, 0.02f), shapeMats[(k + d + 2) % colors.Length]);
                }

                for (var z = 0; z < 6; z++)
                    Deco(PrimitiveType.Cube, "Zig", seg, new Vector3(-1.25f + z * 0.5f, 2.55f, -0.01f), new Vector3(0.36f, 0.06f, 0.02f), shapeMats[(k + 3) % colors.Length], new Vector3(0f, 0f, z % 2 == 0 ? 35f : -35f));
            });

            var rng = new System.Random(3);
            for (var g = 0; g < 3; g++)
            {
                var cluster = Piece(h, "FloatingShapes_" + g, new Vector3((g - 1) * 2.6f, 0f, 1.2f + (g % 2) * 1.2f));
                for (var s = 0; s < 5; s++)
                {
                    var type = (PrimitiveType)new[] { 0, 3, 1, 2 }[(s + g) % 4];
                    var pos = new Vector3((float)rng.NextDouble() * 1.6f - 0.8f, 2.0f + (float)rng.NextDouble() * 0.8f, (float)rng.NextDouble() * 1.6f - 0.8f);
                    var shape = Deco(type, "Shape", cluster, pos, Vector3.one * (0.14f + (float)rng.NextDouble() * 0.16f), shapeMats[(s + g * 2) % colors.Length], new Vector3(30f * s, 15f * g, 0f));
                    var f = shape.AddComponent<FloatSpin>();
                    f.spinAxis = new Vector3((float)rng.NextDouble(), 1f, (float)rng.NextDouble()).normalized;
                    f.spinSpeed = 30f + s * 12f;
                    f.bobHeight = 0.08f;
                    f.bobSpeed = 0.2f + s * 0.04f;
                    f.phase = s + g;
                }
            }

            foreach (var spot in new[] { new Vector3(-3.4f, 0f, 2.9f), new Vector3(3.4f, 0f, -2.6f) })
            {
                var easel = Piece(h, "Easel", spot, spot.x < 0 ? 35f : -140f);
                Deco(PrimitiveType.Cube, "LegL", easel, new Vector3(-0.3f, 0.8f, 0f), new Vector3(0.04f, 1.65f, 0.04f), wood, new Vector3(0f, 0f, -8f));
                Deco(PrimitiveType.Cube, "LegR", easel, new Vector3(0.3f, 0.8f, 0f), new Vector3(0.04f, 1.65f, 0.04f), wood, new Vector3(0f, 0f, 8f));
                Deco(PrimitiveType.Cube, "LegBack", easel, new Vector3(0f, 0.75f, 0.3f), new Vector3(0.04f, 1.6f, 0.04f), wood, new Vector3(-18f, 0f, 0f));
                Deco(PrimitiveType.Cube, "Canvas", easel, new Vector3(0f, 1.25f, -0.04f), new Vector3(0.8f, 0.6f, 0.03f), white, new Vector3(-6f, 0f, 0f));
                for (var b = 0; b < 4; b++)
                    Deco(PrimitiveType.Sphere, "Blob", easel, new Vector3(-0.25f + b * 0.17f, 1.2f + (b % 2) * 0.12f, -0.062f), new Vector3(0.16f, 0.12f, 0.01f), shapeMats[(b + (spot.x < 0 ? 0 : 3)) % colors.Length], new Vector3(-6f, 0f, b * 25f));
            }

            var tower = Piece(h, "SpiralTower", new Vector3(3.4f, 0f, 3.3f));
            var spiral = Group(tower, "Spiral", Vector3.zero, Quaternion.identity);
            for (var s = 0; s < 14; s++)
                Deco(PrimitiveType.Cube, "Step", spiral, new Vector3(0f, 0.15f + s * 0.18f, 0f), new Vector3(0.6f - s * 0.025f, 0.14f, 0.18f), shapeMats[s % colors.Length], new Vector3(0f, s * 26f, 0f));
            Floating(spiral.gameObject, 0f, 0f, 14f, 0f);

            var pencil = Piece(h, "GiantPencil", new Vector3(-3.5f, 0f, -3.2f));
            Deco(PrimitiveType.Cylinder, "Body", pencil, new Vector3(0.35f, 1.2f, 0f), new Vector3(0.22f, 1.2f, 0.22f), shapeMats[1], new Vector3(0f, 0f, -16f));
            Deco(PrimitiveType.Sphere, "Eraser", pencil, new Vector3(1.0f, 2.45f, 0f), new Vector3(0.22f, 0.2f, 0.22f), shapeMats[0]);

            var lightPiece = Piece(h, "ColorLight", new Vector3(0f, 0f, 0.8f));
            var light = new GameObject("PointLight").AddComponent<Light>();
            light.transform.SetParent(lightPiece, false);
            light.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.85f);
            light.intensity = 1.8f;
            light.range = 7f;
            light.shadows = LightShadows.None;

            // Paint wall (front)
            var easelWall = Piece(h, "PaintWallFrame", new Vector3(0f, 0f, 3.5f));
            Prim(PrimitiveType.Cube, "Frame", easelWall, new Vector3(0f, 1.55f, 0f), new Vector3(2.2f, 1.2f, 0.06f), wood);
            Deco(PrimitiveType.Cube, "LegL", easelWall, new Vector3(-0.9f, 0.5f, 0.05f), new Vector3(0.08f, 1f, 0.08f), wood);
            Deco(PrimitiveType.Cube, "LegR", easelWall, new Vector3(0.9f, 0.5f, 0.05f), new Vector3(0.08f, 1f, 0.08f), wood);
            for (var s = 0; s < 6; s++)
                Deco(PrimitiveType.Sphere, "FloorSplat", easelWall, new Vector3(-0.8f + s * 0.32f, 0.004f, -0.3f - (s % 2) * 0.2f), new Vector3(0.3f, 0.004f, 0.22f), shapeMats[s % colors.Length]);

            var paintFrame = Group(h.interactives, "PaintWall", Vector3.zero, Quaternion.identity);
            var canvasQuad = Prim(PrimitiveType.Quad, "Canvas", paintFrame, new Vector3(0f, 1.55f, 3.465f), new Vector3(2f, 1f, 1f), Mat("PaintCanvas", Color.white, 0.2f));
            StripColliders(canvasQuad);
            var canvasFace = Group(paintFrame, "CanvasFace", new Vector3(0f, 1.55f, 3.465f), Quaternion.identity);
            var trayCenter = new Vector3(0f, 0f, 1.45f);
            StationTable(h, "BallTray", trayCenter, new Vector3(0.9f, 0.85f, 0.45f), wood, wood);
            var trayFrame = Group(h.interactives, "BallTrayItems", trayCenter, FacingViewer(trayCenter));
            Deco(PrimitiveType.Cube, "RimBack", trayFrame, new Vector3(0f, 0.87f, -0.12f), new Vector3(0.8f, 0.04f, 0.02f), white);
            var bounce = Friction("PaintBall", 0.6f, 0.05f);
            var balls = new List<ResettablePose>();
            var ballColors = new List<Color>();
            for (var i = 0; i < colors.Length; i++)
            {
                var ball = Prim(PrimitiveType.Sphere, "PaintBall" + i, trayFrame, new Vector3(-0.3f + i * 0.12f, 0.9f, 0f), Vector3.one * 0.09f, Mat("Paint" + i, colors[i], 0.85f));
                ball.GetComponent<Collider>().sharedMaterial = bounce;
                GrabProp(ball, 0.15f, 5f);
                balls.Add(ball.GetComponent<ResettablePose>());
                ballColors.Add(colors[i]);
            }

            var paint = paintFrame.gameObject.AddComponent<PaintSplatWall>();
            SetActivity(paint, h.id, "ecc_tinta", "Tela de respingos");
            paint.canvasRenderer = canvasQuad.GetComponent<Renderer>();
            paint.canvasFace = canvasFace;
            paint.canvasSize = new Vector2(2f, 1f);
            paint.balls = balls.ToArray();
            paint.ballColors = ballColors.ToArray();
            StationLabel(trayFrame, "Arremesse a tinta na tela", new Vector3(0f, 1.15f, 0f), 1.0f, h.labelColor);

            // Magnetic sculpture (left)
            StationTable(h, "SculptTable", StationLeft, new Vector3(0.9f, 0.8f, 0.7f), white, wood);
            var sculptFrame = Group(h.interactives, "MagnetSculpture", StationLeft, FacingViewer(StationLeft));
            var shapeDefs = new (PrimitiveType type, Vector3 scale, bool convex)[]
            {
                (PrimitiveType.Cube, Vector3.one * 0.08f, false),
                (PrimitiveType.Sphere, Vector3.one * 0.085f, false),
                (PrimitiveType.Capsule, new Vector3(0.05f, 0.05f, 0.05f), false),
                (PrimitiveType.Cylinder, new Vector3(0.08f, 0.035f, 0.08f), true),
                (PrimitiveType.Cube, new Vector3(0.16f, 0.04f, 0.04f), false),
                (PrimitiveType.Sphere, new Vector3(0.11f, 0.06f, 0.08f), true),
                (PrimitiveType.Cube, new Vector3(0.06f, 0.09f, 0.06f), false),
                (PrimitiveType.Cylinder, new Vector3(0.045f, 0.07f, 0.045f), true)
            };
            var shapes = new List<Rigidbody>();
            for (var i = 0; i < shapeDefs.Length; i++)
            {
                var def = shapeDefs[i];
                var a = i * 45f * Mathf.Deg2Rad;
                var halfHeight = def.type == PrimitiveType.Capsule ? def.scale.y : def.type == PrimitiveType.Cylinder ? def.scale.y : def.scale.y * 0.5f;
                var shape = Prim(def.type, "Shape" + i, sculptFrame, new Vector3(Mathf.Cos(a) * 0.27f, 0.8f + halfHeight + 0.004f, Mathf.Sin(a) * 0.22f - 0.02f), def.scale, shapeMats[i % colors.Length]);
                shape.transform.localRotation = Quaternion.Euler(0f, i * 31f, 0f);
                if (def.convex)
                    ConvexOnly(shape);
                GrabProp(shape, 0.2f);
                shape.AddComponent<CollisionRelay>();
                shapes.Add(shape.GetComponent<Rigidbody>());
            }

            var release = Prim(PrimitiveType.Cylinder, "ReleaseButton", sculptFrame, new Vector3(0.36f, 0.815f, 0.27f), new Vector3(0.08f, 0.015f, 0.08f), Glow("EccRelease", new Color(1f, 0.25f, 0.25f), 1.2f));
            BoxOnly(release);
            AddPoke(release);
            StationLabel(sculptFrame, "Soltar", new Vector3(0.36f, 0.88f, 0.27f), 0.4f, h.labelColor);
            var magnet = sculptFrame.gameObject.AddComponent<MagnetSculpture>();
            SetActivity(magnet, h.id, "ecc_escultura", "Escultura magnética");
            magnet.shapes = shapes.ToArray();
            magnet.releaseButton = release.GetComponent<XRSimpleInteractable>();
            StationLabel(sculptFrame, "Encoste as peças: elas grudam", new Vector3(0f, 1.15f, -0.1f), 1.0f, h.labelColor);

            // Shape xylophone (right)
            var xyloBase = Piece(h, "XyloBase", StationRight);
            Deco(PrimitiveType.Cylinder, "Platform", xyloBase, new Vector3(0f, 0.025f, 0f), new Vector3(1.2f, 0.025f, 1.2f), shapeMats[3]);
            Deco(PrimitiveType.Cylinder, "Inner", xyloBase, new Vector3(0f, 0.052f, 0f), new Vector3(0.9f, 0.003f, 0.9f), shapeMats[1]);
            var xyloFrame = Group(h.interactives, "ShapeXylophone", StationRight, FacingViewer(StationRight));
            var rainbow = new[]
            {
                new Color(1f, 0.25f, 0.25f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.88f, 0.2f), new Color(0.35f, 0.9f, 0.3f),
                new Color(0.2f, 0.85f, 0.95f), new Color(0.3f, 0.45f, 1f), new Color(0.7f, 0.35f, 1f)
            };
            var keys = new List<Transform>();
            for (var i = 0; i < rainbow.Length; i++)
            {
                var a = Mathf.Lerp(-60f, 60f, i / 6f) * Mathf.Deg2Rad;
                var type = i % 3 == 0 ? PrimitiveType.Sphere : i % 3 == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule;
                var scale = type == PrimitiveType.Capsule ? new Vector3(0.08f, 0.06f, 0.08f) : Vector3.one * (type == PrimitiveType.Cube ? 0.1f : 0.11f);
                var key = Prim(type, "Key" + i, xyloFrame, new Vector3(Mathf.Sin(a) * 0.5f, 1.0f + (i % 2) * 0.09f, -Mathf.Cos(a) * 0.5f + 0.3f), scale, Glow("Xylo" + i, rainbow[i], 0.5f));
                AddPoke(key);
                Floating(key, 0.012f, 0.3f, 20f + i * 4f, i * 0.4f);
                keys.Add(key.transform);
            }

            var malletStand = Group(xyloFrame, "MalletStand", new Vector3(0f, 0f, 0.5f), Quaternion.identity);
            Prim(PrimitiveType.Cube, "Top", malletStand, new Vector3(0f, 0.75f, 0f), new Vector3(0.42f, 0.04f, 0.2f), white);
            Deco(PrimitiveType.Cylinder, "Leg", malletStand, new Vector3(0f, 0.37f, 0f), new Vector3(0.06f, 0.37f, 0.06f), wood);
            var malletHeads = new List<Transform>();
            for (var m = 0; m < 2; m++)
            {
                var stickRoot = new GameObject("Mallet" + m);
                stickRoot.transform.SetParent(malletStand, false);
                stickRoot.transform.localPosition = new Vector3(-0.08f, 0.785f, -0.05f + m * 0.1f);
                var stick = Prim(PrimitiveType.Cylinder, "Stick", stickRoot.transform, Vector3.zero, new Vector3(0.018f, 0.12f, 0.018f), wood);
                stick.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var headGo = Prim(PrimitiveType.Sphere, "Head", stickRoot.transform, new Vector3(0.135f, 0f, 0f), Vector3.one * 0.045f, shapeMats[m == 0 ? 0 : 2]);
                StripColliders(stickRoot);
                stick.AddComponent<CapsuleCollider>();
                headGo.AddComponent<SphereCollider>();
                GrabProp(stickRoot, 0.15f);
                malletHeads.Add(headGo.transform);
            }

            var xylo = xyloFrame.gameObject.AddComponent<ShapeXylophone>();
            SetActivity(xylo, h.id, "ecc_xilofone", "Xilofone de formas");
            xylo.keys = keys.ToArray();
            xylo.malletHeads = malletHeads.ToArray();
            StationLabel(xyloFrame, "Toque as formas com as baquetas ou a mão", new Vector3(0f, 1.38f, -0.1f), 1.0f, h.labelColor);

            ReturnStation(h);
            CameraStation(h);
            BuildHousePlaque(h.interactives, h.info, new Vector3(0f, 2.55f, 4.38f), new[] { "Tela de respingos", "Escultura magnética", "Xilofone de formas" });
            return h.transition;
        }

        // ---------------------------------------------------------------- Acolhedor / Empático

        static HouseTransition BuildCozyHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Acolhedor);
            h.labelColor = new Color(0.32f, 0.17f, 0.08f);
            var floorA = Mat("CozyFloorA", new Color(0.5f, 0.32f, 0.2f), 0.45f);
            var floorB = Mat("CozyFloorB", new Color(0.46f, 0.29f, 0.18f), 0.45f);
            var seamMat = Mat("CozySeam", new Color(0.28f, 0.17f, 0.1f), 0.3f);
            var wallMat = Mat("CozyWall", new Color(0.93f, 0.85f, 0.72f), 0.2f);
            var wood = Mat("CozyWood", new Color(0.42f, 0.26f, 0.15f), 0.45f);
            var darkWood = Mat("CozyDarkWood", new Color(0.26f, 0.15f, 0.09f), 0.4f);
            var stone = Mat("CozyStone", new Color(0.55f, 0.5f, 0.46f), 0.25f);
            var soot = Mat("CozySoot", new Color(0.08f, 0.06f, 0.05f), 0.1f);
            var fabric = Mat("CozyFabric", new Color(0.62f, 0.3f, 0.22f), 0.1f);
            var fabricB = Mat("CozyFabricB", new Color(0.86f, 0.74f, 0.55f), 0.1f);
            var green = Mat("CozyLeaf", new Color(0.25f, 0.5f, 0.25f), 0.3f);
            var terracotta = Mat("CozyTerracotta", new Color(0.72f, 0.38f, 0.24f), 0.25f);
            var warmGlow = Glow("CozyWarmGlow", new Color(1f, 0.72f, 0.38f), 1.8f);
            var windowGlow = Glow("CozyWindow", new Color(0.45f, 0.58f, 0.85f), 0.8f);

            Floor(h, floorA, floorB);
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    var tile = h.pieces.Find("Floor_" + (x + 1) + (z + 1));
                    for (var s = 0; s < 4; s++)
                        Deco(PrimitiveType.Cube, "Seam", tile, new Vector3(-1.125f + s * 0.75f, 0.003f, 0f), new Vector3(0.012f, 0.004f, 3f), seamMat);
                }
            }

            var rug = Piece(h, "Rug", new Vector3(0f, 0f, 0.5f));
            Deco(PrimitiveType.Cylinder, "Outer", rug, new Vector3(0f, 0.006f, 0f), new Vector3(3.4f, 0.004f, 2.6f), terracotta);
            Deco(PrimitiveType.Cylinder, "Inner", rug, new Vector3(0f, 0.008f, 0f), new Vector3(2.8f, 0.004f, 2.0f), fabricB);

            Walls(h, wallMat, 3f, (seg, side, i, height) =>
            {
                Deco(PrimitiveType.Cube, "Wainscot", seg, new Vector3(0f, 0.45f, -0.015f), new Vector3(3f, 0.9f, 0.03f), wood);
                Deco(PrimitiveType.Cube, "Rail", seg, new Vector3(0f, 0.92f, -0.03f), new Vector3(3f, 0.05f, 0.06f), darkWood);
                Deco(PrimitiveType.Cube, "Crown", seg, new Vector3(0f, height - 0.05f, -0.03f), new Vector3(3f, 0.1f, 0.06f), darkWood);
                if (i == 0 && (side == 1 || side == 3))
                {
                    Deco(PrimitiveType.Cube, "Glass", seg, new Vector3(0f, 1.75f, -0.01f), new Vector3(1.3f, 1.0f, 0.02f), windowGlow);
                    Deco(PrimitiveType.Cube, "FrameH", seg, new Vector3(0f, 1.75f, -0.03f), new Vector3(1.4f, 0.05f, 0.04f), wallMat);
                    Deco(PrimitiveType.Cube, "FrameV", seg, new Vector3(0f, 1.75f, -0.03f), new Vector3(0.05f, 1.1f, 0.04f), wallMat);
                    Deco(PrimitiveType.Cube, "Sill", seg, new Vector3(0f, 1.22f, -0.06f), new Vector3(1.5f, 0.05f, 0.12f), darkWood);
                }
                else if (side == 2 && i != 0)
                {
                    Deco(PrimitiveType.Cube, "Picture", seg, new Vector3(0f, 1.8f, -0.02f), new Vector3(0.7f, 0.5f, 0.03f), darkWood);
                    Deco(PrimitiveType.Cube, "Art", seg, new Vector3(0f, 1.8f, -0.04f), new Vector3(0.6f, 0.4f, 0.01f), i > 0 ? green : terracotta);
                }
            });

            for (var b = 0; b < 3; b++)
            {
                var beam = Piece(h, "Beam_" + b, new Vector3((b - 1) * 3f, 0f, 0f));
                Deco(PrimitiveType.Cube, "Beam", beam, new Vector3(0f, 2.85f, 0f), new Vector3(0.2f, 0.22f, 9f), darkWood);
                Deco(PrimitiveType.Cube, "Ceiling", beam, new Vector3(0f, 3.02f, 0f), new Vector3(3f, 0.03f, 9f), wood);
            }

            var lights = Piece(h, "StringLights", new Vector3(0f, 0f, 0.5f));
            for (var i = 0; i < 16; i++)
            {
                var u = i / 15f;
                var x = Mathf.Lerp(-3.8f, 3.8f, u);
                var sag = Mathf.Sin(u * Mathf.PI * 2f) * 0.5f;
                Deco(PrimitiveType.Sphere, "Bulb", lights, new Vector3(x, 2.6f - Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f)) * 0.25f, sag), Vector3.one * 0.05f, warmGlow);
            }

            var sofa = Piece(h, "Sofa", new Vector3(0f, 0f, -3.4f));
            Deco(PrimitiveType.Cube, "Base", sofa, new Vector3(0f, 0.22f, 0f), new Vector3(2.3f, 0.36f, 0.9f), fabric);
            Deco(PrimitiveType.Cube, "Back", sofa, new Vector3(0f, 0.62f, -0.36f), new Vector3(2.3f, 0.5f, 0.2f), fabric);
            Deco(PrimitiveType.Cube, "ArmL", sofa, new Vector3(-1.1f, 0.5f, 0f), new Vector3(0.2f, 0.36f, 0.9f), fabric);
            Deco(PrimitiveType.Cube, "ArmR", sofa, new Vector3(1.1f, 0.5f, 0f), new Vector3(0.2f, 0.36f, 0.9f), fabric);
            for (var c = 0; c < 3; c++)
                Deco(PrimitiveType.Cube, "Cushion", sofa, new Vector3(-0.66f + c * 0.66f, 0.45f, 0.04f), new Vector3(0.62f, 0.1f, 0.72f), fabricB);
            Deco(PrimitiveType.Cube, "Pillow", sofa, new Vector3(-0.75f, 0.62f, -0.18f), new Vector3(0.35f, 0.3f, 0.1f), green, new Vector3(-10f, 10f, 8f));
            Deco(PrimitiveType.Cube, "Pillow2", sofa, new Vector3(0.75f, 0.62f, -0.18f), new Vector3(0.35f, 0.3f, 0.1f), terracotta, new Vector3(-10f, -10f, -8f));
            var blanket = Deco(PrimitiveType.Cube, "Blanket", sofa, new Vector3(0.95f, 0.55f, 0.1f), new Vector3(0.4f, 0.04f, 0.8f), Mat("CozyBlanket", new Color(0.85f, 0.85f, 0.78f), 0.1f), new Vector3(0f, 0f, 30f));
            blanket.name = "Blanket";

            var shelf = Piece(h, "Bookshelf", new Vector3(4.15f, 0f, -1.4f), -90f);
            Deco(PrimitiveType.Cube, "Frame", shelf, new Vector3(0f, 1f, 0.1f), new Vector3(1.4f, 2f, 0.35f), darkWood);
            var bookColors = new[] { terracotta, green, fabricB, fabric, Mat("CozyBookBlue", new Color(0.25f, 0.35f, 0.55f), 0.3f) };
            for (var row = 0; row < 4; row++)
            {
                for (var bk = 0; bk < 9; bk++)
                {
                    var bh = 0.28f + ((bk * 7 + row) % 4) * 0.03f;
                    Deco(PrimitiveType.Cube, "Book", shelf, new Vector3(-0.58f + bk * 0.14f, 0.12f + row * 0.48f + bh * 0.5f, -0.08f), new Vector3(0.1f, bh, 0.24f), bookColors[(bk + row) % bookColors.Length]);
                }
            }

            var lamp = Piece(h, "FloorLamp", new Vector3(-3.4f, 0f, -3.3f));
            Deco(PrimitiveType.Cylinder, "Foot", lamp, new Vector3(0f, 0.02f, 0f), new Vector3(0.35f, 0.02f, 0.35f), darkWood);
            Deco(PrimitiveType.Cylinder, "Pole", lamp, new Vector3(0f, 0.8f, 0f), new Vector3(0.04f, 0.8f, 0.04f), darkWood);
            Deco(PrimitiveType.Cylinder, "Shade", lamp, new Vector3(0f, 1.68f, 0f), new Vector3(0.5f, 0.18f, 0.5f), warmGlow);

            foreach (var spot in new[] { new Vector3(-3.7f, 0f, 3.7f), new Vector3(3.7f, 0f, 3.7f), new Vector3(3.7f, 0f, -3.7f) })
            {
                var plant = Piece(h, "BigPlant", spot);
                Deco(PrimitiveType.Cylinder, "Pot", plant, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.25f, 0.5f), terracotta);
                for (var l = 0; l < 5; l++)
                {
                    var a = l * 72f * Mathf.Deg2Rad;
                    Deco(PrimitiveType.Sphere, "Leaf", plant, new Vector3(Mathf.Cos(a) * 0.22f, 0.8f + l * 0.12f, Mathf.Sin(a) * 0.22f), new Vector3(0.45f, 0.18f, 0.25f), green, new Vector3(0f, l * 72f, 25f));
                }
            }

            // Plants (left)
            StationTable(h, "PlantBench", StationLeft, new Vector3(1.6f, 0.6f, 0.45f), wood, darkWood);
            var plantFrame = Group(h.interactives, "PlantCare", StationLeft, FacingViewer(StationLeft));
            var pots = new List<Transform>();
            var growth = new List<Transform>();
            var flowers = new List<GameObject>();
            var petalMats = new[] { Mat("CozyPetalPink", new Color(1f, 0.55f, 0.7f), 0.3f), Mat("CozyPetalYellow", new Color(1f, 0.85f, 0.3f), 0.3f), Mat("CozyPetalLilac", new Color(0.75f, 0.55f, 1f), 0.3f) };
            var soil = Mat("CozySoil", new Color(0.2f, 0.13f, 0.08f), 0.1f);
            var flowerCenter = Mat("CozyFlowerCenter", new Color(1f, 0.75f, 0.2f), 0.3f);
            for (var i = 0; i < 3; i++)
            {
                var x = -0.2f + i * 0.38f;
                var pot = Prim(PrimitiveType.Cylinder, "Pot" + i, plantFrame, new Vector3(x, 0.69f, 0f), new Vector3(0.2f, 0.09f, 0.2f), terracotta);
                BoxOnly(pot);
                Deco(PrimitiveType.Cylinder, "Soil", plantFrame, new Vector3(x, 0.781f, 0f), new Vector3(0.17f, 0.004f, 0.17f), soil);
                pots.Add(Group(plantFrame, "PotTop" + i, new Vector3(x, 0.785f, 0f), Quaternion.identity));
                var grow = Group(plantFrame, "Growth" + i, new Vector3(x, 0.785f, 0f), Quaternion.identity);
                Deco(PrimitiveType.Cylinder, "Stem", grow, new Vector3(0f, 0.09f, 0f), new Vector3(0.015f, 0.09f, 0.015f), green);
                for (var l = 0; l < 3; l++)
                    Deco(PrimitiveType.Sphere, "Leaf", grow, new Vector3((l - 1) * 0.04f, 0.06f + l * 0.045f, 0f), new Vector3(0.07f, 0.02f, 0.035f), green, new Vector3(0f, l * 60f, (l - 1) * 30f));
                var flower = Group(grow, "Flower", new Vector3(0f, 0.19f, 0f), Quaternion.identity);
                for (var p = 0; p < 5; p++)
                {
                    var a = p * 72f * Mathf.Deg2Rad;
                    Deco(PrimitiveType.Sphere, "Petal", flower, new Vector3(Mathf.Cos(a) * 0.03f, 0f, Mathf.Sin(a) * 0.03f), new Vector3(0.04f, 0.012f, 0.03f), petalMats[i], new Vector3(0f, -p * 72f, 0f));
                }

                Deco(PrimitiveType.Sphere, "Center", flower, Vector3.zero, Vector3.one * 0.025f, flowerCenter);
                flower.gameObject.SetActive(false);
                growth.Add(grow);
                flowers.Add(flower.gameObject);
            }

            var can = new GameObject("WateringCan");
            can.transform.SetParent(plantFrame, false);
            can.transform.localPosition = new Vector3(-0.6f, 0.602f, 0f);
            var canMetal = Mat("CozyCan", new Color(0.3f, 0.55f, 0.45f), 0.6f, 0.5f);
            Prim(PrimitiveType.Cylinder, "Body", can.transform, new Vector3(0f, 0.075f, 0f), new Vector3(0.13f, 0.075f, 0.13f), canMetal);
            Prim(PrimitiveType.Capsule, "Handle", can.transform, new Vector3(-0.02f, 0.17f, 0f), new Vector3(0.02f, 0.06f, 0.02f), canMetal).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var spoutDir = new Vector3(0.866f, 0.5f, 0f);
            var spoutBase = new Vector3(0.06f, 0.06f, 0f);
            Prim(PrimitiveType.Cylinder, "SpoutPipe", can.transform, spoutBase + spoutDir * 0.09f, new Vector3(0.022f, 0.09f, 0.022f), canMetal).transform.localRotation = Quaternion.Euler(0f, 0f, -60f);
            Prim(PrimitiveType.Cylinder, "Rose", can.transform, spoutBase + spoutDir * 0.18f, new Vector3(0.04f, 0.01f, 0.04f), canMetal).transform.localRotation = Quaternion.Euler(0f, 0f, -60f);
            var spout = Group(can.transform, "Spout", spoutBase + spoutDir * 0.19f, Quaternion.LookRotation(spoutDir, Vector3.up));
            StripColliders(can);
            var canBox = can.AddComponent<BoxCollider>();
            canBox.size = new Vector3(0.14f, 0.2f, 0.14f);
            canBox.center = new Vector3(0f, 0.09f, 0f);
            GrabProp(can, 0.5f);
            var water = Particles(spout, "Water", Vector3.zero, Quaternion.identity, ParticleMat("Water", new Color(0.55f, 0.78f, 1f, 0.8f), false));
            var main = water.main;
            main.playOnAwake = false;
            main.startLifetime = 0.7f;
            main.startSpeed = 1.1f;
            main.startSize = 0.025f;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;
            var emission = water.emission;
            emission.rateOverTime = 60f;
            var shape = water.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.008f;

            var plantCare = plantFrame.gameObject.AddComponent<PlantCare>();
            SetActivity(plantCare, h.id, "aco_plantas", "Plantas floridas");
            plantCare.wateringCan = can.GetComponent<ResettablePose>();
            plantCare.spout = spout;
            plantCare.water = water;
            plantCare.pots = pots.ToArray();
            plantCare.growParts = growth.ToArray();
            plantCare.flowers = flowers.ToArray();
            StationLabel(plantFrame, "Incline o regador sobre os vasos", new Vector3(0.1f, 1.08f, 0f), 1.0f, h.labelColor);

            // Cat (right)
            var ottoman = Piece(h, "CatCorner", StationRight);
            ottoman.localRotation = FacingViewer(StationRight);
            Prim(PrimitiveType.Cube, "Ottoman", ottoman, new Vector3(0.1f, 0.2f, -0.05f), new Vector3(0.7f, 0.4f, 0.55f), fabric);
            Deco(PrimitiveType.Cylinder, "Cushion", ottoman, new Vector3(0.1f, 0.42f, -0.05f), new Vector3(0.55f, 0.025f, 0.45f), fabricB);
            Deco(PrimitiveType.Cylinder, "SideTop", ottoman, new Vector3(-0.55f, 0.6f, 0.25f), new Vector3(0.4f, 0.015f, 0.4f), wood);
            Deco(PrimitiveType.Cylinder, "SidePole", ottoman, new Vector3(-0.55f, 0.3f, 0.25f), new Vector3(0.05f, 0.3f, 0.05f), darkWood);
            var sideTop = Prim(PrimitiveType.Cube, "SideTopCollider", ottoman, new Vector3(-0.55f, 0.6f, 0.25f), new Vector3(0.36f, 0.03f, 0.36f), wood);
            sideTop.GetComponent<Renderer>().enabled = false;

            var catFrame = Group(h.interactives, "ServeTheCat", StationRight, FacingViewer(StationRight));
            var cat = Group(catFrame, "Cat", new Vector3(0.1f, 0.44f, -0.05f), Quaternion.identity);
            var fur = Mat("CatFur", new Color(0.92f, 0.55f, 0.25f), 0.15f);
            var furLight = Mat("CatFurLight", new Color(0.98f, 0.92f, 0.85f), 0.15f);
            Deco(PrimitiveType.Sphere, "Body", cat, new Vector3(0f, 0.1f, 0f), new Vector3(0.24f, 0.2f, 0.32f), fur);
            Deco(PrimitiveType.Sphere, "Chest", cat, new Vector3(0f, 0.12f, 0.11f), new Vector3(0.16f, 0.15f, 0.12f), furLight);
            Deco(PrimitiveType.Sphere, "PawL", cat, new Vector3(-0.05f, 0.025f, 0.15f), new Vector3(0.05f, 0.035f, 0.07f), furLight);
            Deco(PrimitiveType.Sphere, "PawR", cat, new Vector3(0.05f, 0.025f, 0.15f), new Vector3(0.05f, 0.035f, 0.07f), furLight);
            var catHead = Prim(PrimitiveType.Sphere, "Head", cat, new Vector3(0f, 0.25f, 0.13f), new Vector3(0.17f, 0.15f, 0.15f), fur);
            catHead.AddComponent<XRSimpleInteractable>();
            Deco(PrimitiveType.Cube, "EarL", cat, new Vector3(-0.05f, 0.325f, 0.12f), new Vector3(0.05f, 0.05f, 0.02f), fur, new Vector3(0f, 0f, 45f));
            Deco(PrimitiveType.Cube, "EarR", cat, new Vector3(0.05f, 0.325f, 0.12f), new Vector3(0.05f, 0.05f, 0.02f), fur, new Vector3(0f, 0f, 45f));
            var eyeMat = Glow("CatEye", new Color(0.35f, 0.7f, 0.3f), 0.4f);
            var eyeL = Deco(PrimitiveType.Sphere, "EyeL", cat, new Vector3(-0.035f, 0.265f, 0.2f), new Vector3(0.03f, 0.032f, 0.012f), eyeMat);
            var eyeR = Deco(PrimitiveType.Sphere, "EyeR", cat, new Vector3(0.035f, 0.265f, 0.2f), new Vector3(0.03f, 0.032f, 0.012f), eyeMat);
            Deco(PrimitiveType.Sphere, "Muzzle", cat, new Vector3(0f, 0.225f, 0.195f), new Vector3(0.07f, 0.045f, 0.04f), furLight);
            Deco(PrimitiveType.Sphere, "Nose", cat, new Vector3(0f, 0.24f, 0.215f), new Vector3(0.018f, 0.012f, 0.01f), Mat("CatNose", new Color(0.95f, 0.5f, 0.55f), 0.4f));
            var tailPivot = Group(cat, "TailPivot", new Vector3(0f, 0.07f, -0.15f), Quaternion.Euler(-50f, 0f, 0f));
            Deco(PrimitiveType.Capsule, "Tail", tailPivot, new Vector3(0f, 0.12f, 0f), new Vector3(0.04f, 0.12f, 0.04f), fur);
            var mouth = Group(cat, "Mouth", new Vector3(0f, 0.22f, 0.24f), Quaternion.identity);

            var mug = new GameObject("CocoaMug");
            mug.transform.SetParent(catFrame, false);
            mug.transform.localPosition = new Vector3(-0.55f, 0.616f, 0.25f);
            var mugMat = Mat("CozyMug", new Color(0.86f, 0.42f, 0.28f), 0.7f);
            Prim(PrimitiveType.Cylinder, "Body", mug.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.085f, 0.05f, 0.085f), mugMat);
            Prim(PrimitiveType.Capsule, "Handle", mug.transform, new Vector3(0.052f, 0.05f, 0f), new Vector3(0.016f, 0.026f, 0.016f), mugMat);
            Prim(PrimitiveType.Cylinder, "Cocoa", mug.transform, new Vector3(0f, 0.096f, 0f), new Vector3(0.074f, 0.003f, 0.074f), Mat("CozyCocoa", new Color(0.3f, 0.17f, 0.09f), 0.8f));
            Prim(PrimitiveType.Cube, "Marshmallow", mug.transform, new Vector3(0.012f, 0.1f, 0.008f), Vector3.one * 0.018f, furLight);
            var steamMat = TransparentGlow("CozySteam", new Color(1f, 1f, 1f), 0.25f);
            var steamRoot = Group(mug.transform, "Steam", Vector3.zero, Quaternion.identity);
            for (var s = 0; s < 3; s++)
            {
                var puff = Prim(PrimitiveType.Sphere, "Puff" + s, steamRoot, new Vector3((s - 1) * 0.015f, 0.13f + s * 0.03f, 0f), Vector3.one * (0.025f + s * 0.008f), steamMat);
                Floating(puff, 0.012f, 0.5f + s * 0.2f, 20f, s);
            }

            StripColliders(mug);
            var mugBox = mug.AddComponent<BoxCollider>();
            mugBox.size = new Vector3(0.1f, 0.1f, 0.09f);
            mugBox.center = new Vector3(0.008f, 0.05f, 0f);
            GrabProp(mug, 0.3f);

            var serve = catFrame.gameObject.AddComponent<ServeTheCat>();
            SetActivity(serve, h.id, "aco_gato", "Chocolate para o gato");
            serve.catMouth = mouth;
            serve.mug = mug.GetComponent<ResettablePose>();
            serve.catHead = catHead.GetComponent<XRSimpleInteractable>();
            serve.tail = tailPivot;
            serve.eyes = new[] { eyeL.transform, eyeR.transform };
            StationLabel(catFrame, "Leve o chocolate ao gato e faça carinho", new Vector3(-0.15f, 1.05f, 0f), 1.0f, h.labelColor);

            // Fireplace (front)
            var fp = Piece(h, "Fireplace", new Vector3(0f, 0f, 4.14f));
            Prim(PrimitiveType.Cube, "Hearth", fp, new Vector3(0f, 0.05f, -0.05f), new Vector3(1.8f, 0.1f, 0.75f), stone);
            Prim(PrimitiveType.Cube, "PillarL", fp, new Vector3(-0.65f, 0.675f, 0f), new Vector3(0.3f, 1.15f, 0.6f), stone);
            Prim(PrimitiveType.Cube, "PillarR", fp, new Vector3(0.65f, 0.675f, 0f), new Vector3(0.3f, 1.15f, 0.6f), stone);
            Prim(PrimitiveType.Cube, "Back", fp, new Vector3(0f, 0.675f, 0.25f), new Vector3(1.0f, 1.15f, 0.1f), soot);
            Deco(PrimitiveType.Cube, "Lintel", fp, new Vector3(0f, 1.36f, 0f), new Vector3(1.6f, 0.22f, 0.62f), stone);
            Deco(PrimitiveType.Cube, "Mantle", fp, new Vector3(0f, 1.5f, -0.06f), new Vector3(1.9f, 0.07f, 0.38f), darkWood);
            Deco(PrimitiveType.Cube, "Chimney", fp, new Vector3(0f, 2.3f, 0.07f), new Vector3(1.2f, 1.6f, 0.5f), stone);
            Deco(PrimitiveType.Cylinder, "Grate1", fp, new Vector3(0f, 0.15f, -0.02f), new Vector3(0.09f, 0.3f, 0.09f), soot, new Vector3(0f, 20f, 90f));
            Deco(PrimitiveType.Cylinder, "Grate2", fp, new Vector3(0.05f, 0.22f, 0.04f), new Vector3(0.08f, 0.26f, 0.08f), Mat("Log", new Color(0.38f, 0.24f, 0.14f), 0.2f), new Vector3(0f, -25f, 90f));
            var candleMat = Mat("CozyWax", new Color(0.98f, 0.93f, 0.8f), 0.3f);
            for (var c = 0; c < 3; c++)
            {
                var cx = -0.7f + c * 0.15f;
                var ch = 0.08f + c * 0.03f;
                Deco(PrimitiveType.Cylinder, "Candle", fp, new Vector3(cx, 1.535f + ch * 0.5f, -0.1f), new Vector3(0.045f, ch * 0.5f, 0.045f), candleMat);
                Deco(PrimitiveType.Sphere, "Flame", fp, new Vector3(cx, 1.55f + ch, -0.1f), new Vector3(0.015f, 0.03f, 0.015f), warmGlow);
            }

            var crateCenter = new Vector3(1.3f, 0f, 3.0f);
            var crate = Piece(h, "LogCrate", crateCenter);
            crate.localRotation = FacingViewer(crateCenter);
            Prim(PrimitiveType.Cube, "Crate", crate, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.4f, 0.45f), wood);
            for (var sl = 0; sl < 3; sl++)
                Deco(PrimitiveType.Cube, "Slat", crate, new Vector3(0f, 0.08f + sl * 0.13f, 0.228f), new Vector3(0.62f, 0.05f, 0.01f), darkWood);

            var fireFrame = Group(h.interactives, "Fireplace", Vector3.zero, Quaternion.identity);
            var hearth = Group(fireFrame, "HearthVolume", new Vector3(0f, 0.45f, 4.05f), Quaternion.identity);
            var fire = Particles(fireFrame, "Fire", new Vector3(0f, 0.14f, 4.05f), Quaternion.Euler(-90f, 0f, 0f), ParticleMat("Fire", new Color(1f, 0.55f, 0.18f, 1f), true));
            var fireMain = fire.main;
            fireMain.playOnAwake = true;
            fireMain.loop = true;
            fireMain.startLifetime = 0.9f;
            fireMain.startSpeed = 0.55f;
            fireMain.startSize = 0.2f;
            fireMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.3f));
            fireMain.gravityModifier = -0.1f;
            fireMain.maxParticles = 160;
            var fireEmission = fire.emission;
            fireEmission.rateOverTime = 20f;
            var fireShape = fire.shape;
            fireShape.shapeType = ParticleSystemShapeType.Cone;
            fireShape.angle = 10f;
            fireShape.radius = 0.22f;
            var fade = fire.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var shrink = fire.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var fireLight = new GameObject("FireLight").AddComponent<Light>();
            fireLight.transform.SetParent(fireFrame, false);
            fireLight.transform.localPosition = new Vector3(0f, 0.7f, 3.7f);
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.58f, 0.28f);
            fireLight.intensity = 1.5f;
            fireLight.range = 5.5f;
            fireLight.shadows = LightShadows.None;

            var logMat = Mat("Log", new Color(0.38f, 0.24f, 0.14f), 0.2f);
            var logs = new List<ResettablePose>();
            for (var i = 0; i < 4; i++)
            {
                var layer = i < 3 ? 0 : 1;
                var lz = i < 3 ? (i - 1) * 0.1f : 0f;
                var log = Prim(PrimitiveType.Cylinder, "Log" + i, fireFrame, crateCenter + new Vector3(0f, 0.442f + layer * 0.08f, lz), new Vector3(0.08f, 0.17f, 0.08f), logMat);
                log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                GrabProp(log, 0.6f);
                logs.Add(log.GetComponent<ResettablePose>());
            }

            var fireplace = fireFrame.gameObject.AddComponent<Fireplace>();
            SetActivity(fireplace, h.id, "aco_lareira", "Lareira acesa");
            fireplace.hearth = hearth;
            fireplace.hearthHalfExtents = new Vector3(0.45f, 0.4f, 0.3f);
            fireplace.fire = fire;
            fireplace.fireLight = fireLight;
            fireplace.logs = logs.ToArray();
            var crateLabel = Group(h.interactives, "CrateLabel", crateCenter, FacingViewer(crateCenter));
            StationLabel(crateLabel, "Jogue lenha na lareira", new Vector3(0f, 0.8f, 0f), 1.0f, h.labelColor);

            ReturnStation(h);
            CameraStation(h);
            BuildHousePlaque(h.interactives, h.info, new Vector3(0f, 2.45f, 3.92f), new[] { "Plantas floridas", "Chocolate para o gato", "Lareira acesa" });
            return h.transition;
        }

        static void SetActivity(ProfileActivity activity, ProfileId id, string activityId, string displayName)
        {
            activity.profile = id;
            activity.activityId = activityId;
            activity.displayName = displayName;
        }
    }
}
