using System.IO;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Light-wood plaque. 始 and COMEÇAR are engraved decals, readable from ~1 m.
    /// Idle: dark cut. Hover: amber emission on the letters only.
    /// </summary>
    public sealed class StartPlaque : MonoBehaviour
    {
        static readonly Color Wood = new Color(0.82f, 0.68f, 0.46f);
        static readonly Color Engraved = new Color(0.22f, 0.12f, 0.06f);
        static readonly Color Amber = new Color(1f, 0.55f, 0.12f);

        DojoDirector director;
        Material wood;
        Material kanjiMat;
        Material wordMat;
        AudioSource chime;

        public static void Ensure(Transform table, DojoDirector director)
        {
            if (table == null || director == null)
                return;
            var existing = table.Find("StartPlaque");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(existing.gameObject);
                else
                    Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject("StartPlaque");
            go.transform.SetParent(table, false);
            var top = table.Find("TableGroup/Table");
            if (top != null && top.GetComponent<Renderer>() != null)
            {
                var bounds = top.GetComponent<Renderer>().bounds;
                go.transform.position = bounds.center + Vector3.up * (bounds.extents.y + 0.12f) + Vector3.forward * -0.04f;
            }
            else
                go.transform.localPosition = new Vector3(0f, 0.78f, -0.12f);

            go.transform.rotation = Quaternion.Euler(-32f, 180f, 0f);
            var plaque = go.AddComponent<StartPlaque>();
            plaque.director = director;
            plaque.Build();
        }

        void Awake()
        {
            if (wood == null)
                Build();
        }

        void Build()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            wood = new Material(lit != null ? lit : Shader.Find("Standard"));
            wood.color = Wood;
            if (wood.HasProperty("_BaseColor"))
                wood.SetColor("_BaseColor", Wood);
            if (wood.HasProperty("_Smoothness"))
                wood.SetFloat("_Smoothness", 0.18f);
            wood.EnableKeyword("_NORMALMAP");

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(transform, false);
            board.transform.localScale = new Vector3(0.18f, 0.24f, 0.02f);
            board.GetComponent<Renderer>().sharedMaterial = wood;
            var box = board.GetComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 2.4f);

            var foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foot.name = "Foot";
            foot.transform.SetParent(transform, false);
            foot.transform.localPosition = new Vector3(0f, -0.13f, 0.01f);
            foot.transform.localScale = new Vector3(0.13f, 0.02f, 0.06f);
            Object.Destroy(foot.GetComponent<Collider>());
            foot.GetComponent<Renderer>().sharedMaterial = wood;

            kanjiMat = Glyph("plaque-kanji");
            wordMat = Glyph("plaque-word");
            Quad("Kanji", new Vector3(0f, 0.038f, 0.022f), new Vector3(0.14f, 0.14f, 1f), kanjiMat);
            Quad("Word", new Vector3(0f, -0.072f, 0.022f), new Vector3(0.17f, 0.05f, 1f), wordMat);
            SetGlow(false);

            var interactable = board.AddComponent<XRSimpleInteractable>();
            var filter = board.AddComponent<XRPokeFilter>();
            filter.pokeConfiguration = new PokeThresholdDatumProperty(new PokeThresholdData
            {
                pokeDirection = PokeAxis.None,
                enablePokeAngleThreshold = false,
                interactionDepthOffset = 0f
            });
            interactable.hoverEntered.AddListener(_ => SetGlow(true));
            interactable.hoverExited.AddListener(_ => SetGlow(false));
            interactable.selectEntered.AddListener(OnSelect);

            chime = gameObject.AddComponent<AudioSource>();
            chime.playOnAwake = false;
            chime.spatialBlend = 0.4f;
            chime.clip = MakeChime();
        }

        void Quad(string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(transform, false);
            q.transform.localPosition = localPos;
            q.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            q.transform.localScale = localScale;
            Object.Destroy(q.GetComponent<Collider>());
            q.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Material Glyph(string resource)
        {
            var tex = Resources.Load<Texture2D>(resource);
            if (tex == null)
            {
                var disk = Path.Combine(Application.dataPath, "Resources", resource + ".png");
                if (File.Exists(disk))
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(File.ReadAllBytes(disk));
                }
            }
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }

            mat.SetFloat("_Surface", 1f);
            mat.renderQueue = 3000;
            return mat;
        }

        void OnSelect(SelectEnterEventArgs args)
        {
            var started = director != null && director.TryBeginVisit();
            SetGlow(true);
            if (chime != null)
            {
                chime.pitch = started ? 1f : 0.7f;
                chime.Play();
            }

            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.HeldInHand | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                devices);
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i].TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    devices[i].SendHapticImpulse(0u, started ? 0.45f : 0.15f, 0.06f);
            }

            if (started)
                Gen.GenHistoryClient.Note("https://genjutsuai.vercel.app", "quest", "COMEÇAR", "start");
            Gen.GenVoiceLog.Info("plaque " + (started ? "start" : "waiting"));
        }

        public void PreviewGlow(bool on)
        {
            SetGlow(on);
        }

        void SetGlow(bool on)
        {
            Paint(kanjiMat, on);
            Paint(wordMat, on);
        }

        static void Paint(Material mat, bool on)
        {
            if (mat == null)
                return;
            var color = on ? new Color(1f, 0.55f, 0.12f, 1f) : new Color(0.22f, 0.12f, 0.06f, 1f);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (on && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Amber * 5f);
            }
        }

        static AudioClip MakeChime()
        {
            const int rate = 22050;
            var n = rate / 4;
            var data = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)rate;
                var env = Mathf.Exp(-t * 7f);
                data[i] = env * 0.22f * (Mathf.Sin(2f * Mathf.PI * 523f * t) + 0.55f * Mathf.Sin(2f * Mathf.PI * 784f * t));
            }

            var clip = AudioClip.Create("StartChime", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
