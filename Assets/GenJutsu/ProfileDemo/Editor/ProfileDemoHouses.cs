using UnityEngine;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public static partial class ProfileDemoBuilder
    {
        const float Deck = 6.4f;
        const float Interior = 5.05f;
        const float PostReach = 2.52f;
        const float MatPitch = 1.64f;
        const float MatSize = 1.52f;

        static HouseTransition BuildZenHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Zen);
            var shell = BuildSharedShell(h, new Color(1f, 0.86f, 0.62f), 1.35f);
            ZenAccents(h, shell);
            RoomLight(shell.structure, new Color(1f, 0.9f, 0.75f), 0.35f, 7f);
            return h.transition;
        }

        static HouseTransition BuildCyberHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Cyber);
            var shell = BuildSharedShell(h, new Color(0.75f, 0.9f, 1f), 0.55f);
            CyberAccents(h, shell);
            RoomLight(shell.structure, new Color(0.45f, 0.8f, 1f), 0.22f, 6.5f);
            return h.transition;
        }

        static HouseTransition BuildEccentricHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Excentrico);
            var shell = BuildSharedShell(h, new Color(1f, 0.9f, 0.78f), 1.1f);
            EccentricAccents(h, shell);
            RoomLight(shell.structure, new Color(1f, 0.86f, 0.9f), 0.28f, 7f);
            return h.transition;
        }

        static HouseTransition BuildCozyHouse(Transform parent)
        {
            var h = NewHouse(parent, ProfileId.Acolhedor);
            var shell = BuildSharedShell(h, new Color(1f, 0.72f, 0.42f), 1.7f);
            CozyAccents(h, shell);
            RoomLight(shell.structure, new Color(1f, 0.72f, 0.42f), 0.7f, 7.5f);
            return h.transition;
        }

        struct Shell
        {
            public Transform platform;
            public Transform structure;
            public Transform accents;
            public Material wood;
            public Material woodDark;
            public Material leaf;
            public Material pot;
        }

        static Shell BuildSharedShell(House h, Color lantern, float lanternEmission)
        {
            var wood = Mat("DojoWood", new Color(0.40f, 0.26f, 0.15f), 0.32f);
            var woodDark = Mat("DojoWoodDark", new Color(0.16f, 0.10f, 0.07f), 0.22f);
            var tatami = Mat("DojoTatami", new Color(0.78f, 0.75f, 0.55f), 0.08f);
            var border = Mat("DojoTatamiEdge", new Color(0.27f, 0.40f, 0.26f), 0.12f);
            var paper = ShojiPaper();
            var table = Mat("DojoLowTable", new Color(0.95f, 0.94f, 0.91f), 0.22f);
            var lamp = Glow("DojoLantern_" + h.info.Key, lantern, lanternEmission);

            var platform = Piece(h, "Platform", Vector3.zero);
            Deco(PrimitiveType.Cube, "Deck", platform, new Vector3(0f, -0.05f, 0f), new Vector3(Deck, 0.14f, Deck), woodDark);
            Deco(PrimitiveType.Cube, "Lip", platform, new Vector3(0f, 0.03f, -3.05f), new Vector3(Deck, 0.04f, 0.18f), wood);
            Deco(PrimitiveType.Cube, "TatamiField", platform, new Vector3(0f, 0.028f, 0f), new Vector3(Interior, 0.02f, Interior), border);
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    Deco(PrimitiveType.Cube, "Tatami", platform,
                        new Vector3(x * MatPitch, 0.042f, z * MatPitch),
                        new Vector3(MatSize, 0.016f, MatSize), tatami);
                }
            }

            var structure = Piece(h, "Frame", Vector3.zero);
            foreach (var x in new[] { -PostReach, PostReach })
            {
                foreach (var z in new[] { -PostReach, 0f, PostReach })
                    Post(structure, new Vector3(x, 0f, z), woodDark);
            }

            Post(structure, new Vector3(0f, 0f, PostReach), woodDark);
            Beam(structure, new Vector3(0f, 2.42f, -PostReach), new Vector3(Interior + 0.2f, 0.14f, 0.14f), woodDark);
            Beam(structure, new Vector3(0f, 2.42f, PostReach), new Vector3(Interior + 0.2f, 0.14f, 0.14f), woodDark);
            Beam(structure, new Vector3(0f, 2.5f, 0f), new Vector3(Interior + 0.2f, 0.12f, 0.12f), wood);
            Beam(structure, new Vector3(-PostReach, 2.42f, 0f), new Vector3(0.14f, 0.14f, Interior + 0.2f), woodDark);
            Beam(structure, new Vector3(PostReach, 2.42f, 0f), new Vector3(0.14f, 0.14f, Interior + 0.2f), woodDark);
            Beam(structure, new Vector3(0f, 2.5f, 0f), new Vector3(0.12f, 0.12f, Interior + 0.2f), wood);

            ShojiWall(structure, paper, wood, new Vector3(0f, 0f, PostReach - 0.06f), 0f);
            ShojiWall(structure, paper, wood, new Vector3(-PostReach + 0.06f, 0f, 0f), 90f);
            ShojiWall(structure, paper, wood, new Vector3(PostReach - 0.06f, 0f, 0f), -90f);

            var lanterns = Piece(h, "Lanterns", Vector3.zero);
            foreach (var p in new[]
                     {
                         new Vector3(-1.55f, 0f, -1.15f), new Vector3(1.55f, 0f, -1.15f),
                         new Vector3(-1.55f, 0f, 1.35f), new Vector3(1.55f, 0f, 1.35f)
                     })
                PaperLantern(lanterns, p, lamp, woodDark);

            var tablePiece = Piece(h, "LowTable", Vector3.zero);
            Deco(PrimitiveType.Cube, "Top", tablePiece, new Vector3(0f, 0.16f, 0.05f), new Vector3(1.05f, 0.1f, 1.05f), table);

            return new Shell
            {
                platform = platform,
                structure = structure,
                accents = Piece(h, "Accents", Vector3.zero),
                wood = wood,
                woodDark = woodDark,
                leaf = Mat("DojoLeaf", new Color(0.22f, 0.42f, 0.22f), 0.2f),
                pot = Mat("DojoPot", new Color(0.32f, 0.36f, 0.34f), 0.45f)
            };
        }

        static void Post(Transform parent, Vector3 xz, Material wood)
        {
            Deco(PrimitiveType.Cube, "Post", parent, new Vector3(xz.x, 1.26f, xz.z), new Vector3(0.12f, 2.36f, 0.12f), wood);
        }

        static void Beam(Transform parent, Vector3 pos, Vector3 scale, Material wood)
        {
            Deco(PrimitiveType.Cube, "Beam", parent, pos, scale, wood);
        }

        static void ShojiWall(Transform parent, Material paper, Material wood, Vector3 origin, float yaw)
        {
            var wall = Group(parent, "Shoji", origin, Quaternion.Euler(0f, yaw, 0f));
            Deco(PrimitiveType.Cube, "PaperL", wall, new Vector3(-0.78f, 1.22f, 0f), new Vector3(1.42f, 2.05f, 0.02f), paper);
            Deco(PrimitiveType.Cube, "PaperR", wall, new Vector3(0.78f, 1.22f, 0f), new Vector3(1.42f, 2.05f, 0.02f), paper);
            Deco(PrimitiveType.Cube, "Mullion", wall, new Vector3(0f, 1.22f, -0.012f), new Vector3(0.045f, 2.12f, 0.03f), wood);
            foreach (var y in new[] { 0.28f, 1.15f, 2.05f })
                Deco(PrimitiveType.Cube, "Rail", wall, new Vector3(0f, y, -0.012f), new Vector3(3.05f, 0.04f, 0.03f), wood);
            Deco(PrimitiveType.Cube, "Sill", wall, new Vector3(0f, 0.16f, 0f), new Vector3(3.15f, 0.08f, 0.06f), wood);
        }

        static void PaperLantern(Transform parent, Vector3 xz, Material paper, Material wood)
        {
            var hang = Group(parent, "Lantern", xz, Quaternion.identity);
            Deco(PrimitiveType.Cylinder, "Cord", hang, new Vector3(0f, 2.28f, 0f), new Vector3(0.012f, 0.12f, 0.012f), wood);
            Deco(PrimitiveType.Cylinder, "Cap", hang, new Vector3(0f, 2.12f, 0f), new Vector3(0.16f, 0.02f, 0.16f), wood);
            Deco(PrimitiveType.Cylinder, "Shade", hang, new Vector3(0f, 1.92f, 0f), new Vector3(0.22f, 0.16f, 0.22f), paper);
            Deco(PrimitiveType.Cylinder, "Foot", hang, new Vector3(0f, 1.74f, 0f), new Vector3(0.14f, 0.015f, 0.14f), wood);
        }

        static void RoomLight(Transform parent, Color color, float intensity, float range)
        {
            var go = new GameObject("Fill");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        static void ZenAccents(House h, Shell shell)
        {
            var root = shell.accents;
            Bonsai(root, new Vector3(-2.95f, 0.02f, -1.45f), shell);
            Bowl(root, new Vector3(-2.92f, 0.02f, 0.15f), shell.woodDark);
            Bowl(root, new Vector3(-2.62f, 0.02f, 0.72f), shell.woodDark);
            RockGarden(root, new Vector3(2.85f, 0.02f, -0.15f), shell.woodDark);
        }

        static void Bonsai(Transform parent, Vector3 pos, Shell shell)
        {
            var t = Group(parent, "Bonsai", pos, Quaternion.identity);
            Deco(PrimitiveType.Cylinder, "Pot", t, new Vector3(0f, 0.08f, 0f), new Vector3(0.34f, 0.07f, 0.34f), shell.pot);
            Deco(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0.02f, 0.24f, 0f), new Vector3(0.06f, 0.1f, 0.06f), shell.wood, new Vector3(0f, 0f, 12f));
            Deco(PrimitiveType.Sphere, "Crown", t, new Vector3(-0.02f, 0.4f, 0f), new Vector3(0.42f, 0.18f, 0.32f), shell.leaf);
            Deco(PrimitiveType.Sphere, "CrownB", t, new Vector3(0.14f, 0.34f, 0.04f), new Vector3(0.22f, 0.12f, 0.2f), shell.leaf);
        }

        static void Bowl(Transform parent, Vector3 pos, Material wood)
        {
            var metal = Mat("DojoBowl", new Color(0.62f, 0.64f, 0.66f), 0.72f, 0.85f);
            var t = Group(parent, "Bowl", pos, Quaternion.identity);
            Deco(PrimitiveType.Cylinder, "Body", t, new Vector3(0f, 0.045f, 0f), new Vector3(0.2f, 0.035f, 0.2f), metal);
            Deco(PrimitiveType.Cylinder, "Lip", t, new Vector3(0f, 0.08f, 0f), new Vector3(0.22f, 0.008f, 0.22f), metal);
            Deco(PrimitiveType.Cylinder, "Stand", t, new Vector3(0f, 0.012f, 0f), new Vector3(0.1f, 0.01f, 0.1f), wood);
        }

        static void RockGarden(Transform parent, Vector3 pos, Material wood)
        {
            var sand = Mat("DojoSand", new Color(0.86f, 0.82f, 0.7f), 0.08f);
            var stone = Mat("DojoStone", new Color(0.28f, 0.3f, 0.32f), 0.4f);
            var stoneB = Mat("DojoStoneLight", new Color(0.5f, 0.5f, 0.48f), 0.35f);
            var t = Group(parent, "RockGarden", pos, Quaternion.identity);
            Deco(PrimitiveType.Cylinder, "Tray", t, new Vector3(0f, 0.03f, 0f), new Vector3(0.95f, 0.02f, 0.95f), wood);
            Deco(PrimitiveType.Cylinder, "Sand", t, new Vector3(0f, 0.05f, 0f), new Vector3(0.82f, 0.015f, 0.82f), sand);
            Deco(PrimitiveType.Cube, "RakeA", t, new Vector3(-0.05f, 0.06f, 0.05f), new Vector3(0.55f, 0.004f, 0.02f), stoneB, new Vector3(0f, 28f, 0f));
            Deco(PrimitiveType.Cube, "RakeB", t, new Vector3(0.08f, 0.06f, -0.12f), new Vector3(0.48f, 0.004f, 0.02f), stoneB, new Vector3(0f, -18f, 0f));
            Deco(PrimitiveType.Sphere, "Stone", t, new Vector3(-0.12f, 0.09f, 0.08f), new Vector3(0.1f, 0.06f, 0.08f), stone);
            Deco(PrimitiveType.Sphere, "StoneB", t, new Vector3(0.1f, 0.08f, 0.02f), new Vector3(0.07f, 0.05f, 0.06f), stoneB);
            Deco(PrimitiveType.Sphere, "StoneC", t, new Vector3(0.02f, 0.1f, -0.08f), new Vector3(0.09f, 0.07f, 0.07f), stone);
            Deco(PrimitiveType.Sphere, "StoneD", t, new Vector3(0.16f, 0.075f, 0.12f), new Vector3(0.05f, 0.04f, 0.045f), stoneB);
        }

        static void CyberAccents(House h, Shell shell)
        {
            var root = shell.accents;
            var cyan = Glow("DojoNeonCyan", new Color(0.15f, 0.9f, 1f), 2.2f);
            var magenta = Glow("DojoNeonMagenta", new Color(0.75f, 0.25f, 1f), 1.6f);
            Deco(PrimitiveType.Cube, "NeonFront", root, new Vector3(0f, 0.045f, -2.35f), new Vector3(4.7f, 0.012f, 0.025f), cyan);
            Deco(PrimitiveType.Cube, "NeonBack", root, new Vector3(0f, 0.045f, 2.35f), new Vector3(4.7f, 0.012f, 0.025f), cyan);
            Deco(PrimitiveType.Cube, "NeonLeft", root, new Vector3(-2.35f, 0.045f, 0f), new Vector3(0.025f, 0.012f, 4.7f), cyan);
            Deco(PrimitiveType.Cube, "NeonPost", root, new Vector3(PostReach - 0.02f, 1.3f, PostReach - 0.02f), new Vector3(0.02f, 1.5f, 0.02f), magenta);

            var pane = NeonPane("DojoHolo", new Color(0.2f, 0.85f, 1f, 0.28f));
            var holo = Group(root, "Holo", new Vector3(-0.15f, 1.35f, PostReach - 0.18f), Quaternion.identity);
            Deco(PrimitiveType.Cube, "Pane", holo, Vector3.zero, new Vector3(1.15f, 0.62f, 0.012f), pane);
            var line = Glow("DojoHoloLine", new Color(0.5f, 1f, 1f), 1.4f);
            Deco(PrimitiveType.Cube, "LineA", holo, new Vector3(-0.15f, 0.12f, -0.01f), new Vector3(0.7f, 0.012f, 0.008f), line);
            Deco(PrimitiveType.Cube, "LineB", holo, new Vector3(0.1f, -0.05f, -0.01f), new Vector3(0.45f, 0.012f, 0.008f), line);
            Deco(PrimitiveType.Cube, "LineC", holo, new Vector3(-0.2f, -0.18f, -0.01f), new Vector3(0.28f, 0.08f, 0.008f), line);

            var cubeMat = Glow("DojoWireCube", new Color(0.35f, 0.95f, 1f), 1.5f);
            foreach (var p in new[] { new Vector3(-1.3f, 0.35f, 1.1f), new Vector3(1.45f, 0.42f, 0.4f), new Vector3(0.9f, 0.55f, -1.2f) })
            {
                var cube = Deco(PrimitiveType.Cube, "Node", root, p, Vector3.one * 0.14f, cubeMat);
                Floating(cube, 0.04f, 0.6f, 18f, p.x);
            }
        }

        static void EccentricAccents(House h, Shell shell)
        {
            var root = shell.accents;
            var pink = Mat("DojoPaintPink", new Color(1f, 0.25f, 0.62f), 0.3f);
            var blue = Mat("DojoPaintBlue", new Color(0.2f, 0.45f, 1f), 0.3f);
            var yellow = Mat("DojoPaintYellow", new Color(1f, 0.82f, 0.15f), 0.35f);
            var green = Mat("DojoPaintGreen", new Color(0.25f, 0.85f, 0.35f), 0.3f);
            Splat(root, new Vector3(-1.7f, 1.55f, PostReach - 0.1f), new Vector3(0.55f, 0.42f, 0.02f), pink, 18f);
            Splat(root, new Vector3(0.9f, 1.85f, PostReach - 0.1f), new Vector3(0.38f, 0.5f, 0.02f), blue, -12f);
            Splat(root, new Vector3(-PostReach + 0.1f, 1.4f, -0.4f), new Vector3(0.02f, 0.46f, 0.36f), yellow, 0f);
            Splat(root, new Vector3(PostReach - 0.1f, 1.15f, 0.8f), new Vector3(0.02f, 0.32f, 0.48f), green, 0f);

            var frame = Group(root, "Painting", new Vector3(1.15f, 1.45f, PostReach - 0.14f), Quaternion.identity);
            Deco(PrimitiveType.Cube, "Frame", frame, Vector3.zero, new Vector3(0.72f, 0.52f, 0.03f), shell.woodDark);
            Deco(PrimitiveType.Cube, "Canvas", frame, new Vector3(0f, 0f, -0.02f), new Vector3(0.58f, 0.38f, 0.01f), pink);

            var star = Glow("DojoShapeStar", new Color(0.2f, 0.55f, 1f), 0.4f);
            var a = Deco(PrimitiveType.Cube, "ShapeA", root, new Vector3(-0.85f, 0.85f, -0.35f), new Vector3(0.28f, 0.28f, 0.06f), star, new Vector3(0f, 20f, 45f));
            Floating(a, 0.05f, 0.45f, 22f, 0.2f);
            var disc = Mat("DojoShapeDisc", new Color(1f, 0.78f, 0.1f), 0.4f);
            var b = Deco(PrimitiveType.Cylinder, "ShapeB", root, new Vector3(0.7f, 0.95f, 0.55f), new Vector3(0.22f, 0.02f, 0.22f), disc, new Vector3(90f, 0f, 0f));
            Floating(b, 0.04f, 0.5f, 12f, 1f);
            var block = Mat("DojoShapeBlock", new Color(0.95f, 0.28f, 0.22f), 0.35f);
            var c = Deco(PrimitiveType.Cube, "ShapeC", root, new Vector3(0.15f, 0.72f, 0.85f), new Vector3(0.16f, 0.16f, 0.16f), block);
            Floating(c, 0.03f, 0.4f, 16f, 2f);
        }

        static void Splat(Transform parent, Vector3 pos, Vector3 scale, Material mat, float yaw)
        {
            Deco(PrimitiveType.Sphere, "Splat", parent, pos, scale, mat, new Vector3(0f, yaw, 0f));
        }

        static void CozyAccents(House h, Shell shell)
        {
            var root = shell.accents;
            Plant(root, new Vector3(-2.9f, 0.02f, 1.7f), shell);
            Plant(root, new Vector3(2.9f, 0.02f, -1.55f), shell);
            var cushion = Mat("DojoCushion", new Color(0.86f, 0.42f, 0.24f), 0.25f);
            var cushionB = Mat("DojoCushionWarm", new Color(0.93f, 0.72f, 0.48f), 0.22f);
            Deco(PrimitiveType.Cylinder, "Cushion", root, new Vector3(-0.72f, 0.07f, -0.55f), new Vector3(0.42f, 0.035f, 0.42f), cushion);
            Deco(PrimitiveType.Cylinder, "CushionB", root, new Vector3(0.78f, 0.07f, 0.62f), new Vector3(0.38f, 0.03f, 0.38f), cushionB);
        }

        static void Plant(Transform parent, Vector3 pos, Shell shell)
        {
            var t = Group(parent, "Plant", pos, Quaternion.identity);
            Deco(PrimitiveType.Cube, "Pot", t, new Vector3(0f, 0.14f, 0f), new Vector3(0.28f, 0.26f, 0.28f), shell.pot);
            Deco(PrimitiveType.Cylinder, "Stem", t, new Vector3(0f, 0.4f, 0f), new Vector3(0.04f, 0.14f, 0.04f), shell.wood);
            Deco(PrimitiveType.Sphere, "Leaf", t, new Vector3(0f, 0.62f, 0f), new Vector3(0.36f, 0.28f, 0.32f), shell.leaf);
            Deco(PrimitiveType.Sphere, "LeafB", t, new Vector3(0.12f, 0.5f, 0.06f), new Vector3(0.22f, 0.18f, 0.2f), shell.leaf);
        }

        static Material ShojiPaper()
        {
            var mat = LitMat("DojoShoji", new Color(0.97f, 0.94f, 0.86f, 0.55f), 0.05f, 0f, false, Color.black);
            MakeTransparent(mat, false);
            return mat;
        }

        static Material NeonPane(string name, Color color)
        {
            return UnlitMat(name, color, true);
        }
    }
}
