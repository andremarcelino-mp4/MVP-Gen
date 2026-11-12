using System.Collections.Generic;
using GenJutsu.HouseState;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Reference ZCEA interiors (Zen / Cyber / Excêntrico / Acolhedor). Primitives, Quest-cheap materials.
    /// </summary>
    public static class DojoHomeKit
    {
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        public static void EnsureAll()
        {
            var looks = Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include);
            for (var i = 0; i < looks.Length; i++)
                Ensure(looks[i]);
            for (var i = 0; i < looks.Length; i++)
                HideLooseToys(looks[i]);
        }

        public static void Ensure(ProfileLook look)
        {
            if (look == null)
                return;
            var mark = look.GetComponentInChildren<ZceaFurnished>(true);
            if (mark != null && mark.version >= ZceaFurnished.Current)
                return;

            StripStale(look);
            Build(look);
        }

        static void StripStale(ProfileLook look)
        {
            var rig = look.GetComponent<HouseDeviceRig>();
            if (rig != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(rig);
                else
                    Object.DestroyImmediate(rig);
            }

            var home = look.transform.Find("Pieces/Home") ?? look.transform.Find("Home");
            if (home != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(home.gameObject);
                else
                    Object.DestroyImmediate(home.gameObject);
            }

            var hide = new[] { "LowTable", "Frame", "Platform", "Lanterns", "Accents", "Toys" };
            for (var i = 0; i < hide.Length; i++)
            {
                var t = look.transform.Find("Pieces/" + hide[i]) ?? look.transform.Find(hide[i]);
                if (t != null)
                    t.gameObject.SetActive(false);
            }

            HideLooseToys(look);
        }

        public static void HideLooseToys(ProfileLook look)
        {
            if (look == null)
                return;
            var clutter = new[] { "GamePad", "Ball", "Book", "CushionLoose", "TvRemote", "Toys" };
            var all = look.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                var n = all[i].name;
                for (var c = 0; c < clutter.Length; c++)
                {
                    if (n == clutter[c])
                        all[i].gameObject.SetActive(false);
                }
            }
        }

        static void Build(ProfileLook look)
        {
            var root = look.transform.Find("Pieces") ?? look.transform;
            var home = new GameObject("Home");
            home.transform.SetParent(root, false);
            var marker = home.AddComponent<ZceaFurnished>();
            marker.version = ZceaFurnished.Current;
            var style = look.profile;
            ClosedRoom(home.transform, style);
            switch (style)
            {
                case ProfileId.Cyber:
                    Cyber(home.transform);
                    break;
                case ProfileId.Excentrico:
                    Excentrico(home.transform);
                    break;
                case ProfileId.Acolhedor:
                    Acolhedor(home.transform);
                    break;
                default:
                    Zen(home.transform);
                    break;
            }

            WireDevices(look, home.transform, style);
            TvSet.Ensure(home.transform);
            AirConUnit.Ensure(home.transform);
        }

        static void ClosedRoom(Transform home, ProfileId style)
        {
            var room = Group(home, "Room", Vector3.zero, 0f);
            Color wallCol;
            Color ceilCol;
            Color beamCol;
            Texture floorTex;
            Texture wallTex;
            switch (style)
            {
                case ProfileId.Cyber:
                    wallCol = new Color(0.08f, 0.07f, 0.14f);
                    ceilCol = new Color(0.05f, 0.05f, 0.1f);
                    beamCol = new Color(0.12f, 0.08f, 0.16f);
                    floorTex = Tex("woodFloor");
                    wallTex = Tex("plaster");
                    break;
                case ProfileId.Excentrico:
                    wallCol = new Color(0.46f, 0.24f, 0.58f);
                    ceilCol = new Color(0.32f, 0.18f, 0.12f);
                    beamCol = new Color(0.22f, 0.12f, 0.07f);
                    floorTex = Tex("woodFloor");
                    wallTex = Tex("plaster");
                    break;
                case ProfileId.Acolhedor:
                    wallCol = new Color(0.86f, 0.76f, 0.62f);
                    ceilCol = new Color(0.78f, 0.68f, 0.52f);
                    beamCol = new Color(0.38f, 0.24f, 0.14f);
                    floorTex = Tex("woodFloor");
                    wallTex = Tex("plaster");
                    break;
                default:
                    wallCol = new Color(0.9f, 0.84f, 0.7f);
                    ceilCol = new Color(0.93f, 0.88f, 0.76f);
                    beamCol = new Color(0.4f, 0.26f, 0.14f);
                    floorTex = Tex("woodFloor");
                    wallTex = Tex("paper");
                    break;
            }

            var floorTint = style == ProfileId.Cyber ? new Color(0.12f, 0.12f, 0.16f)
                : style == ProfileId.Excentrico ? new Color(0.72f, 0.62f, 0.42f)
                : new Color(0.55f, 0.4f, 0.24f);
            var floor = style == ProfileId.Zen
                ? TexMat("RoomTatami", new Color(0.78f, 0.74f, 0.52f), 0.08f, Tex("woodFloor"))
                : TexMat("RoomFloor_" + style, floorTint, 0.18f, floorTex);
            var wall = TexMat("RoomWall_" + style, wallCol, 0.12f, wallTex);
            var ceil = TexMat("RoomCeil_" + style, ceilCol, 0.1f, Tex("plaster"));
            var beam = TexMat("RoomBeam_" + style, beamCol, 0.22f, Tex("wood"));
            var paper = TexMat("RoomShoji", new Color(0.96f, 0.93f, 0.84f), 0.06f, Tex("paper"));

            const float half = 2.35f;
            const float h = 2.52f;
            const float t = 0.07f;
            Cube("Floor", room, new Vector3(0f, 0.02f, 0f), new Vector3(half * 2f + 0.2f, 0.08f, half * 2f + 0.2f), floor);
            Cube("Ceiling", room, new Vector3(0f, h + 0.04f, 0f), new Vector3(half * 2f + 0.2f, 0.1f, half * 2f + 0.2f), ceil);
            Cube("WallN", room, new Vector3(0f, h * 0.5f, half), new Vector3(half * 2f + 0.12f, h + 0.08f, t), wall);
            Cube("WallW", room, new Vector3(-half, h * 0.5f, 0f), new Vector3(t, h + 0.08f, half * 2f + 0.12f), wall);
            Cube("WallE", room, new Vector3(half, h * 0.5f, 0f), new Vector3(t, h + 0.08f, half * 2f + 0.12f), wall);
            Cube("WallS", room, new Vector3(0f, h * 0.5f, -half), new Vector3(half * 2f + 0.12f, h + 0.08f, t), wall);
            Cube("BeamX", room, new Vector3(0f, h - 0.06f, 0f), new Vector3(half * 2f, 0.1f, 0.14f), beam);
            Cube("BeamZ", room, new Vector3(0f, h - 0.08f, 0f), new Vector3(0.14f, 0.1f, half * 2f), beam);
            Cube("PostNW", room, new Vector3(-half + 0.05f, h * 0.5f, half - 0.05f), new Vector3(0.14f, h, 0.14f), beam);
            Cube("PostNE", room, new Vector3(half - 0.05f, h * 0.5f, half - 0.05f), new Vector3(0.14f, h, 0.14f), beam);
            Cube("PostSW", room, new Vector3(-half + 0.05f, h * 0.5f, -half + 0.05f), new Vector3(0.14f, h, 0.14f), beam);
            Cube("PostSE", room, new Vector3(half - 0.05f, h * 0.5f, -half + 0.05f), new Vector3(0.14f, h, 0.14f), beam);

            if (style == ProfileId.Zen || style == ProfileId.Acolhedor || style == ProfileId.Excentrico)
            {
                Shoji(room, new Vector3(-half + 0.05f, 1.15f, 0f), 90f, 3.2f, 1.85f, paper, beam);
                Shoji(room, new Vector3(half - 0.05f, 1.15f, 0.35f), -90f, 2.4f, 1.85f, paper, beam);
            }
            else
            {
                Cube("SillL", room, new Vector3(-1.2f, 1.25f, -half + 0.03f), new Vector3(1.0f, 0.85f, 0.02f), paper);
                Cube("SillR", room, new Vector3(1.2f, 1.25f, -half + 0.03f), new Vector3(1.0f, 0.85f, 0.02f), paper);
            }

            if (style == ProfileId.Zen)
            {
                var edge = Mat("TatamiEdge", new Color(0.22f, 0.28f, 0.14f), 0.1f);
                Cube("TatamiBorder", room, new Vector3(0f, 0.028f, 0f), new Vector3(4.55f, 0.012f, 4.55f), edge);
                var tatami = Mat("TatamiFace", new Color(0.76f, 0.71f, 0.5f), 0.06f);
                Cube("TatamiNW", room, new Vector3(-1.12f, 0.09f, 1.12f), new Vector3(2.18f, 0.03f, 2.18f), tatami);
                Cube("TatamiNE", room, new Vector3(1.12f, 0.09f, 1.12f), new Vector3(2.18f, 0.03f, 2.18f), tatami);
                Cube("TatamiSW", room, new Vector3(-1.12f, 0.09f, -1.12f), new Vector3(2.18f, 0.03f, 2.18f), tatami);
                Cube("TatamiSE", room, new Vector3(1.12f, 0.09f, -1.12f), new Vector3(2.18f, 0.03f, 2.18f), tatami);
            }

            if (style == ProfileId.Cyber)
            {
                var cyan = Glow("RoomNeon", new Color(0.15f, 0.85f, 1f), 2f);
                Cube("NeonN", room, new Vector3(0f, 0.04f, half - 0.05f), new Vector3(4.6f, 0.02f, 0.03f), cyan);
                Cube("NeonS", room, new Vector3(0f, 0.04f, -half + 0.05f), new Vector3(4.6f, 0.02f, 0.03f), cyan);
                Cube("NeonW", room, new Vector3(-half + 0.05f, 0.04f, 0f), new Vector3(0.03f, 0.02f, 4.6f), cyan);
                Cube("NeonE", room, new Vector3(half - 0.05f, 0.04f, 0f), new Vector3(0.03f, 0.02f, 4.6f), cyan);
            }
        }

        static void Shoji(Transform room, Vector3 pos, float yaw, float width, float height, Material paper, Material wood)
        {
            var root = Group(room, "Shoji", pos, yaw);
            Cube("Paper", root, Vector3.zero, new Vector3(width, height, 0.02f), paper);
            Cube("FrameT", root, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, 0.04f, 0.04f), wood);
            Cube("FrameB", root, new Vector3(0f, -height * 0.5f, 0f), new Vector3(width, 0.04f, 0.04f), wood);
            Cube("FrameL", root, new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.04f, height, 0.04f), wood);
            Cube("FrameR", root, new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.04f, height, 0.04f), wood);
            for (var i = 1; i < 4; i++)
                Cube("V" + i, root, new Vector3(-width * 0.5f + i * (width / 4f), 0f, 0.005f), new Vector3(0.025f, height, 0.02f), wood);
            for (var j = 1; j < 3; j++)
                Cube("H" + j, root, new Vector3(0f, -height * 0.5f + j * (height / 3f), 0.005f), new Vector3(width, 0.025f, 0.02f), wood);
        }

        static void Zen(Transform home)
        {
            TvCabinet(home, new Vector3(-0.15f, 0f, -2.05f), 0f, new Color(0.28f, 0.16f, 0.08f), false);
            Chabudai(home, Vector3.zero, new Color(0.32f, 0.18f, 0.09f));
            var zab = new Color(0.28f, 0.38f, 0.3f);
            Zabuton(home, "ZenZabN", new Vector3(0f, 0.125f, 0.7f), zab);
            Zabuton(home, "ZenZabS", new Vector3(0f, 0.125f, -0.7f), zab);
            Zabuton(home, "ZenZabE", new Vector3(0.7f, 0.125f, 0f), zab);
            Zabuton(home, "ZenZabW", new Vector3(-0.7f, 0.125f, 0f), zab);
            Tokonoma(home);
            Byobu(home, new Vector3(0.95f, 0f, 1.55f));
            LanternHang(home, new Vector3(-1.75f, 0f, -1.45f), new Color(1f, 0.9f, 0.65f));
            LanternHang(home, new Vector3(1.55f, 0f, -1.45f), new Color(1f, 0.9f, 0.65f));
            LanternHang(home, new Vector3(-1.75f, 0f, 1.45f), new Color(1f, 0.9f, 0.65f));
            LanternHang(home, new Vector3(1.55f, 0f, 1.45f), new Color(1f, 0.9f, 0.65f));
        }

        static void Tokonoma(Transform home)
        {
            var t = Group(home, "Tokonoma", new Vector3(0.35f, 0f, 1.92f), 0f);
            Cube("Deck", t, new Vector3(0f, 0.1f, 0f), new Vector3(1.85f, 0.16f, 0.7f), Wood(0.3f, 0.16f, 0.08f));
            Scroll(t, new Vector3(0.05f, 1.18f, 0.3f), "Zen", new Color(0.1f, 0.1f, 0.08f));
            Kanji(t, new Vector3(0.05f, 1.22f, 0.28f), "禅");
            Bonsai(t, new Vector3(-0.48f, 0.18f, 0.02f));
            Ikebana(t, new Vector3(0.52f, 0.18f, 0.06f));
            SandGarden(t, new Vector3(0.02f, 0.18f, -0.08f));
        }

        static void Cyber(Transform home)
        {
            var tv = Group(home, "TvStand", new Vector3(-1.85f, 0f, 0.05f), 90f);
            Cube("Cabinet", tv, new Vector3(0f, 0.38f, 0f), new Vector3(1.45f, 0.08f, 0.55f), Mat("CyberDesk", new Color(0.08f, 0.08f, 0.1f), 0.22f));
            Cube("Legs", tv, new Vector3(0f, 0.18f, 0f), new Vector3(1.35f, 0.36f, 0.12f), Mat("CyberDesk", new Color(0.08f, 0.08f, 0.1f), 0.22f));
            Cube("Screen", tv, new Vector3(-0.15f, 0.78f, 0.02f), new Vector3(0.72f, 0.42f, 0.04f), Glow("HomeTv_Cyber", new Color(0.2f, 0.45f, 1f), 3.2f));
            Cube("Bezel", tv, new Vector3(-0.15f, 0.78f, -0.02f), new Vector3(0.8f, 0.5f, 0.03f), Mat("TvBezel", new Color(0.05f, 0.05f, 0.07f), 0.2f));
            Cube("Keyboard", tv, new Vector3(-0.1f, 0.44f, 0.18f), new Vector3(0.42f, 0.03f, 0.14f), Mat("Keys", new Color(0.12f, 0.12f, 0.14f), 0.2f));
            Cube("ScreenB", tv, new Vector3(0.42f, 0.72f, 0.02f), new Vector3(0.38f, 0.28f, 0.03f), Glow("HomeTv_CyberB", new Color(0.85f, 0.2f, 1f), 2.4f));
            Strip(tv, new Vector3(0f, 0.42f, 0.26f), new Vector3(1.4f, 0.02f, 0.02f), Glow("CyberEdge", new Color(1f, 0.1f, 0.45f), 2.4f));
            Maneki(tv, new Vector3(0.48f, 0.44f, 0.05f));
            Cube("Tower", home, new Vector3(-1.55f, 0.55f, -0.95f), new Vector3(0.28f, 1.05f, 0.42f), Mat("PcCase", new Color(0.05f, 0.05f, 0.07f), 0.25f));
            Cube("TowerLed", home, new Vector3(-1.41f, 0.55f, -0.95f), new Vector3(0.03f, 0.72f, 0.08f), Glow("PcLed", new Color(0.15f, 0.9f, 1f), 3f));
            Cube("Pad", home, new Vector3(-0.85f, 0.05f, 0.55f), new Vector3(0.16f, 0.04f, 0.1f), Mat("Pad", new Color(0.75f, 0.75f, 0.78f), 0.35f));
            var chair = Group(home, "Chair", new Vector3(-1.05f, 0f, 0.15f), 90f);
            Cube("Seat", chair, new Vector3(0f, 0.28f, 0f), new Vector3(0.5f, 0.08f, 0.5f), Mat("Gamer", new Color(0.04f, 0.05f, 0.07f), 0.18f));
            Cube("Back", chair, new Vector3(0f, 0.58f, 0.2f), new Vector3(0.5f, 0.55f, 0.08f), Mat("Gamer", new Color(0.04f, 0.05f, 0.07f), 0.18f));
            Cube("Accent", chair, new Vector3(0f, 0.58f, 0.24f), new Vector3(0.08f, 0.45f, 0.02f), Glow("GamerNeon", new Color(0.15f, 0.85f, 1f), 2f));
            var bag = Group(home, "BeanBag", new Vector3(0.55f, 0f, 1.05f), 0f);
            Prim(PrimitiveType.Sphere, "Bag", bag, new Vector3(0f, 0.22f, 0f), new Vector3(0.78f, 0.38f, 0.66f), Mat("Bag", new Color(0.07f, 0.09f, 0.12f), 0.12f));
            NeonSign(home, new Vector3(0.85f, 1.72f, 2.22f), new Color(1f, 0.12f, 0.72f));
            Torii(home, new Vector3(-0.75f, 1.55f, 2.22f));
            Katana(home, new Vector3(1.55f, 1.15f, 2.18f));
            LanternHang(home, new Vector3(-1.55f, 0f, 1.55f), new Color(1f, 0.2f, 0.38f));
            LanternHang(home, new Vector3(1.55f, 0f, 1.55f), new Color(1f, 0.2f, 0.38f));
            LanternHang(home, new Vector3(1.55f, 0f, -1.25f), new Color(1f, 0.2f, 0.38f));
        }

        static void Excentrico(Transform home)
        {
            TvCabinet(home, new Vector3(-1.35f, 0f, -2.05f), 0f, new Color(0.18f, 0.16f, 0.14f), true);
            Arcade(home, new Vector3(0.15f, 0f, -1.95f));
            Koinobori(home);
            Oni(home, new Vector3(-0.05f, 1.78f, 2.22f));
            Oni(home, new Vector3(0.55f, 1.52f, 2.22f));
            Daruma(home, new Vector3(-0.95f, 0.02f, 0.75f));
            Daruma(home, new Vector3(1.15f, 0.02f, 0.85f));
            Origami(home);
            Parasol(home, new Vector3(0.75f, 0f, 0.15f));
            Dartboard(home, new Vector3(2.22f, 1.42f, 0.35f));
            Lava(home, new Vector3(-1.95f, 0.02f, -1.05f), new Color(1f, 0.35f, 0.1f));
            Lava(home, new Vector3(-1.95f, 0.02f, 0.35f), new Color(0.7f, 0.2f, 1f));
            Stool(home, "StoolA", new Vector3(0.15f, 0f, 1.05f), new Color(0.85f, 0.12f, 0.18f));
            Stool(home, "StoolB", new Vector3(-0.55f, 0f, 1.15f), new Color(0.42f, 0.12f, 0.72f));
            Stool(home, "StoolC", new Vector3(0.95f, 0f, -0.85f), new Color(0.95f, 0.55f, 0.12f));
            Prim(PrimitiveType.Sphere, "BallToy", home, new Vector3(0.55f, 0.08f, 0.85f), Vector3.one * 0.14f, Mat("YelBall", new Color(0.95f, 0.82f, 0.12f), 0.35f));
            Cube("Samurai", home, new Vector3(-1.85f, 0.28f, 0.95f), new Vector3(0.22f, 0.48f, 0.16f), Mat("Yoroi", new Color(0.12f, 0.18f, 0.42f), 0.25f));
            LanternHang(home, new Vector3(-1.15f, 0f, 1.15f), new Color(1f, 0.82f, 0.35f));
            LanternHang(home, new Vector3(1.35f, 0f, 1.15f), new Color(1f, 0.82f, 0.35f));
            LanternHang(home, new Vector3(-1.85f, 0f, -0.35f), new Color(1f, 0.82f, 0.35f));
            LanternHang(home, new Vector3(1.75f, 0f, -0.15f), new Color(1f, 0.82f, 0.35f));
            Cube("Poster", home, new Vector3(-0.85f, 1.35f, 2.28f), new Vector3(0.42f, 0.55f, 0.02f), Mat("Poster", new Color(0.55f, 0.12f, 0.18f), 0.2f));
        }

        static void Acolhedor(Transform home)
        {
            TvCabinet(home, new Vector3(-1.85f, 0f, -0.85f), 90f, new Color(0.28f, 0.16f, 0.08f), false);
            var kota = Group(home, "Kotatsu", new Vector3(0.15f, 0f, -0.05f), 0f);
            kota.name = "Chabudai";
            Cube("Top", kota, new Vector3(0f, 0.34f, 0f), new Vector3(1.12f, 0.05f, 1.12f), Wood(0.32f, 0.18f, 0.1f));
            Legs(kota, 0.46f, 0.16f, Wood(0.32f, 0.18f, 0.1f));
            Cube("Cloth", kota, new Vector3(0f, 0.22f, 0f), new Vector3(1.32f, 0.2f, 1.32f), Mat("KotatsuCloth", new Color(0.78f, 0.22f, 0.18f), 0.16f));
            Cube("Check", kota, new Vector3(0f, 0.325f, 0f), new Vector3(1.05f, 0.02f, 1.05f), Mat("Check", new Color(0.72f, 0.42f, 0.32f), 0.14f));
            Prim(PrimitiveType.Sphere, "OrangeA", kota, new Vector3(-0.1f, 0.42f, 0.04f), Vector3.one * 0.075f, Mat("Orange", new Color(0.95f, 0.45f, 0.12f), 0.35f));
            Prim(PrimitiveType.Sphere, "OrangeB", kota, new Vector3(0.06f, 0.42f, -0.04f), Vector3.one * 0.075f, Mat("Orange", new Color(0.95f, 0.45f, 0.12f), 0.35f));
            Prim(PrimitiveType.Sphere, "OrangeC", kota, new Vector3(0.0f, 0.42f, 0.1f), Vector3.one * 0.075f, Mat("Orange", new Color(0.95f, 0.45f, 0.12f), 0.35f));
            Zabuton(home, "AcoZabN", new Vector3(0.15f, 0.05f, 0.78f), new Color(0.55f, 0.16f, 0.14f));
            Zabuton(home, "AcoZabS", new Vector3(0.15f, 0.05f, -0.82f), new Color(0.18f, 0.32f, 0.62f));
            Zabuton(home, "AcoZabE", new Vector3(0.92f, 0.05f, -0.05f), new Color(0.82f, 0.42f, 0.2f));
            Zabuton(home, "AcoZabW", new Vector3(-0.62f, 0.05f, -0.05f), new Color(0.55f, 0.16f, 0.14f));
            Cat(home, new Vector3(-0.05f, 0.14f, 0.78f));
            Fireplace(home, new Vector3(0.55f, 0f, 2.05f));
            Bookshelf(home, new Vector3(-0.85f, 0f, 2.05f));
            Noren(home, new Vector3(-1.65f, 1.5f, 2.22f));
            Scroll(home, new Vector3(-0.12f, 1.52f, 2.22f), "Aco", new Color(0.45f, 0.32f, 0.2f));
            Plant(home, new Vector3(1.85f, 0f, 1.75f));
            Plant(home, new Vector3(-1.95f, 0f, 1.55f));
            Sofa(home, new Vector3(-1.85f, 0f, 0.55f), 90f, new Color(0.62f, 0.6f, 0.56f));
            Cube("Shelf", home, new Vector3(2.05f, 0.85f, 0.55f), new Vector3(0.28f, 1.55f, 0.55f), Mat("WhiteShelf", new Color(0.92f, 0.9f, 0.86f), 0.18f));
            Prim(PrimitiveType.Sphere, "BowlA", home, new Vector3(2.05f, 0.28f, 0.55f), Vector3.one * 0.12f, Mat("Bowl", new Color(0.85f, 0.22f, 0.18f), 0.35f));
            Prim(PrimitiveType.Sphere, "RugBall", home, new Vector3(1.15f, 0.08f, 0.85f), Vector3.one * 0.16f, Mat("Foot", Color.white, 0.25f));
            LanternHang(home, new Vector3(-0.45f, 0f, 1.15f), new Color(1f, 0.82f, 0.52f));
            LanternHang(home, new Vector3(1.15f, 0f, 1.15f), new Color(1f, 0.82f, 0.52f));
            LanternHang(home, new Vector3(1.75f, 0f, -0.85f), new Color(1f, 0.82f, 0.52f));
        }

        static void Chabudai(Transform home, Vector3 pos, Color woodCol)
        {
            var table = Group(home, "Chabudai", pos, 0f);
            var wood = Wood(woodCol.r, woodCol.g, woodCol.b);
            Cube("Top", table, new Vector3(0f, 0.32f, 0f), new Vector3(0.98f, 0.05f, 0.98f), wood);
            Legs(table, 0.4f, 0.15f, wood);
            TeaSet(table);
        }

        static void Bed(Transform home, ProfileId style)
        {
            var t = Group(home, "Bed", new Vector3(-1.85f, 0f, 1.75f), 8f);
            var bed = ZceaModels.Place("bedSingle", t, Vector3.zero, 0f, 1f);
            var linen = style == ProfileId.Cyber ? new Color(0.16f, 0.18f, 0.22f)
                : style == ProfileId.Excentrico ? new Color(0.55f, 0.85f, 0.9f)
                : style == ProfileId.Acolhedor ? new Color(0.93f, 0.86f, 0.74f)
                : new Color(0.86f, 0.82f, 0.74f);
            ZceaModels.Tint(bed, linen);
        }

        static void Kitchen(Transform home, ProfileId style)
        {
            var k = Group(home, "Kitchen", new Vector3(1.95f, 0f, 1.85f), 0f);
            ZceaModels.Place("kitchenCabinet", k, Vector3.zero, 0f, 1f);
            ZceaModels.Place("kitchenCoffeeMachine", k, new Vector3(-0.15f, 0.55f, 0f), 0f, 1f);
            var kettle = Glow("HomeKettle_" + style, style == ProfileId.Excentrico ? new Color(0.95f, 0.2f, 0.55f) : new Color(0.82f, 0.82f, 0.84f), 0.15f);
            Prim(PrimitiveType.Cylinder, "Kettle", k, new Vector3(-0.18f, 0.72f, 0f), new Vector3(0.12f, 0.1f, 0.12f), kettle);
            Prim(PrimitiveType.Cylinder, "MugA", k, new Vector3(0.12f, 0.64f, 0.04f), new Vector3(0.06f, 0.05f, 0.06f), Mat("HomeMug", new Color(0.86f, 0.82f, 0.76f), 0.35f));
            Prim(PrimitiveType.Cylinder, "MugB", k, new Vector3(0.22f, 0.64f, 0.04f), new Vector3(0.06f, 0.05f, 0.06f), Mat("HomeMugB", new Color(0.75f, 0.7f, 0.62f), 0.35f));
        }

        static void TvCabinet(Transform home, Vector3 pos, float yaw, Color wood, bool retro)
        {
            var tv = Group(home, "TvStand", pos, yaw);
            Cube("Cabinet", tv, new Vector3(0f, 0.16f, 0f), new Vector3(1.15f, 0.32f, 0.36f), Mat("TvWood", wood, 0.22f));
            Cube("Bezel", tv, new Vector3(0f, 0.62f, 0.12f), new Vector3(1.06f, 0.58f, 0.03f), Mat("TvBezel", new Color(0.08f, 0.08f, 0.08f), 0.25f));
            Cube("Screen", tv, new Vector3(0f, 0.62f, 0.145f), new Vector3(0.94f, 0.48f, 0.02f), Glow(retro ? "HomeTvScreenRetro" : "HomeTvScreenDark", retro ? new Color(0.2f, 0.55f, 0.4f) : new Color(0.07f, 0.07f, 0.09f), retro ? 0.7f : 0.2f));
            Cube("Console", tv, new Vector3(0.28f, 0.36f, 0.14f), new Vector3(0.28f, 0.06f, 0.16f), Mat("Console", new Color(0.82f, 0.82f, 0.84f), 0.35f));
            Cube("Deck", tv, new Vector3(-0.28f, 0.35f, 0.14f), new Vector3(0.22f, 0.05f, 0.14f), Mat("Deck", new Color(0.12f, 0.12f, 0.14f), 0.25f));
        }

        static void Sofa(Transform home, Vector3 pos, float yaw, Color fabric)
        {
            var sofa = Group(home, "Sofa", pos, yaw);
            var model = ZceaModels.Place("loungeSofa", sofa, Vector3.zero, 0f, 1f);
            ZceaModels.Tint(model, fabric);
        }

        static void SofaBench(Transform home, Vector3 pos, float yaw, Color fabric)
        {
            var sofa = Group(home, "Sofa", pos, yaw);
            Cube("Base", sofa, new Vector3(0f, 0.18f, 0f), new Vector3(1.1f, 0.2f, 0.42f), Mat("Bench", fabric, 0.2f));
            Cube("Back", sofa, new Vector3(0f, 0.38f, 0.16f), new Vector3(1.1f, 0.28f, 0.08f), Mat("Bench", fabric, 0.2f));
        }

        static void TeaSet(Transform table)
        {
            Cube("Tray", table, new Vector3(0f, 0.355f, 0.05f), new Vector3(0.42f, 0.02f, 0.28f), Wood(0.22f, 0.12f, 0.07f));
            Prim(PrimitiveType.Cylinder, "Pot", table, new Vector3(0.04f, 0.4f, 0.04f), new Vector3(0.11f, 0.07f, 0.11f), Mat("TeaPot", new Color(0.92f, 0.9f, 0.86f), 0.4f));
            Prim(PrimitiveType.Cylinder, "CupA", table, new Vector3(-0.16f, 0.375f, 0.1f), new Vector3(0.055f, 0.03f, 0.055f), Mat("TeaCup", new Color(0.95f, 0.95f, 0.93f), 0.35f));
            Prim(PrimitiveType.Cylinder, "CupB", table, new Vector3(-0.08f, 0.375f, -0.08f), new Vector3(0.055f, 0.03f, 0.055f), Mat("TeaCup", new Color(0.95f, 0.95f, 0.93f), 0.35f));
            Cube("Remote", table, new Vector3(0.22f, 0.36f, -0.18f), new Vector3(0.14f, 0.02f, 0.05f), Mat("Hold", new Color(0.12f, 0.12f, 0.12f), 0.2f));
        }

        static void Zabuton(Transform home, string name, Vector3 pos, Color color)
        {
            Cube(name, home, pos, new Vector3(0.52f, 0.07f, 0.52f), Mat(name, color, 0.16f));
        }

        static void Bonsai(Transform home, Vector3 pos)
        {
            var t = Group(home, "Bonsai", pos, 0f);
            Prim(PrimitiveType.Cylinder, "Pot", t, new Vector3(0f, 0.08f, 0f), new Vector3(0.32f, 0.08f, 0.32f), Mat("Pot", new Color(0.22f, 0.16f, 0.12f), 0.3f));
            Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0.02f, 0.28f, 0f), new Vector3(0.05f, 0.16f, 0.05f), Wood(0.32f, 0.2f, 0.1f));
            Prim(PrimitiveType.Sphere, "Crown", t, new Vector3(-0.04f, 0.48f, 0f), new Vector3(0.38f, 0.16f, 0.28f), Mat("Leaf", new Color(0.22f, 0.42f, 0.18f), 0.2f));
            Prim(PrimitiveType.Sphere, "CrownB", t, new Vector3(0.14f, 0.4f, 0.04f), new Vector3(0.22f, 0.12f, 0.18f), Mat("Leaf", new Color(0.22f, 0.42f, 0.18f), 0.2f));
        }

        static void Ikebana(Transform home, Vector3 pos)
        {
            var t = Group(home, "Ikebana", pos, 0f);
            Prim(PrimitiveType.Cylinder, "Vase", t, new Vector3(0f, 0.22f, 0f), new Vector3(0.1f, 0.22f, 0.1f), Mat("Vase", new Color(0.18f, 0.16f, 0.32f), 0.45f));
            Prim(PrimitiveType.Sphere, "Bloom", t, new Vector3(0.02f, 0.52f, 0f), new Vector3(0.18f, 0.12f, 0.12f), Mat("Sakura", new Color(0.95f, 0.55f, 0.62f), 0.2f));
            Cube("Stem", t, new Vector3(0f, 0.4f, 0f), new Vector3(0.02f, 0.2f, 0.02f), Mat("Stem", new Color(0.2f, 0.35f, 0.18f), 0.2f));
        }

        static void Scroll(Transform home, Vector3 pos, string id, Color ink)
        {
            var t = Group(home, "KanjiScroll", pos, 0f);
            Cube("Paper", t, Vector3.zero, new Vector3(0.32f, 1.05f, 0.02f), Mat("ScrollPaper", new Color(0.93f, 0.9f, 0.78f), 0.08f));
            Cube("RodT", t, new Vector3(0f, 0.55f, 0f), new Vector3(0.38f, 0.04f, 0.04f), Wood(0.2f, 0.12f, 0.08f));
            Cube("RodB", t, new Vector3(0f, -0.55f, 0f), new Vector3(0.38f, 0.04f, 0.04f), Wood(0.2f, 0.12f, 0.08f));
            Stroke(t, ink, new Vector3(0f, 0.12f, -0.015f), new Vector3(0.04f, 0.42f, 0.01f));
            Stroke(t, ink, new Vector3(0f, 0.22f, -0.015f), new Vector3(0.18f, 0.04f, 0.01f));
            Stroke(t, ink, new Vector3(0f, -0.08f, -0.015f), new Vector3(0.16f, 0.04f, 0.01f));
        }

        static void Byobu(Transform home, Vector3 pos)
        {
            var t = Group(home, "Byobu", pos, -12f);
            var paper = Mat("ByobuPaper", new Color(0.93f, 0.78f, 0.22f), 0.12f);
            var wood = Wood(0.28f, 0.16f, 0.08f);
            for (var i = 0; i < 3; i++)
            {
                var p = Group(t, "Panel" + i, new Vector3(i * 0.42f, 0.72f, i * 0.03f), i * 6f);
                Cube("Paper", p, Vector3.zero, new Vector3(0.4f, 1.4f, 0.03f), paper);
                Cube("Frame", p, Vector3.zero, new Vector3(0.42f, 1.44f, 0.025f), wood);
            }
        }

        static void SandGarden(Transform home, Vector3 pos)
        {
            var t = Group(home, "SandGarden", pos + new Vector3(0f, 0f, -0.55f), 0f);
            Cube("Tray", t, new Vector3(0f, 0.04f, 0f), new Vector3(1.05f, 0.06f, 0.7f), Mat("Sand", new Color(0.86f, 0.82f, 0.7f), 0.08f));
            Prim(PrimitiveType.Sphere, "RockA", t, new Vector3(-0.28f, 0.12f, 0.08f), new Vector3(0.16f, 0.1f, 0.14f), Mat("Stone", new Color(0.32f, 0.32f, 0.3f), 0.3f));
            Prim(PrimitiveType.Sphere, "RockB", t, new Vector3(0.22f, 0.1f, -0.12f), new Vector3(0.12f, 0.08f, 0.1f), Mat("StoneB", new Color(0.4f, 0.4f, 0.38f), 0.3f));
        }

        static void LanternHang(Transform home, Vector3 xz, Color paper)
        {
            var hang = Group(home, "Lantern", xz, 0f);
            Prim(PrimitiveType.Cylinder, "Cord", hang, new Vector3(0f, 2.22f, 0f), new Vector3(0.012f, 0.18f, 0.012f), Wood(0.16f, 0.1f, 0.07f));
            Prim(PrimitiveType.Cylinder, "Cap", hang, new Vector3(0f, 2.02f, 0f), new Vector3(0.14f, 0.025f, 0.14f), Wood(0.16f, 0.1f, 0.07f));
            Prim(PrimitiveType.Cylinder, "Shade", hang, new Vector3(0f, 1.88f, 0f), new Vector3(0.11f, 0.16f, 0.11f), Glow("Lantern" + xz.x + xz.z, paper, 1.6f));
            Prim(PrimitiveType.Cylinder, "CapB", hang, new Vector3(0f, 1.62f, 0f), new Vector3(0.14f, 0.02f, 0.14f), Wood(0.16f, 0.1f, 0.07f));
            Point("Glow", hang, new Vector3(0f, 1.82f, 0f), paper, 0.85f, 3.4f);
        }

        static void Chair(Transform home, Vector3 pos)
        {
            var t = Group(home, "Chair", pos, 195f);
            var model = ZceaModels.Place("loungeChairRelax", t, Vector3.zero, 0f, 1f);
            ZceaModels.Tint(model, new Color(0.08f, 0.1f, 0.14f));
        }

        static void NeonSign(Transform home, Vector3 pos, Color color)
        {
            var t = Group(home, "NeonKanji", pos, 0f);
            Cube("Board", t, Vector3.zero, new Vector3(0.72f, 0.42f, 0.04f), Glow("NeonBoard", color, 2.2f));
            Stroke(t, Color.white, new Vector3(-0.12f, 0.04f, -0.03f), new Vector3(0.06f, 0.22f, 0.02f));
            Stroke(t, Color.white, new Vector3(0.12f, 0.04f, -0.03f), new Vector3(0.22f, 0.05f, 0.02f));
        }

        static void Torii(Transform home, Vector3 pos)
        {
            var t = Group(home, "Torii", pos, 0f);
            var red = Glow("ToriiRed", new Color(0.95f, 0.12f, 0.18f), 1.6f);
            Cube("PostL", t, new Vector3(-0.28f, 0.05f, 0f), new Vector3(0.06f, 0.7f, 0.06f), red);
            Cube("PostR", t, new Vector3(0.28f, 0.05f, 0f), new Vector3(0.06f, 0.7f, 0.06f), red);
            Cube("Kasagi", t, new Vector3(0f, 0.42f, 0f), new Vector3(0.85f, 0.06f, 0.08f), red);
        }

        static void Katana(Transform home, Vector3 pos)
        {
            var t = Group(home, "Katana", pos, 0f);
            Cube("Stand", t, new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.35f, 0.08f), Wood(0.2f, 0.12f, 0.08f));
            Cube("Blade", t, new Vector3(0.02f, 0.12f, 0f), new Vector3(0.72f, 0.03f, 0.03f), Mat("Steel", new Color(0.75f, 0.78f, 0.82f), 0.7f, 0.8f));
            Cube("Saya", t, new Vector3(0.02f, -0.02f, 0f), new Vector3(0.7f, 0.04f, 0.04f), Mat("Saya", new Color(0.12f, 0.08f, 0.08f), 0.3f));
        }

        static void Maneki(Transform home, Vector3 pos)
        {
            var t = Group(home, "Maneki", pos, 0f);
            Prim(PrimitiveType.Sphere, "Body", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.2f, 0.16f), Glow("Neko", new Color(1f, 0.25f, 0.45f), 1.5f));
            Prim(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 0.28f, 0f), new Vector3(0.16f, 0.14f, 0.14f), Glow("Neko", new Color(1f, 0.25f, 0.45f), 1.5f));
            Cube("Arm", t, new Vector3(0.1f, 0.3f, 0f), new Vector3(0.04f, 0.16f, 0.04f), Glow("Neko", new Color(1f, 0.25f, 0.45f), 1.5f));
        }

        static void Arcade(Transform home, Vector3 pos)
        {
            var t = Group(home, "Arcade", pos, 0f);
            Cube("Body", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.55f, 1.35f, 0.48f), Mat("Cab", new Color(0.08f, 0.08f, 0.1f), 0.2f));
            Cube("Art", t, new Vector3(0f, 0.55f, 0.25f), new Vector3(0.5f, 0.7f, 0.02f), Glow("CabArt", new Color(1f, 0.45f, 0.1f), 1.2f));
            Cube("Glass", t, new Vector3(0f, 1.15f, 0.08f), new Vector3(0.42f, 0.32f, 0.04f), Glow("HomeTv_Arcade", new Color(0.2f, 0.4f, 0.95f), 1.4f));
        }

        static void Koinobori(Transform home)
        {
            var t = Group(home, "Koinobori", new Vector3(-1.85f, 1.85f, 2.18f), 0f);
            Fish(t, new Vector3(0f, 0.35f, 0f), new Color(0.15f, 0.55f, 0.95f));
            Fish(t, new Vector3(0f, 0f, 0.15f), new Color(0.95f, 0.35f, 0.12f));
            Fish(t, new Vector3(0f, -0.35f, 0.05f), new Color(0.95f, 0.82f, 0.15f));
            Cube("Streamer", t, new Vector3(0f, 0.7f, -0.1f), new Vector3(0.7f, 0.08f, 0.04f), Mat("Stream", new Color(0.2f, 0.75f, 0.35f), 0.2f));
        }

        static void Fish(Transform parent, Vector3 pos, Color color)
        {
            var f = Group(parent, "Fish", pos, 0f);
            Prim(PrimitiveType.Sphere, "Body", f, Vector3.zero, new Vector3(0.55f, 0.16f, 0.12f), Mat("Koi" + color.r, color, 0.25f));
            Cube("Tail", f, new Vector3(-0.28f, 0f, 0f), new Vector3(0.12f, 0.18f, 0.03f), Mat("KoiT" + color.g, color * 0.85f, 0.25f));
        }

        static void Oni(Transform home, Vector3 pos)
        {
            var t = Group(home, "Oni", pos, 0f);
            Prim(PrimitiveType.Sphere, "Face", t, Vector3.zero, new Vector3(0.42f, 0.46f, 0.2f), Mat("OniRed", new Color(0.75f, 0.08f, 0.08f), 0.25f));
            Cube("HornL", t, new Vector3(-0.1f, 0.22f, 0f), new Vector3(0.05f, 0.16f, 0.05f), Mat("Horn", new Color(0.12f, 0.08f, 0.08f), 0.2f));
            Cube("HornR", t, new Vector3(0.1f, 0.22f, 0f), new Vector3(0.05f, 0.16f, 0.05f), Mat("Horn", new Color(0.12f, 0.08f, 0.08f), 0.2f));
        }

        static void Daruma(Transform home, Vector3 pos)
        {
            var t = Group(home, "Daruma", pos, 0f);
            Prim(PrimitiveType.Sphere, "Body", t, new Vector3(0f, 0.14f, 0f), new Vector3(0.28f, 0.3f, 0.28f), Glow("Daruma", new Color(0.85f, 0.12f, 0.1f), 0.6f));
            Prim(PrimitiveType.Sphere, "EyeL", t, new Vector3(-0.06f, 0.2f, -0.12f), Vector3.one * 0.05f, Mat("Eye", Color.white, 0.2f));
            Prim(PrimitiveType.Sphere, "EyeR", t, new Vector3(0.06f, 0.2f, -0.12f), Vector3.one * 0.05f, Mat("Eye", Color.white, 0.2f));
        }

        static void Origami(Transform home)
        {
            var t = Group(home, "Origami", new Vector3(0.15f, 2.15f, 0.4f), 0f);
            var colors = new[]
            {
                new Color(0.55f, 0.2f, 0.85f), new Color(0.95f, 0.2f, 0.25f), new Color(0.95f, 0.75f, 0.1f),
                new Color(0.95f, 0.55f, 0.15f), Color.white
            };
            for (var i = 0; i < colors.Length; i++)
                Cube("Crane" + i, t, new Vector3((i - 2) * 0.28f, 0f, 0f), new Vector3(0.14f, 0.03f, 0.1f), Mat("Crane" + i, colors[i], 0.25f));
        }

        static void Parasol(Transform home, Vector3 pos)
        {
            var t = Group(home, "Parasol", pos, -20f);
            Prim(PrimitiveType.Cylinder, "Pole", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.03f, 0.7f, 0.03f), Wood(0.2f, 0.12f, 0.08f));
            Prim(PrimitiveType.Cylinder, "Canopy", t, new Vector3(0f, 1.28f, 0f), new Vector3(0.52f, 0.04f, 0.52f), Mat("Aka", new Color(0.85f, 0.08f, 0.08f), 0.2f));
        }

        static void Dartboard(Transform home, Vector3 pos)
        {
            var t = Group(home, "Dartboard", pos, -90f);
            Prim(PrimitiveType.Cylinder, "Board", t, Vector3.zero, new Vector3(0.35f, 0.03f, 0.35f), Mat("Dart", new Color(0.12f, 0.12f, 0.12f), 0.25f));
            Prim(PrimitiveType.Cylinder, "Ring", t, new Vector3(0f, 0.02f, 0f), new Vector3(0.22f, 0.02f, 0.22f), Mat("DartR", new Color(0.75f, 0.12f, 0.12f), 0.25f));
        }

        static void Lava(Transform home, Vector3 pos, Color glow)
        {
            var t = Group(home, "LavaLamp", pos, 0f);
            Prim(PrimitiveType.Cylinder, "Glass", t, new Vector3(0f, 0.28f, 0f), new Vector3(0.1f, 0.22f, 0.1f), Glow("Lava" + glow.r, glow, 1.4f));
            Prim(PrimitiveType.Cylinder, "Base", t, new Vector3(0f, 0.06f, 0f), new Vector3(0.12f, 0.06f, 0.12f), Mat("LavaBase", new Color(0.08f, 0.08f, 0.1f), 0.3f));
        }

        static void Stool(Transform home, string name, Vector3 pos, Color color)
        {
            var t = Group(home, name, pos, 0f);
            var model = ZceaModels.Place("stoolBar", t, Vector3.zero, 0f, 1f);
            ZceaModels.Tint(model, color);
        }

        static void Cat(Transform home, Vector3 pos)
        {
            var t = Group(home, "Cat", pos, 40f);
            Prim(PrimitiveType.Sphere, "Body", t, new Vector3(0f, 0.06f, 0f), new Vector3(0.28f, 0.12f, 0.18f), Mat("CatFur", new Color(0.72f, 0.7f, 0.66f), 0.15f));
            Prim(PrimitiveType.Sphere, "Head", t, new Vector3(0.12f, 0.08f, 0.02f), new Vector3(0.12f, 0.1f, 0.1f), Mat("CatFur", new Color(0.72f, 0.7f, 0.66f), 0.15f));
            Cube("Ear", t, new Vector3(0.1f, 0.14f, 0.04f), new Vector3(0.04f, 0.05f, 0.02f), Mat("CatEar", new Color(0.85f, 0.55f, 0.5f), 0.15f));
        }

        static void Fireplace(Transform home, Vector3 pos)
        {
            var t = Group(home, "Fireplace", pos, 180f);
            Cube("Stone", t, new Vector3(0f, 0.72f, 0f), new Vector3(1.05f, 1.45f, 0.48f), Mat("StoneFp", new Color(0.42f, 0.4f, 0.38f), 0.2f));
            Cube("Chimney", t, new Vector3(0f, 1.55f, 0f), new Vector3(0.42f, 0.55f, 0.42f), Mat("StoneFp", new Color(0.42f, 0.4f, 0.38f), 0.2f));
            Cube("Mouth", t, new Vector3(0f, 0.32f, 0.22f), new Vector3(0.58f, 0.48f, 0.12f), Glow("Fire", new Color(1f, 0.45f, 0.12f), 2.2f));
            var light = Point("FireLight", t, new Vector3(0f, 0.35f, 0.2f), new Color(1f, 0.55f, 0.2f), 1.1f, 4f);
            light.gameObject.name = "FireLight";
        }

        static void Bookshelf(Transform home, Vector3 pos)
        {
            var t = Group(home, "Bookshelf", pos, 180f);
            ZceaModels.Place("bookcaseOpen", t, Vector3.zero, 0f, 1f);
            ZceaModels.Place("books", t, new Vector3(0f, 0.55f, 0.05f), 0f, 1f);
        }

        static void Noren(Transform home, Vector3 pos)
        {
            var t = Group(home, "Noren", pos, 0f);
            Cube("PanelL", t, new Vector3(-0.22f, 0f, 0f), new Vector3(0.38f, 0.85f, 0.02f), Mat("Noren", new Color(0.12f, 0.22f, 0.55f), 0.15f));
            Cube("PanelR", t, new Vector3(0.22f, 0f, 0f), new Vector3(0.38f, 0.85f, 0.02f), Mat("Noren", new Color(0.12f, 0.22f, 0.55f), 0.15f));
            Stroke(t, Color.white, new Vector3(-0.22f, 0.1f, -0.015f), new Vector3(0.12f, 0.28f, 0.01f));
        }

        static void Plant(Transform home, Vector3 pos)
        {
            var t = Group(home, "Plant", pos, 0f);
            ZceaModels.Place("pottedPlant", t, Vector3.zero, 0f, 1.1f);
        }

        static void Stroke(Transform parent, Color color, Vector3 pos, Vector3 scale)
        {
            Cube("Stroke", parent, pos, scale, Mat("Ink" + color.r + pos.x, color, 0.1f));
        }

        static void Kanji(Transform parent, Vector3 pos, string glyph)
        {
            var go = new GameObject("Kanji");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * 0.08f;
            var tm = go.AddComponent<TextMesh>();
            tm.text = glyph;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.08f, 0.08f, 0.08f);
            tm.characterSize = 0.12f;
        }

        static void Legs(Transform table, float span, float y, Material wood)
        {
            Cube("LegA", table, new Vector3(-span, y, -span), new Vector3(0.05f, 0.22f, 0.05f), wood);
            Cube("LegB", table, new Vector3(span, y, -span), new Vector3(0.05f, 0.22f, 0.05f), wood);
            Cube("LegC", table, new Vector3(-span, y, span), new Vector3(0.05f, 0.22f, 0.05f), wood);
            Cube("LegD", table, new Vector3(span, y, span), new Vector3(0.05f, 0.22f, 0.05f), wood);
        }

        static void Strip(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            Cube("Led", parent, pos, scale, mat);
        }

        static void WireDevices(ProfileLook look, Transform home, ProfileId style)
        {
            var rig = look.gameObject.AddComponent<HouseDeviceRig>();
            var sala = Point("Device_sala_luz", home, new Vector3(0f, 2.15f, 0f), SalaColor(style), 1.85f, 8.2f);
            var quarto = Point("Device_quarto_luz", home, new Vector3(-1.5f, 1.85f, 1.45f), new Color(1f, 0.86f, 0.62f), 1.1f, 5.5f);
            var lamp = Group(home, "Device_abajur", new Vector3(-1.85f, 0f, 1.55f), 0f);
            Prim(PrimitiveType.Cylinder, "Stem", lamp, new Vector3(0f, 0.28f, 0f), new Vector3(0.04f, 0.22f, 0.04f), Wood(0.18f, 0.12f, 0.08f));
            Prim(PrimitiveType.Cylinder, "Shade", lamp, new Vector3(0f, 0.52f, 0f), new Vector3(0.18f, 0.1f, 0.18f), Glow("HomeAbajur", new Color(1f, 0.82f, 0.55f), 1.4f));
            var abajur = Point("Light", lamp, new Vector3(0f, 0.52f, 0f), new Color(1f, 0.78f, 0.45f), 0.7f, 3.2f);
            var strip = Group(home, "Device_fita_led", Vector3.zero, 0f);
            var led = Glow("HomeLed_" + style, style == ProfileId.Cyber ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.7f, 0.35f), 1.8f);
            Cube("LedFront", strip, new Vector3(0f, 0.12f, -2.28f), new Vector3(4.4f, 0.03f, 0.04f), led);
            Cube("LedRight", strip, new Vector3(2.28f, 0.12f, 0f), new Vector3(0.04f, 0.03f, 4.4f), led);
            Cube("LedLeft", strip, new Vector3(-2.28f, 0.12f, 0f), new Vector3(0.04f, 0.03f, 4.4f), led);
            Cube("LedBack", strip, new Vector3(0f, 0.12f, 2.28f), new Vector3(4.4f, 0.03f, 0.04f), led);
            var fita = Point("Light", strip, new Vector3(0f, 0.2f, -2f), style == ProfileId.Cyber ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.7f, 0.4f), 0.45f, 4.5f);
            var screen = FindNamed(home, "Screen");
            var tvPos = screen != null ? screen.position : home.TransformPoint(new Vector3(-0.05f, 0.78f, -2.08f));
            var tvLight = Point("Device_tv", home, home.InverseTransformPoint(tvPos), TvColor(style), 0.35f, 2.4f);
            var ambiente = new GameObject("Device_ambiente");
            ambiente.transform.SetParent(home, false);

            rig.sala = sala;
            rig.quarto = quarto;
            rig.abajur = abajur;
            rig.fita = fita;
            rig.tv = tvLight;
            rig.salaGlow = Shades(look.transform, true);
            rig.quartoGlow = Shades(look.transform, false);
            rig.abajurGlow = new[] { lamp.Find("Shade") != null ? lamp.Find("Shade").GetComponent<Renderer>() : null };
            rig.fitaGlow = strip.GetComponentsInChildren<Renderer>();
            rig.tvGlow = screen != null ? new[] { screen.GetComponent<Renderer>() } : System.Array.Empty<Renderer>();
        }

        static Transform FindNamed(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                    return all[i];
            }

            return null;
        }

        static Renderer[] Shades(Transform look, bool front)
        {
            var list = new List<Renderer>();
            var lanterns = look.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < lanterns.Length; i++)
            {
                if (lanterns[i].name != "Shade")
                    continue;
                var z = lanterns[i].localPosition.z;
                var parent = lanterns[i].parent;
                if (parent != null)
                    z = parent.localPosition.z;
                if (front && z < 0f || !front && z >= 0f)
                    list.Add(lanterns[i].GetComponent<Renderer>());
            }

            return list.ToArray();
        }

        static Light Point(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
#if UNITY_EDITOR
            light.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            return light;
        }

        static Color TvColor(ProfileId style)
        {
            return style == ProfileId.Cyber ? new Color(0.15f, 0.55f, 0.85f) : new Color(0.08f, 0.08f, 0.1f);
        }

        static Color SalaColor(ProfileId style)
        {
            switch (style)
            {
                case ProfileId.Cyber: return new Color(0.45f, 0.8f, 1f);
                case ProfileId.Excentrico: return new Color(1f, 0.86f, 0.9f);
                case ProfileId.Acolhedor: return new Color(1f, 0.72f, 0.42f);
                default: return new Color(1f, 0.9f, 0.75f);
            }
        }

        static Material Wood(float r, float g, float b)
        {
            return Mat("Wood" + r + g + b, new Color(r, g, b), 0.28f);
        }

        static Transform Group(Transform parent, string name, Vector3 pos, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            return Prim(PrimitiveType.Cube, name, parent, pos, scale, mat);
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            go.isStatic = false;
            return go;
        }

        static Texture Tex(string id)
        {
            return Resources.Load<Texture>("ZceaTex/" + id);
        }

        static Material TexMat(string name, Color tint, float smoothness, Texture albedo)
        {
            var mat = Mat(name, tint, smoothness);
            if (albedo != null)
            {
                mat.mainTexture = albedo;
                mat.mainTextureScale = new Vector2(3f, 3f);
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", albedo);
                    mat.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
                }
            }
            return mat;
        }

        static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
        {
            if (Mats.TryGetValue(name, out var existing) && existing != null)
                return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader != null ? shader : Shader.Find("Standard")) { name = name };
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            Mats[name] = mat;
            return mat;
        }

        static Material Glow(string name, Color color, float intensity)
        {
            var mat = Mat(name, color, 0.45f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            return mat;
        }
    }
}
