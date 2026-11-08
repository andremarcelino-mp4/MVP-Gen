using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// Quest 3S graphics: PBR maps, mixed GI, boxed probes, MSAA 4x, eye scale 1.15.
    /// </summary>
    public static class QuestGraphicsPass
    {
        const string TexFolder = "Assets/GenJutsu/ProfileDemo/Textures/Pbr";

        [MenuItem("GenJutsu/Quest Graphics Pass")]
        public static void ApplyMenu()
        {
            Debug.Log("[QuestGfx] " + Apply());
        }

        public static string Apply()
        {
            Directory.CreateDirectory(TexFolder);
            var wood = MakeMaps("wood", 1024, WoodAlbedo, WoodNormal, WoodMask, new Color(0.42f, 0.27f, 0.15f));
            var washi = MakeMaps("washi", 1024, WashiAlbedo, WashiNormal, WashiMask, new Color(0.96f, 0.93f, 0.85f));
            var tatami = MakeMaps("tatami", 1024, TatamiAlbedo, TatamiNormal, TatamiMask, new Color(0.76f, 0.72f, 0.5f));
            var stone = MakeMaps("stone", 1024, StoneAlbedo, StoneNormal, StoneMask, new Color(0.32f, 0.33f, 0.34f));
            Bind("DojoWood", wood, 0.45f, 0.08f);
            Bind("DojoWoodDark", wood, 0.55f, 0.12f);
            Bind("DojoLowTable", wood, 0.35f, 0.04f);
            Bind("DojoShoji", washi, 0.15f, 0.02f);
            Bind("DojoTatami", tatami, 0.7f, 0.06f);
            Bind("DojoTatamiEdge", tatami, 0.55f, 0.1f);
            Bind("DojoStone", stone, 0.85f, 0.08f);
            Bind("DojoStoneLight", stone, 0.7f, 0.06f);
            TunePipeline();
            var director = Object.FindFirstObjectByType<ProfileDemoDirector>();
            QuestGiRig.Ensure(director);
            QuestClarity.Ensure(director);
            Lightmapping.lightingSettings = LightingSettingsOrCreate();
            AssetDatabase.SaveAssets();
            return "PBR maps + mixed GI rig + clarity 1.15/MSAA4";
        }

        static void TunePipeline()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null)
                    continue;
                asset.msaaSampleCount = 4;
                asset.renderScale = QuestClarity.EyeScale;
                SerializedObject so = new SerializedObject(asset);
                so.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
                so.FindProperty("m_MixedLightingSupported").boolValue = true;
                var add = so.FindProperty("m_AdditionalLightsRenderingMode");
                if (add != null)
                    add.intValue = 1;
                var limit = so.FindProperty("m_AdditionalLightsPerObjectLimit");
                if (limit != null)
                    limit.intValue = 4;
                var shadow = so.FindProperty("m_MainLightShadowmapResolution");
                if (shadow != null && shadow.intValue > 2048)
                    shadow.intValue = 2048;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        static LightingSettings LightingSettingsOrCreate()
        {
            const string path = "Assets/GenJutsu/ProfileDemo/QuestLighting.settings";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings();
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.Shadowmask;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.directSampleCount = 16;
            settings.indirectSampleCount = 32;
            settings.environmentSampleCount = 64;
            settings.lightmapMaxSize = 1024;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        struct Maps
        {
            public Texture2D albedo;
            public Texture2D normal;
            public Texture2D mask;
        }

        delegate Color Pixel(int x, int y, int n);
        delegate Vector3 Nrm(int x, int y, int n);

        static Maps MakeMaps(string name, int n, Pixel albedo, Nrm nrm, Pixel mask, Color fallback)
        {
            return new Maps
            {
                albedo = Write(name + "_albedo", n, albedo, false, fallback),
                normal = WriteNormal(name + "_n", n, nrm),
                mask = Write(name + "_mask", n, mask, false, new Color(1f, 1f, 0.4f, 0.2f))
            };
        }

        static void Bind(string matName, Maps maps, float bump, float smoothness)
        {
            var guids = AssetDatabase.FindAssets(matName + " t:Material");
            if (guids.Length == 0)
                return;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (mat == null)
                return;
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_BaseMap", maps.albedo);
            mat.SetTexture("_BumpMap", maps.normal);
            mat.SetTexture("_MetallicGlossMap", maps.mask);
            mat.SetFloat("_BumpScale", bump);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.02f);
            EditorUtility.SetDirty(mat);
        }

        static Texture2D Write(string name, int n, Pixel fn, bool linear, Color _)
        {
            var path = TexFolder + "/" + name + ".png";
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, linear);
            var px = new Color[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                px[y * n + x] = fn(x, y, n);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.sRGBTexture = !linear;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.maxTextureSize = n;
            var android = imp.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = n;
            android.format = TextureImporterFormat.ASTC_6x6;
            imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D WriteNormal(string name, int n, Nrm nrm)
        {
            var path = TexFolder + "/" + name + ".png";
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, true);
            var px = new Color[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var v = nrm(x, y, n).normalized;
                px[y * n + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
            }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.NormalMap;
            imp.sRGBTexture = false;
            var android = imp.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = n;
            android.format = TextureImporterFormat.ASTC_6x6;
            imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float N(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }

        static Color WoodAlbedo(int x, int y, int n)
        {
            var u = x / (float)n;
            var v = y / (float)n;
            var grain = N(u * 18f, v * 3.2f) * 0.55f + N(u * 42f, v * 9f) * 0.2f;
            var wear = Mathf.SmoothStep(0.72f, 1f, N(u * 6f, v * 6f));
            var c = Color.Lerp(new Color(0.38f, 0.22f, 0.12f), new Color(0.55f, 0.36f, 0.2f), grain);
            return Color.Lerp(c, new Color(0.62f, 0.5f, 0.36f), wear * 0.18f);
        }

        static Vector3 WoodNormal(int x, int y, int n)
        {
            var u = x / (float)n * 18f;
            var v = y / (float)n * 3.2f;
            return new Vector3((N(u + 0.02f, v) - N(u - 0.02f, v)) * 4f, (N(u, v + 0.02f) - N(u, v - 0.02f)) * 1.2f, 1f);
        }

        static Color WoodMask(int x, int y, int n)
        {
            var g = N(x / (float)n * 20f, y / (float)n * 4f);
            return new Color(0.03f, 1f, 0f, 0.18f + g * 0.22f);
        }

        static Color WashiAlbedo(int x, int y, int n)
        {
            var f = 0.88f + N(x * 0.07f, y * 0.07f) * 0.08f + N(x * 0.31f, y * 0.29f) * 0.04f;
            return new Color(f, f * 0.97f, f * 0.9f);
        }

        static Vector3 WashiNormal(int x, int y, int n)
        {
            var u = x * 0.09f;
            var v = y * 0.09f;
            return new Vector3((N(u + 0.05f, v) - N(u, v)) * 1.4f, (N(u, v + 0.05f) - N(u, v)) * 1.4f, 1f);
        }

        static Color WashiMask(int x, int y, int n)
        {
            return new Color(0.01f, 1f, 0f, 0.08f + N(x * 0.05f, y * 0.05f) * 0.06f);
        }

        static Color TatamiAlbedo(int x, int y, int n)
        {
            var u = x / (float)n;
            var v = y / (float)n;
            var straw = ((int)(u * 48f) + (int)(v * 12f)) % 2 == 0 ? 0.08f : 0f;
            var weave = N(u * 80f, v * 14f);
            var c = Color.Lerp(new Color(0.7f, 0.66f, 0.42f), new Color(0.82f, 0.78f, 0.54f), weave);
            return c + Color.white * straw * 0.04f;
        }

        static Vector3 TatamiNormal(int x, int y, int n)
        {
            var u = x / (float)n * 80f;
            var v = y / (float)n * 14f;
            return new Vector3((N(u + 0.1f, v) - N(u, v)) * 3f, (N(u, v + 0.1f) - N(u, v)) * 0.8f, 1f);
        }

        static Color TatamiMask(int x, int y, int n)
        {
            return new Color(0.02f, 1f, 0f, 0.12f);
        }

        static Color StoneAlbedo(int x, int y, int n)
        {
            var p = N(x * 0.02f, y * 0.02f);
            var speckle = N(x * 0.2f, y * 0.2f);
            var c = Color.Lerp(new Color(0.24f, 0.25f, 0.26f), new Color(0.4f, 0.4f, 0.38f), p);
            return Color.Lerp(c, new Color(0.5f, 0.48f, 0.44f), speckle * 0.15f);
        }

        static Vector3 StoneNormal(int x, int y, int n)
        {
            var u = x * 0.04f;
            var v = y * 0.04f;
            return new Vector3((N(u + 0.04f, v) - N(u, v)) * 2.4f, (N(u, v + 0.04f) - N(u, v)) * 2.4f, 1f);
        }

        static Color StoneMask(int x, int y, int n)
        {
            return new Color(0.04f, 1f, 0f, 0.16f + N(x * 0.03f, y * 0.03f) * 0.1f);
        }
    }
}
