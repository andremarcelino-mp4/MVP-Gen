using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Light-wood plaque on the white table. Kanji 始 and COMEÇAR.
    /// Hover or touch warms the engraving. A poke or ray select starts the visit morph.
    /// </summary>
    public sealed class StartPlaque : MonoBehaviour
    {
        static readonly Color Wood = new Color(0.86f, 0.72f, 0.5f);
        static readonly Color Engraved = new Color(0.28f, 0.16f, 0.08f);
        static readonly Color Amber = new Color(1f, 0.62f, 0.18f);

        DojoDirector director;
        Material wood;
        TMP_Text kanji;
        TMP_Text word;
        AudioSource chime;
        bool hot;

        public static void Ensure(Transform table, DojoDirector director)
        {
            if (table == null || director == null)
                return;
            var existing = table.Find("StartPlaque");
            if (existing != null)
            {
                var kept = existing.GetComponent<StartPlaque>() ?? existing.gameObject.AddComponent<StartPlaque>();
                kept.director = director;
                return;
            }

            var go = new GameObject("StartPlaque");
            go.transform.SetParent(table, false);
            var top = table.Find("TableGroup/Table");
            if (top != null)
            {
                var bounds = top.GetComponent<Renderer>() != null
                    ? top.GetComponent<Renderer>().bounds
                    : new Bounds(top.position, new Vector3(0.4f, 0.06f, 0.3f));
                go.transform.position = bounds.center + Vector3.up * (bounds.extents.y + 0.11f) + Vector3.forward * -0.05f;
            }
            else
                go.transform.localPosition = new Vector3(0f, 0.78f, -0.12f);

            var face = Camera.main != null ? Camera.main.transform.position - go.transform.position : Vector3.back;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
                go.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
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
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            wood = new Material(shader != null ? shader : Shader.Find("Standard"));
            wood.color = Wood;
            if (wood.HasProperty("_BaseColor"))
                wood.SetColor("_BaseColor", Wood);
            wood.EnableKeyword("_EMISSION");
            SetGlow(false);

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(transform, false);
            board.transform.localScale = new Vector3(0.16f, 0.22f, 0.018f);
            board.GetComponent<Renderer>().sharedMaterial = wood;
            var box = board.GetComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 2.2f);

            var foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foot.name = "Foot";
            foot.transform.SetParent(transform, false);
            foot.transform.localPosition = new Vector3(0f, -0.12f, 0.01f);
            foot.transform.localScale = new Vector3(0.12f, 0.02f, 0.06f);
            Destroy(foot.GetComponent<Collider>());
            foot.GetComponent<Renderer>().sharedMaterial = wood;

            var canvasGo = new GameObject("Engraving", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.01f, 0.011f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.00055f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = new Vector2(240f, 320f);
            kanji = Label(rect, "Kanji", "始", 140f, new Vector2(0f, 0.42f), new Vector2(1f, 1f));
            word = Label(rect, "Word", "COMEÇAR", 42f, new Vector2(0f, 0f), new Vector2(1f, 0.42f));
            PaintText(false);

            var interactable = board.AddComponent<XRSimpleInteractable>();
            var filter = board.AddComponent<XRPokeFilter>();
            filter.pokeConfiguration = new PokeThresholdDatumProperty(new PokeThresholdData
            {
                pokeDirection = PokeAxis.None,
                enablePokeAngleThreshold = false,
                interactionDepthOffset = 0f
            });
            interactable.hoverEntered.AddListener(OnHover);
            interactable.hoverExited.AddListener(OnLeave);
            interactable.selectEntered.AddListener(OnSelect);

            chime = gameObject.AddComponent<AudioSource>();
            chime.playOnAwake = false;
            chime.spatialBlend = 0.4f;
            chime.clip = MakeChime();
        }

        void OnHover(HoverEnterEventArgs _)
        {
            SetGlow(true);
        }

        void OnLeave(HoverExitEventArgs _)
        {
            SetGlow(false);
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
            hot = on;
            var emission = on ? Amber * 1.4f : Color.black;
            if (wood != null)
            {
                if (wood.HasProperty("_EmissionColor"))
                    wood.SetColor("_EmissionColor", emission);
                else
                    wood.SetColor("_EmissionColor", emission);
            }

            PaintText(on);
        }

        void PaintText(bool on)
        {
            var color = on ? Amber : Engraved;
            if (kanji != null)
                kanji.color = color;
            if (word != null)
                word.color = color;
        }

        static TMP_Text Label(RectTransform parent, string name, string value, float size, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
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
