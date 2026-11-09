using System.Collections.Generic;
using GenJutsu.HouseState;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Lived-in ZCEA homes: bed, sofa, TV, kitchen, ofuro. Quest-cheap primitives, shared materials.
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
            if (look.GetComponentInChildren<HouseDeviceRig>(true) != null)
                return;
            Build(look);
        }

        static void Build(ProfileLook look)
        {
            var root = look.transform.Find("Pieces") ?? look.transform;
            var home = new GameObject("Home");
            home.transform.SetParent(root, false);
            var style = look.profile;
            Bed(home.transform, style);
            Living(home.transform, style);
            Kitchen(home.transform, style);
            Bath(home.transform, style);
            if (style == ProfileId.Excentrico)
                Blocks(home.transform);
            if (style == ProfileId.Cyber)
                Holos(home.transform);
            WireDevices(look, home.transform, style);
        }

        static void Bed(Transform home, ProfileId style)
        {
            var t = Group(home, "Bed", new Vector3(-1.62f, 0f, 0.35f), 8f);
            var frame = Mat("HomeWood", new Color(0.32f, 0.2f, 0.12f), 0.28f);
            var linen = Mat("HomeLinen_" + style, Linen(style), 0.18f);
            var pillow = Mat("HomePillow_" + style, Pillow(style), 0.22f);
            Cube("Frame", t, new Vector3(0f, 0.16f, 0f), new Vector3(1.05f, 0.14f, 2.05f), frame);
            Cube("Mattress", t, new Vector3(0f, 0.28f, 0.04f), new Vector3(0.96f, 0.12f, 1.88f), linen);
            Cube("PillowA", t, new Vector3(-0.18f, 0.38f, 0.78f), new Vector3(0.32f, 0.08f, 0.22f), pillow);
            Cube("PillowB", t, new Vector3(0.2f, 0.38f, 0.78f), new Vector3(0.32f, 0.08f, 0.22f), pillow);
            if (style == ProfileId.Acolhedor)
                Cube("Throw", t, new Vector3(0.12f, 0.34f, -0.55f), new Vector3(0.7f, 0.04f, 0.7f), Mat("HomeThrow", new Color(0.93f, 0.84f, 0.7f), 0.2f));
            if (style == ProfileId.Excentrico)
            {
                Cube("CushionP", t, new Vector3(-0.28f, 0.36f, -0.2f), new Vector3(0.28f, 0.1f, 0.28f), Mat("HomePink", new Color(1f, 0.35f, 0.7f), 0.25f));
                Cube("CushionY", t, new Vector3(0.28f, 0.36f, 0.15f), new Vector3(0.28f, 0.1f, 0.28f), Mat("HomeYellow", new Color(1f, 0.85f, 0.15f), 0.25f));
            }
        }

        static void Living(Transform home, ProfileId style)
        {
            var sofa = Group(home, "Sofa", new Vector3(1.45f, 0f, -1.05f), -90f);
            var fabric = Mat("HomeSofa_" + style, Sofa(style), 0.2f);
            var wood = Mat("HomeWood", new Color(0.32f, 0.2f, 0.12f), 0.28f);
            Cube("Base", sofa, new Vector3(0f, 0.22f, 0f), new Vector3(1.7f, 0.28f, 0.72f), fabric);
            Cube("Back", sofa, new Vector3(0f, 0.48f, 0.28f), new Vector3(1.7f, 0.4f, 0.14f), fabric);
            Cube("ArmL", sofa, new Vector3(-0.82f, 0.38f, 0f), new Vector3(0.12f, 0.28f, 0.72f), fabric);
            Cube("ArmR", sofa, new Vector3(0.82f, 0.38f, 0f), new Vector3(0.12f, 0.28f, 0.72f), fabric);
            Cube("CushA", sofa, new Vector3(-0.4f, 0.4f, -0.04f), new Vector3(0.5f, 0.12f, 0.5f), fabric);
            Cube("CushB", sofa, new Vector3(0.4f, 0.4f, -0.04f), new Vector3(0.5f, 0.12f, 0.5f), fabric);

            var table = Group(home, "Coffee", new Vector3(0.55f, 0f, -1.05f), 0f);
            Cube("Top", table, new Vector3(0f, 0.22f, 0f), new Vector3(0.72f, 0.06f, 0.72f), style == ProfileId.Acolhedor ? Mat("HomeIvory", new Color(0.95f, 0.92f, 0.86f), 0.35f) : wood);
            Cube("LegA", table, new Vector3(-0.28f, 0.1f, -0.28f), new Vector3(0.05f, 0.2f, 0.05f), wood);
            Cube("LegB", table, new Vector3(0.28f, 0.1f, -0.28f), new Vector3(0.05f, 0.2f, 0.05f), wood);
            Cube("LegC", table, new Vector3(-0.28f, 0.1f, 0.28f), new Vector3(0.05f, 0.2f, 0.05f), wood);
            Cube("LegD", table, new Vector3(0.28f, 0.1f, 0.28f), new Vector3(0.05f, 0.2f, 0.05f), wood);

            var tv = Group(home, "TvStand", new Vector3(-0.15f, 0f, -1.05f), 90f);
            Cube("Cabinet", tv, new Vector3(0f, 0.18f, 0f), new Vector3(0.95f, 0.28f, 0.32f), wood);
            Cube("Screen", tv, new Vector3(0f, 0.62f, -0.02f), new Vector3(0.92f, 0.52f, 0.04f), Glow("HomeTv_" + style, TvColor(style), style == ProfileId.Cyber ? 2.2f : 0.35f));
        }

        static void Kitchen(Transform home, ProfileId style)
        {
            var k = Group(home, "Kitchen", new Vector3(1.85f, 0f, 1.95f), 0f);
            var wood = Mat("HomeWoodDark", new Color(0.18f, 0.12f, 0.08f), 0.22f);
            var counter = Mat("HomeCounter_" + style, style == ProfileId.Cyber ? new Color(0.12f, 0.14f, 0.16f) : new Color(0.42f, 0.28f, 0.16f), 0.4f);
            Cube("Base", k, new Vector3(0f, 0.42f, 0f), new Vector3(1.35f, 0.78f, 0.55f), wood);
            Cube("Top", k, new Vector3(0f, 0.84f, 0f), new Vector3(1.4f, 0.06f, 0.6f), counter);
            Cube("Sink", k, new Vector3(0.22f, 0.88f, 0.05f), new Vector3(0.38f, 0.04f, 0.28f), Mat("HomeSteel", new Color(0.55f, 0.58f, 0.6f), 0.7f, 0.6f));
            var kettle = Glow("HomeKettle_" + style, style == ProfileId.Excentrico ? new Color(0.95f, 0.2f, 0.55f) : new Color(0.82f, 0.82f, 0.84f), 0.15f);
            Prim(PrimitiveType.Cylinder, "Kettle", k, new Vector3(-0.35f, 0.98f, 0.05f), new Vector3(0.12f, 0.1f, 0.12f), kettle);
            Prim(PrimitiveType.Cylinder, "MugA", k, new Vector3(-0.12f, 0.9f, 0.12f), new Vector3(0.06f, 0.05f, 0.06f), Mat("HomeMug", Mug(style), 0.35f));
            Prim(PrimitiveType.Cylinder, "MugB", k, new Vector3(-0.02f, 0.9f, 0.12f), new Vector3(0.06f, 0.05f, 0.06f), Mat("HomeMugB", Mug(style) * 0.85f, 0.35f));
            Cube("Towel", k, new Vector3(0.55f, 1.15f, -0.2f), new Vector3(0.18f, 0.28f, 0.04f), Mat("HomeTowel", new Color(0.9f, 0.88f, 0.82f), 0.15f));
        }

        static void Bath(Transform home, ProfileId style)
        {
            var b = Group(home, "Ofuro", new Vector3(-1.75f, 0f, 1.95f), 0f);
            var wood = Mat("HomeOfuro", new Color(0.45f, 0.28f, 0.14f), 0.35f);
            var tile = Mat("HomeTile", new Color(0.78f, 0.76f, 0.7f), 0.45f);
            Cube("Deck", b, new Vector3(0f, 0.02f, 0f), new Vector3(1.15f, 0.04f, 1.15f), tile);
            Prim(PrimitiveType.Cylinder, "Tub", b, new Vector3(0f, 0.28f, 0f), new Vector3(0.85f, 0.26f, 0.85f), wood);
            Prim(PrimitiveType.Cylinder, "Water", b, new Vector3(0f, 0.46f, 0f), new Vector3(0.72f, 0.04f, 0.72f), Glow("HomeWater", new Color(0.45f, 0.62f, 0.7f), 0.2f));
            Cube("Towel", b, new Vector3(0.42f, 0.7f, -0.42f), new Vector3(0.16f, 0.22f, 0.05f), Mat("HomeTowel", new Color(0.9f, 0.88f, 0.82f), 0.15f));
        }

        static void Blocks(Transform home)
        {
            var stack = Group(home, "Blocks", new Vector3(1.85f, 0f, 0.35f), 0f);
            var colors = new[]
            {
                new Color(0.9f, 0.15f, 0.18f), new Color(0.15f, 0.55f, 0.95f), new Color(0.2f, 0.75f, 0.28f),
                new Color(1f, 0.82f, 0.1f), new Color(0.95f, 0.35f, 0.15f), new Color(0.2f, 0.7f, 0.85f)
            };
            var i = 0;
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 2; x++)
            {
                Cube("Cube" + i, stack, new Vector3((x - 0.5f) * 0.34f, 0.2f + y * 0.32f, (y % 2) * 0.08f), Vector3.one * 0.3f, Mat("HomeBlock" + (i % colors.Length), colors[i % colors.Length], 0.3f));
                i++;
            }
        }

        static void Holos(Transform home)
        {
            var pane = Glow("HomeHolo", new Color(0.2f, 0.85f, 1f), 1.6f);
            Cube("HoloL", home, new Vector3(-2.15f, 1.35f, 0.2f), new Vector3(0.02f, 1.15f, 1.6f), pane);
            Cube("HoloR", home, new Vector3(2.15f, 1.35f, 0.2f), new Vector3(0.02f, 1.15f, 1.6f), pane);
        }

        static void WireDevices(ProfileLook look, Transform home, ProfileId style)
        {
            var rig = look.gameObject.AddComponent<HouseDeviceRig>();
            var sala = Point("Device_sala_luz", home, new Vector3(0.2f, 1.92f, -0.85f), SalaColor(style), 1.4f, 7.2f);
            var quarto = Point("Device_quarto_luz", home, new Vector3(-1.5f, 1.85f, 0.45f), new Color(1f, 0.86f, 0.62f), 1.1f, 5.5f);
            var lamp = Group(home, "Device_abajur", new Vector3(-0.95f, 0f, 1.05f), 0f);
            var wood = Mat("HomeWoodDark", new Color(0.18f, 0.12f, 0.08f), 0.22f);
            var shade = Glow("HomeAbajur", new Color(1f, 0.82f, 0.55f), 1.4f);
            Prim(PrimitiveType.Cylinder, "Stem", lamp, new Vector3(0f, 0.28f, 0f), new Vector3(0.04f, 0.22f, 0.04f), wood);
            Prim(PrimitiveType.Cylinder, "Shade", lamp, new Vector3(0f, 0.52f, 0f), new Vector3(0.18f, 0.1f, 0.18f), shade);
            var abajur = Point("Light", lamp, new Vector3(0f, 0.52f, 0f), new Color(1f, 0.78f, 0.45f), 0.7f, 3.2f);
            var strip = Group(home, "Device_fita_led", Vector3.zero, 0f);
            var led = Glow("HomeLed_" + style, style == ProfileId.Cyber ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.7f, 0.35f), 1.8f);
            Cube("LedFront", strip, new Vector3(0f, 0.04f, -2.38f), new Vector3(4.8f, 0.02f, 0.03f), led);
            Cube("LedRight", strip, new Vector3(2.38f, 0.04f, 0f), new Vector3(0.03f, 0.02f, 4.8f), led);
            if (style == ProfileId.Cyber)
            {
                Cube("LedPost", strip, new Vector3(2.45f, 1.2f, 2.45f), new Vector3(0.03f, 2.2f, 0.03f), Glow("HomeLedMag", new Color(0.85f, 0.25f, 1f), 1.8f));
            }

            var fita = Point("Light", strip, new Vector3(0f, 0.2f, -2f), style == ProfileId.Cyber ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.7f, 0.4f), 0.45f, 4.5f);
            var tvLight = Point("Device_tv", home, new Vector3(-0.15f, 0.62f, -1.05f), TvColor(style), 0.35f, 2.4f);
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
            var screen = home.Find("TvStand/Screen");
            rig.tvGlow = screen != null ? new[] { screen.GetComponent<Renderer>() } : System.Array.Empty<Renderer>();
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

        static Color Linen(ProfileId style)
        {
            switch (style)
            {
                case ProfileId.Cyber: return new Color(0.18f, 0.2f, 0.22f);
                case ProfileId.Excentrico: return new Color(0.55f, 0.85f, 0.9f);
                case ProfileId.Acolhedor: return new Color(0.93f, 0.86f, 0.74f);
                default: return new Color(0.86f, 0.82f, 0.74f);
            }
        }

        static Color Pillow(ProfileId style)
        {
            switch (style)
            {
                case ProfileId.Cyber: return new Color(0.28f, 0.32f, 0.36f);
                case ProfileId.Excentrico: return new Color(0.25f, 0.45f, 0.95f);
                case ProfileId.Acolhedor: return new Color(0.9f, 0.78f, 0.62f);
                default: return new Color(0.9f, 0.88f, 0.82f);
            }
        }

        static Color Sofa(ProfileId style)
        {
            switch (style)
            {
                case ProfileId.Cyber: return new Color(0.35f, 0.38f, 0.42f);
                case ProfileId.Excentrico: return new Color(0.2f, 0.72f, 0.78f);
                case ProfileId.Acolhedor: return new Color(0.78f, 0.38f, 0.18f);
                default: return new Color(0.82f, 0.78f, 0.7f);
            }
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

        static Color Mug(ProfileId style)
        {
            switch (style)
            {
                case ProfileId.Excentrico: return new Color(0.9f, 0.25f, 0.55f);
                case ProfileId.Cyber: return new Color(0.55f, 0.6f, 0.65f);
                default: return new Color(0.86f, 0.82f, 0.76f);
            }
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
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            go.isStatic = true;
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
