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

            var table = look.transform.Find("Pieces/LowTable");
            if (table != null)
                table.gameObject.SetActive(false);
        }

        static void Build(ProfileLook look)
        {
            var root = look.transform.Find("Pieces") ?? look.transform;
            var home = new GameObject("Home");
            home.transform.SetParent(root, false);
            var marker = home.AddComponent<ZceaFurnished>();
            marker.version = ZceaFurnished.Current;
            var style = look.profile;
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

            SharedProps(home.transform, style);
            WireDevices(look, home.transform, style);
        }

        static void SharedProps(Transform home, ProfileId style)
        {
            Bed(home, style);
            Kitchen(home, style);
        }

        static void Zen(Transform home)
        {
            TvCabinet(home, new Vector3(-0.05f, 0f, -2.08f), 0f, new Color(0.28f, 0.16f, 0.08f), false);
            var table = Group(home, "Chabudai", new Vector3(0.08f, 0f, -0.18f), 0f);
            Cube("Top", table, new Vector3(0f, 0.28f, 0f), new Vector3(0.92f, 0.06f, 0.92f), Wood(0.32f, 0.18f, 0.1f));
            Cube("Apron", table, new Vector3(0f, 0.22f, 0f), new Vector3(0.86f, 0.06f, 0.86f), Wood(0.22f, 0.12f, 0.07f));
            Legs(table, 0.38f, 0.21f, Wood(0.22f, 0.12f, 0.07f));
            TeaSet(table);
            Zabuton(home, "ZabutonN", new Vector3(0.08f, 0.05f, 0.42f), new Color(0.22f, 0.32f, 0.28f));
            Zabuton(home, "ZabutonS", new Vector3(0.08f, 0.05f, -0.72f), new Color(0.22f, 0.32f, 0.28f));
            Zabuton(home, "ZabutonE", new Vector3(0.68f, 0.05f, -0.18f), new Color(0.22f, 0.32f, 0.28f));
            Zabuton(home, "ZabutonW", new Vector3(-0.52f, 0.05f, -0.18f), new Color(0.22f, 0.32f, 0.28f));
            Bonsai(home, new Vector3(1.55f, 0f, -1.85f));
            Ikebana(home, new Vector3(1.85f, 0.02f, 0.15f));
            Scroll(home, new Vector3(0.55f, 1.55f, 2.38f), "Zen", new Color(0.18f, 0.28f, 0.16f));
            Byobu(home, new Vector3(2.05f, 0f, 1.35f));
            SandGarden(home, new Vector3(2.05f, 0.02f, 1.35f));
            LanternHang(home, new Vector3(-1.35f, 0f, -1.05f), new Color(1f, 0.86f, 0.62f));
            LanternHang(home, new Vector3(1.35f, 0f, -1.05f), new Color(1f, 0.86f, 0.62f));
            SofaBench(home, new Vector3(1.65f, 0f, 1.85f), -90f, new Color(0.55f, 0.5f, 0.42f));
        }

        static void Cyber(Transform home)
        {
            var desk = Group(home, "TvStand", new Vector3(-1.65f, 0f, -1.55f), 20f);
            Cube("Top", desk, new Vector3(0f, 0.42f, 0f), new Vector3(1.35f, 0.05f, 0.48f), Mat("CyberDesk", new Color(0.08f, 0.09f, 0.12f), 0.35f));
            Cube("Riser", desk, new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.4f, 0.4f), Mat("CyberDesk", new Color(0.08f, 0.09f, 0.12f), 0.35f));
            Cube("Screen", desk, new Vector3(-0.18f, 0.92f, -0.12f), new Vector3(0.72f, 0.48f, 0.04f), Glow("HomeTv_Cyber", new Color(0.2f, 0.45f, 0.95f), 2.4f));
            Cube("ScreenB", desk, new Vector3(0.48f, 0.82f, -0.08f), new Vector3(0.42f, 0.32f, 0.03f), Glow("CyberMon2", new Color(0.55f, 0.2f, 0.95f), 1.6f));
            Cube("Keyboard", desk, new Vector3(-0.1f, 0.46f, 0.08f), new Vector3(0.42f, 0.02f, 0.14f), Mat("Keys", new Color(0.12f, 0.14f, 0.22f), 0.2f));
            Cube("Console", desk, new Vector3(0.42f, 0.48f, 0.05f), new Vector3(0.22f, 0.05f, 0.16f), Mat("ConsoleC", new Color(0.15f, 0.16f, 0.2f), 0.3f));
            Strip(desk, new Vector3(0f, 0.03f, 0.22f), new Vector3(1.3f, 0.02f, 0.02f), Glow("CyberEdge", new Color(1f, 0.15f, 0.45f), 2f));

            Chair(home, new Vector3(0.05f, 0f, -0.35f));
            var bag = Group(home, "BeanBag", new Vector3(1.35f, 0f, 1.25f), 25f);
            Prim(PrimitiveType.Sphere, "Bag", bag, new Vector3(0f, 0.22f, 0f), new Vector3(0.7f, 0.42f, 0.62f), Mat("Bag", new Color(0.12f, 0.14f, 0.2f), 0.15f));
            SofaBench(home, new Vector3(1.55f, 0f, -0.15f), 90f, new Color(0.18f, 0.2f, 0.28f));
            NeonSign(home, new Vector3(0.85f, 1.72f, 2.38f), new Color(1f, 0.2f, 0.85f));
            Torii(home, new Vector3(-1.15f, 1.55f, 2.38f));
            Katana(home, new Vector3(1.55f, 1.05f, 2.32f));
            Maneki(home, new Vector3(-0.55f, 0.48f, -1.55f));
            LanternHang(home, new Vector3(-1.55f, 0f, 0.85f), new Color(1f, 0.25f, 0.35f));
            LanternHang(home, new Vector3(1.55f, 0f, -1.55f), new Color(1f, 0.25f, 0.35f));
            LanternHang(home, new Vector3(1.55f, 0f, 1.85f), new Color(1f, 0.25f, 0.35f));
            Strip(home, new Vector3(0f, 0.03f, -2.35f), new Vector3(4.6f, 0.02f, 0.03f), Glow("CyberFloor", new Color(0.15f, 0.85f, 1f), 1.8f));
            Strip(home, new Vector3(2.32f, 0.03f, 0f), new Vector3(0.03f, 0.02f, 4.6f), Glow("CyberFloor2", new Color(0.15f, 0.85f, 1f), 1.8f));
        }

        static void Excentrico(Transform home)
        {
            TvCabinet(home, new Vector3(-1.85f, 0f, -1.45f), 90f, new Color(0.18f, 0.18f, 0.2f), true);
            Arcade(home, new Vector3(0.25f, 0f, -2.0f));
            Koinobori(home);
            Oni(home, new Vector3(-0.35f, 1.85f, 2.38f));
            Daruma(home, new Vector3(0.15f, 0.12f, 0.55f));
            Origami(home);
            Parasol(home, new Vector3(1.05f, 0f, -0.55f));
            Dartboard(home, new Vector3(2.32f, 1.45f, 0.85f));
            Lava(home, new Vector3(-2.05f, 0.02f, 0.55f), new Color(1f, 0.35f, 0.1f));
            Lava(home, new Vector3(-1.15f, 0.55f, -1.35f), new Color(0.7f, 0.2f, 1f));
            Stool(home, "StoolA", new Vector3(0.85f, 0f, 0.85f), new Color(0.85f, 0.15f, 0.2f));
            Stool(home, "StoolB", new Vector3(-0.15f, 0f, 1.15f), new Color(0.45f, 0.15f, 0.75f));
            SofaBench(home, new Vector3(1.75f, 0f, 1.75f), -20f, new Color(0.2f, 0.72f, 0.55f));
            Cube("Snack", Group(home, "SnackTable", new Vector3(-0.95f, 0f, -0.35f), 0f), new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.08f, 0.35f), Wood(0.4f, 0.22f, 0.12f));
        }

        static void Acolhedor(Transform home)
        {
            TvCabinet(home, new Vector3(-2.05f, 0f, -1.35f), 90f, new Color(0.28f, 0.16f, 0.08f), false);
            var kota = Group(home, "Kotatsu", new Vector3(0.15f, 0f, -0.05f), 0f);
            kota.name = "Chabudai";
            Cube("Board", kota, new Vector3(0f, 0.32f, 0f), new Vector3(1.15f, 0.05f, 1.15f), Wood(0.38f, 0.22f, 0.12f));
            Cube("Cloth", kota, new Vector3(0f, 0.2f, 0f), new Vector3(1.35f, 0.22f, 1.35f), Mat("KotatsuCloth", new Color(0.72f, 0.22f, 0.18f), 0.18f));
            Prim(PrimitiveType.Sphere, "OrangeA", kota, new Vector3(-0.08f, 0.4f, 0.05f), Vector3.one * 0.08f, Mat("Orange", new Color(0.95f, 0.45f, 0.12f), 0.35f));
            Prim(PrimitiveType.Sphere, "OrangeB", kota, new Vector3(0.06f, 0.4f, -0.04f), Vector3.one * 0.08f, Mat("Orange", new Color(0.95f, 0.45f, 0.12f), 0.35f));
            Prim(PrimitiveType.Cylinder, "CupA", kota, new Vector3(-0.22f, 0.38f, 0.18f), new Vector3(0.07f, 0.04f, 0.07f), Mat("Cup", new Color(0.92f, 0.9f, 0.84f), 0.3f));
            Prim(PrimitiveType.Cylinder, "CupB", kota, new Vector3(0.22f, 0.38f, -0.16f), new Vector3(0.07f, 0.04f, 0.07f), Mat("Cup", new Color(0.92f, 0.9f, 0.84f), 0.3f));
            Zabuton(home, "ZabutonN", new Vector3(0.15f, 0.05f, 0.72f), new Color(0.55f, 0.18f, 0.16f));
            Zabuton(home, "ZabutonE", new Vector3(0.85f, 0.05f, -0.05f), new Color(0.15f, 0.28f, 0.55f));
            Zabuton(home, "ZabutonW", new Vector3(-0.55f, 0.05f, -0.05f), new Color(0.82f, 0.42f, 0.22f));
            Cat(home, new Vector3(-0.15f, 0.16f, 0.72f));
            Fireplace(home, new Vector3(0.35f, 0f, 2.15f));
            Bookshelf(home, new Vector3(-0.95f, 0f, 2.15f));
            Noren(home, new Vector3(-1.55f, 1.55f, 2.38f));
            Scroll(home, new Vector3(-0.35f, 1.55f, 2.38f), "Aco", new Color(0.55f, 0.42f, 0.28f));
            Plant(home, new Vector3(1.85f, 0f, -0.35f));
            Plant(home, new Vector3(-2.05f, 0f, 1.55f));
            Sofa(home, new Vector3(-1.85f, 0f, 0.15f), 90f, new Color(0.72f, 0.68f, 0.6f));
            Prim(PrimitiveType.Cylinder, "TeapotLoose", home, new Vector3(-1.35f, 0.18f, 0.85f), new Vector3(0.1f, 0.08f, 0.1f), Mat("IronPot", new Color(0.18f, 0.2f, 0.22f), 0.4f, 0.5f));
        }

        static void Bed(Transform home, ProfileId style)
        {
            var t = Group(home, "Bed", new Vector3(-1.85f, 0f, 1.75f), 8f);
            var linen = style == ProfileId.Cyber ? new Color(0.16f, 0.18f, 0.22f)
                : style == ProfileId.Excentrico ? new Color(0.55f, 0.85f, 0.9f)
                : style == ProfileId.Acolhedor ? new Color(0.93f, 0.86f, 0.74f)
                : new Color(0.86f, 0.82f, 0.74f);
            Cube("Futon", t, new Vector3(0f, 0.08f, 0f), new Vector3(0.95f, 0.08f, 1.7f), Mat("Futon_" + style, linen, 0.18f));
            Cube("Pillow", t, new Vector3(0f, 0.16f, 0.7f), new Vector3(0.4f, 0.08f, 0.22f), Mat("Pillow_" + style, linen * 1.08f, 0.2f));
        }

        static void Kitchen(Transform home, ProfileId style)
        {
            var k = Group(home, "Kitchen", new Vector3(1.95f, 0f, 1.85f), 0f);
            Cube("Shelf", k, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.08f, 0.32f), Wood(0.22f, 0.12f, 0.08f));
            Cube("Base", k, new Vector3(0f, 0.22f, 0f), new Vector3(0.65f, 0.44f, 0.3f), Wood(0.18f, 0.1f, 0.06f));
            var kettle = Glow("HomeKettle_" + style, style == ProfileId.Excentrico ? new Color(0.95f, 0.2f, 0.55f) : new Color(0.82f, 0.82f, 0.84f), 0.15f);
            Prim(PrimitiveType.Cylinder, "Kettle", k, new Vector3(-0.18f, 0.72f, 0f), new Vector3(0.12f, 0.1f, 0.12f), kettle);
            Prim(PrimitiveType.Cylinder, "MugA", k, new Vector3(0.12f, 0.64f, 0.04f), new Vector3(0.06f, 0.05f, 0.06f), Mat("HomeMug", new Color(0.86f, 0.82f, 0.76f), 0.35f));
            Prim(PrimitiveType.Cylinder, "MugB", k, new Vector3(0.22f, 0.64f, 0.04f), new Vector3(0.06f, 0.05f, 0.06f), Mat("HomeMugB", new Color(0.75f, 0.7f, 0.62f), 0.35f));
        }

        static void TvCabinet(Transform home, Vector3 pos, float yaw, Color wood, bool retro)
        {
            var tv = Group(home, "TvStand", pos, yaw);
            Cube("Cabinet", tv, new Vector3(0f, 0.22f, 0f), new Vector3(1.05f, 0.4f, 0.34f), Mat("TvWood", wood, 0.28f));
            Cube("Screen", tv, new Vector3(0f, 0.78f, 0.02f), new Vector3(0.92f, 0.58f, 0.05f), Glow("HomeTvScreen", retro ? new Color(0.25f, 0.7f, 0.45f) : new Color(0.08f, 0.08f, 0.1f), retro ? 0.8f : 0.25f));
            Cube("Console", tv, new Vector3(0.22f, 0.44f, 0.12f), new Vector3(0.28f, 0.07f, 0.18f), Mat("Console", new Color(0.82f, 0.82f, 0.84f), 0.35f));
            Cube("Deck", tv, new Vector3(-0.28f, 0.44f, 0.1f), new Vector3(0.22f, 0.05f, 0.16f), Mat("Deck", new Color(0.12f, 0.12f, 0.14f), 0.25f));
        }

        static void Sofa(Transform home, Vector3 pos, float yaw, Color fabric)
        {
            var sofa = Group(home, "Sofa", pos, yaw);
            var mat = Mat("SofaFab", fabric, 0.2f);
            Cube("Base", sofa, new Vector3(0f, 0.22f, 0f), new Vector3(1.55f, 0.28f, 0.68f), mat);
            Cube("Back", sofa, new Vector3(0f, 0.48f, 0.26f), new Vector3(1.55f, 0.4f, 0.14f), mat);
            Cube("ArmL", sofa, new Vector3(-0.74f, 0.38f, 0f), new Vector3(0.12f, 0.28f, 0.68f), mat);
            Cube("ArmR", sofa, new Vector3(0.74f, 0.38f, 0f), new Vector3(0.12f, 0.28f, 0.68f), mat);
        }

        static void SofaBench(Transform home, Vector3 pos, float yaw, Color fabric)
        {
            var sofa = Group(home, "Sofa", pos, yaw);
            Cube("Base", sofa, new Vector3(0f, 0.18f, 0f), new Vector3(1.1f, 0.2f, 0.42f), Mat("Bench", fabric, 0.2f));
            Cube("Back", sofa, new Vector3(0f, 0.38f, 0.16f), new Vector3(1.1f, 0.28f, 0.08f), Mat("Bench", fabric, 0.2f));
        }

        static void TeaSet(Transform table)
        {
            Prim(PrimitiveType.Cylinder, "Pot", table, new Vector3(0.05f, 0.38f, 0.02f), new Vector3(0.12f, 0.1f, 0.12f), Mat("TeaPot", new Color(0.9f, 0.9f, 0.88f), 0.4f));
            Prim(PrimitiveType.Cylinder, "Cup", table, new Vector3(-0.18f, 0.34f, 0.12f), new Vector3(0.06f, 0.04f, 0.06f), Mat("TeaCup", new Color(0.95f, 0.95f, 0.93f), 0.35f));
            Cube("Tray", table, new Vector3(0f, 0.32f, 0.02f), new Vector3(0.42f, 0.02f, 0.28f), Wood(0.25f, 0.14f, 0.08f));
            Cube("RemoteHold", table, new Vector3(-0.22f, 0.32f, -0.22f), new Vector3(0.12f, 0.015f, 0.08f), Mat("Hold", new Color(0.85f, 0.85f, 0.82f), 0.2f));
        }

        static void Zabuton(Transform home, string name, Vector3 pos, Color color)
        {
            Cube(name, home, pos, new Vector3(0.42f, 0.08f, 0.42f), Mat(name, color, 0.18f));
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
            var t = Group(home, "Byobu", pos, -25f);
            var paper = Mat("ByobuPaper", new Color(0.82f, 0.72f, 0.38f), 0.12f);
            var wood = Wood(0.28f, 0.16f, 0.08f);
            for (var i = 0; i < 4; i++)
            {
                var p = Group(t, "Panel" + i, new Vector3(i * 0.38f - 0.55f, 0.7f, i * 0.04f), i * 8f);
                Cube("Paper", p, Vector3.zero, new Vector3(0.36f, 1.35f, 0.03f), paper);
                Cube("Frame", p, Vector3.zero, new Vector3(0.38f, 1.38f, 0.02f), wood);
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
            Prim(PrimitiveType.Cylinder, "Cord", hang, new Vector3(0f, 2.28f, 0f), new Vector3(0.012f, 0.12f, 0.012f), Wood(0.16f, 0.1f, 0.07f));
            Prim(PrimitiveType.Cylinder, "Cap", hang, new Vector3(0f, 2.12f, 0f), new Vector3(0.16f, 0.02f, 0.16f), Wood(0.16f, 0.1f, 0.07f));
            Prim(PrimitiveType.Cylinder, "Shade", hang, new Vector3(0f, 1.92f, 0f), new Vector3(0.22f, 0.16f, 0.22f), Glow("Lantern" + xz.x, paper, 1.3f));
        }

        static void Chair(Transform home, Vector3 pos)
        {
            var t = Group(home, "Chair", pos, 15f);
            var body = Glow("ChairNeon", new Color(0.1f, 0.85f, 1f), 1.4f);
            Cube("Seat", t, new Vector3(0f, 0.48f, 0f), new Vector3(0.48f, 0.08f, 0.48f), Mat("Chair", new Color(0.08f, 0.1f, 0.14f), 0.25f));
            Cube("Back", t, new Vector3(0f, 0.85f, -0.2f), new Vector3(0.48f, 0.7f, 0.08f), Mat("Chair", new Color(0.08f, 0.1f, 0.14f), 0.25f));
            Cube("Edge", t, new Vector3(0f, 0.85f, -0.24f), new Vector3(0.5f, 0.72f, 0.02f), body);
            Prim(PrimitiveType.Cylinder, "Stem", t, new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.22f, 0.08f), Mat("Metal", new Color(0.2f, 0.22f, 0.25f), 0.5f, 0.6f));
            Prim(PrimitiveType.Cylinder, "Base", t, new Vector3(0f, 0.06f, 0f), new Vector3(0.36f, 0.03f, 0.36f), Mat("Metal", new Color(0.2f, 0.22f, 0.25f), 0.5f, 0.6f));
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
            var t = Group(home, "Koinobori", new Vector3(-2.15f, 1.7f, 0.2f), 90f);
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
            Prim(PrimitiveType.Sphere, "Face", t, Vector3.zero, new Vector3(0.32f, 0.36f, 0.16f), Mat("OniRed", new Color(0.75f, 0.08f, 0.08f), 0.25f));
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
            {
                var c = Cube("Crane" + i, t, new Vector3((i - 2) * 0.22f, Mathf.Sin(i) * 0.08f, 0f), new Vector3(0.12f, 0.02f, 0.08f), Mat("Crane" + i, colors[i], 0.25f));
                c.transform.localRotation = Quaternion.Euler(0f, i * 18f, 25f);
            }
        }

        static void Parasol(Transform home, Vector3 pos)
        {
            var t = Group(home, "Parasol", pos, -20f);
            Prim(PrimitiveType.Cylinder, "Pole", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.03f, 0.7f, 0.03f), Wood(0.2f, 0.12f, 0.08f));
            Prim(PrimitiveType.Sphere, "Canopy", t, new Vector3(0f, 1.35f, 0f), new Vector3(1.1f, 0.18f, 1.1f), Mat("Aka", new Color(0.85f, 0.08f, 0.08f), 0.2f));
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
            Prim(PrimitiveType.Cylinder, "Seat", t, new Vector3(0f, 0.22f, 0f), new Vector3(0.22f, 0.04f, 0.22f), Mat(name, color, 0.25f));
            Prim(PrimitiveType.Cylinder, "Leg", t, new Vector3(0f, 0.1f, 0f), new Vector3(0.05f, 0.12f, 0.05f), Wood(0.2f, 0.12f, 0.08f));
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
            Cube("Stone", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.95f, 1.35f, 0.42f), Mat("StoneFp", new Color(0.42f, 0.4f, 0.38f), 0.2f));
            Cube("Mouth", t, new Vector3(0f, 0.32f, 0.18f), new Vector3(0.55f, 0.45f, 0.12f), Glow("Fire", new Color(1f, 0.45f, 0.12f), 2.2f));
            var light = Point("FireLight", t, new Vector3(0f, 0.35f, 0.2f), new Color(1f, 0.55f, 0.2f), 1.1f, 4f);
            light.gameObject.name = "FireLight";
        }

        static void Bookshelf(Transform home, Vector3 pos)
        {
            var t = Group(home, "Bookshelf", pos, 180f);
            Cube("Case", t, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 1.65f, 0.28f), Wood(0.32f, 0.18f, 0.1f));
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                Cube("Book" + y + x, t, new Vector3(-0.22f + x * 0.14f, 0.28f + y * 0.38f, -0.02f), new Vector3(0.08f, 0.22f, 0.16f),
                    Mat("Book" + ((x + y) % 4), new Color(0.35f + x * 0.1f, 0.18f, 0.12f + y * 0.08f), 0.2f));
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
            Prim(PrimitiveType.Cylinder, "Pot", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.22f, 0.12f, 0.22f), Mat("Clay", new Color(0.45f, 0.28f, 0.16f), 0.2f));
            Prim(PrimitiveType.Sphere, "Leaf", t, new Vector3(0f, 0.38f, 0f), new Vector3(0.32f, 0.28f, 0.28f), Mat("LeafP", new Color(0.18f, 0.48f, 0.18f), 0.2f));
        }

        static void Stroke(Transform parent, Color color, Vector3 pos, Vector3 scale)
        {
            Cube("Stroke", parent, pos, scale, Mat("Ink" + color.r + pos.x, color, 0.1f));
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
            var sala = Point("Device_sala_luz", home, new Vector3(0.2f, 1.92f, -0.55f), SalaColor(style), 1.4f, 7.2f);
            var quarto = Point("Device_quarto_luz", home, new Vector3(-1.5f, 1.85f, 1.45f), new Color(1f, 0.86f, 0.62f), 1.1f, 5.5f);
            var lamp = Group(home, "Device_abajur", new Vector3(-0.95f, 0f, 1.15f), 0f);
            Prim(PrimitiveType.Cylinder, "Stem", lamp, new Vector3(0f, 0.28f, 0f), new Vector3(0.04f, 0.22f, 0.04f), Wood(0.18f, 0.12f, 0.08f));
            Prim(PrimitiveType.Cylinder, "Shade", lamp, new Vector3(0f, 0.52f, 0f), new Vector3(0.18f, 0.1f, 0.18f), Glow("HomeAbajur", new Color(1f, 0.82f, 0.55f), 1.4f));
            var abajur = Point("Light", lamp, new Vector3(0f, 0.52f, 0f), new Color(1f, 0.78f, 0.45f), 0.7f, 3.2f);
            var strip = Group(home, "Device_fita_led", Vector3.zero, 0f);
            var led = Glow("HomeLed_" + style, style == ProfileId.Cyber ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.7f, 0.35f), 1.8f);
            Cube("LedFront", strip, new Vector3(0f, 0.04f, -2.38f), new Vector3(4.8f, 0.02f, 0.03f), led);
            Cube("LedRight", strip, new Vector3(2.38f, 0.04f, 0f), new Vector3(0.03f, 0.02f, 4.8f), led);
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
