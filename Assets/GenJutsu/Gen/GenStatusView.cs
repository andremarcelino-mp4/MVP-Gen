using GenJutsu.GenCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Floating Gen card: dark glass, #ff1a1a glow, crow flipbook, lazy-follow ~1.2 m.
    /// </summary>
    public sealed class GenStatusView : MonoBehaviour
    {
        public bool FollowHead = true;
        public TMP_Text phaseLabel;
        public TMP_Text caption;
        Image background;
        Image border;
        Image crow;
        RectTransform meterFill;
        CanvasGroup group;
        Texture2D sheet;
        Sprite[] crowFrames;
        float fade;
        float shownUntil;
        Vector3 follow;
        bool followInit;

        static readonly Color Accent = new Color(1f, 0.102f, 0.102f, 1f);

        public void EnsureVisual()
        {
            if (phaseLabel != null)
                return;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 8f;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(920f, 280f);
            transform.localScale = Vector3.one * 0.00115f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;

            border = Quad(rect, "Border", Accent, Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f));
            background = Quad(rect, "Background", new Color(0.04f, 0.04f, 0.05f, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var crowGo = new GameObject("Crow", typeof(RectTransform));
            crowGo.transform.SetParent(transform, false);
            var crowRect = (RectTransform)crowGo.transform;
            Stretch(crowRect, new Vector2(0f, 0.12f), new Vector2(0.28f, 0.92f), new Vector2(18f, 8f), new Vector2(-8f, -8f));
            crow = crowGo.AddComponent<Image>();
            crow.raycastTarget = false;
            sheet = CrowSheet();
            crowFrames = new Sprite[4];
            for (var i = 0; i < 4; i++)
                crowFrames[i] = Sprite.Create(sheet, new Rect(i * 64, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
            crow.sprite = crowFrames[0];
            crow.preserveAspect = true;

            phaseLabel = Text(rect, "Phase", "", 36f, Color.white,
                new Vector2(0.28f, 0.58f), new Vector2(1f, 1f), new Vector2(8f, -16f), new Vector2(-28f, -10f));
            phaseLabel.fontStyle = FontStyles.Bold;
            caption = Text(rect, "Caption", "", 28f, new Color(0.95f, 0.95f, 0.96f),
                new Vector2(0.28f, 0.12f), new Vector2(1f, 0.62f), new Vector2(8f, 10f), new Vector2(-28f, 0f));
            caption.textWrappingMode = TextWrappingModes.Normal;

            var trackGo = new GameObject("MicTrack", typeof(RectTransform));
            trackGo.transform.SetParent(transform, false);
            var track = (RectTransform)trackGo.transform;
            Stretch(track, new Vector2(0.28f, 0f), new Vector2(1f, 0.14f), new Vector2(8f, 12f), new Vector2(-28f, 6f));
            trackGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
            var fillGo = new GameObject("MicLevel", typeof(RectTransform));
            fillGo.transform.SetParent(track, false);
            meterFill = (RectTransform)fillGo.transform;
            Stretch(meterFill, Vector2.zero, new Vector2(0.04f, 1f), Vector2.zero, Vector2.zero);
            fillGo.AddComponent<Image>().color = Accent;
            trackGo.SetActive(false);
        }

        public void Show(GenVoicePhase phase, string reply)
        {
            EnsureVisual();
            var listening = phase == GenVoicePhase.Listening;
            if (phaseLabel != null)
                phaseLabel.text = listening ? "OUVINDO" : (string.IsNullOrEmpty(reply) ? GenVoicePhases.Label(phase) : "");
            if (caption != null)
                caption.text = listening && string.IsNullOrEmpty(reply) ? "Segure B e fale" : (reply ?? "");
            if (!string.IsNullOrEmpty(reply) || listening)
                shownUntil = Time.unscaledTime + (listening ? 99f : 8f);
            if (meterFill != null)
                meterFill.parent.gameObject.SetActive(listening);
            if (!listening)
                SetLevel(0f);
        }

        public void SetLevel(float rms)
        {
            EnsureVisual();
            if (meterFill == null)
                return;
            var level = Mathf.Clamp01(rms * 12f);
            meterFill.anchorMax = new Vector2(Mathf.Max(0.04f, level), 1f);
            meterFill.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            EnsureVisual();
            var want = Time.unscaledTime < shownUntil ? 1f : 0f;
            fade = Mathf.MoveTowards(fade, want, Time.deltaTime * 2.4f);
            if (group != null)
                group.alpha = fade;
            if (crow != null && crowFrames != null && crowFrames.Length > 0)
                crow.sprite = crowFrames[(int)(Time.unscaledTime * 8f) % crowFrames.Length];

            if (!FollowHead)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;
            var head = cam.transform;
            var target = head.position + head.forward * 1.2f + head.up * 0.05f;
            if (!followInit)
            {
                follow = target;
                followInit = true;
            }
            follow = Vector3.Lerp(follow, target, 1f - Mathf.Exp(-Time.deltaTime * 3.2f));
            follow.y += Mathf.Sin(Time.unscaledTime * 1.4f) * 0.012f;
            transform.position = follow;
            var toCam = head.position - follow;
            if (toCam.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-toCam, Vector3.up);
        }

        public static Texture2D CrowSheet()
        {
            var tex = new Texture2D(256, 64, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[256 * 64];
            var fill = new Color(0.18f, 0.18f, 0.2f, 1f);
            var edge = new Color(0.92f, 0.9f, 0.9f, 1f);
            var beak = new Color(1f, 0.55f, 0.15f, 1f);
            for (var f = 0; f < 4; f++)
            {
                var ox = f * 64;
                var flap = f % 2 == 0 ? -7 : 8;
                for (var y = 2; y < 62; y++)
                for (var x = 4; x < 60; x++)
                {
                    var dx = (x - 28) / 18f;
                    var dy = (y - 32) / 16f;
                    var body = dx * dx * 0.75f + dy * dy < 1f;
                    var bill = x > 46 && x < 60 && y > 28 && y < 38 && y > 26 + (x - 46) * 0.15f;
                    var wing = Mathf.Abs(y - (32 + flap)) < 7 && x > 10 && x < 38;
                    var eye = (x - 40) * (x - 40) + (y - 36) * (y - 36) < 10;
                    Color c = default;
                    if (bill)
                        c = beak;
                    else if (eye)
                        c = Accent;
                    else if (body || wing)
                        c = fill;
                    if (c.a < 0.5f)
                        continue;
                    var outline = !body && !bill && !wing;
                    if (outline)
                        continue;
                    px[y * 256 + ox + x] = c;
                    StampEdge(px, ox, x, y, edge, body || wing || bill);
                }
            }

            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        static void StampEdge(Color[] px, int ox, int x, int y, Color edge, bool solid)
        {
            if (!solid)
                return;
            for (var oy = -1; oy <= 1; oy++)
            for (var ox2 = -1; ox2 <= 1; ox2++)
            {
                if (ox2 == 0 && oy == 0)
                    continue;
                var xx = x + ox2;
                var yy = y + oy;
                if (xx < 0 || xx >= 64 || yy < 0 || yy >= 64)
                    continue;
                var i = yy * 256 + ox + xx;
                if (px[i].a < 0.1f)
                    px[i] = edge;
            }
        }

        static Image Quad(RectTransform parent, string name, Color color, Vector2 a0, Vector2 a1, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, a0, a1, o0, o1);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TMP_Text Text(RectTransform parent, string name, string value, float size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, anchorMin, anchorMax, offsetMin, offsetMax);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
        }

        static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
