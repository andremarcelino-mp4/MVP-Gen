using GenJutsu.GenCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// End-of-visit token line. Prefers real usage from Gen Live / chat; otherwise GET /api/session.
    /// Hidden when neither source reported counts.
    /// </summary>
    public sealed class SessionTokenMeter : MonoBehaviour
    {
        public TMP_Text label;
        public CanvasGroup group;

        LlmTokenUsage local;
        LlmTokenUsage api;
        bool visitActive;

        static readonly Color Ink = new Color(0.55f, 0.08f, 0.08f, 1f);
        static readonly Color Glass = new Color(0.04f, 0.03f, 0.03f, 0.9f);

        public LlmTokenUsage Snapshot()
        {
            return LlmTokenUsage.PreferLocal(local, api);
        }

        public void BeginVisit()
        {
            local = default;
            api = default;
            visitActive = true;
            Hide();
        }

        public void NoteApi(LlmTokenUsage usage)
        {
            if (usage.HasCounts)
                api = usage;
        }

        public void NoteLocal(LlmTokenUsage usage, bool liveCumulative)
        {
            if (!usage.HasCounts)
                return;
            if (liveCumulative)
                local.ObserveLive(usage);
            else
                local.Add(usage);
        }

        public void ShowEndOfVisit()
        {
            if (!visitActive)
            {
                Hide();
                return;
            }

            visitActive = false;
            var snap = Snapshot();
            if (!snap.HasCounts)
            {
                Debug.Log("[GenJutsu] Sessão encerrada sem contagem de tokens (nem uso local nem API).");
                Hide();
                return;
            }

            EnsureVisual();
            var line = snap.FormatPtBr();
            if (label != null)
                label.text = line;
            if (group != null)
                group.alpha = 1f;
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = true;
            Debug.Log("[GenJutsu] " + line);
        }

        public void Hide()
        {
            if (group != null)
                group.alpha = 0f;
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = false;
        }

        public void EnsureVisual()
        {
            if (label != null)
                return;

            if (!gameObject.TryGetComponent<Canvas>(out var canvas))
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (!TryGetComponent<CanvasScaler>(out var scaler))
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 8f;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(780f, 72f);
            if (transform.localScale == Vector3.one)
                transform.localScale = Vector3.one * 0.0011f;

            if (!gameObject.TryGetComponent<CanvasGroup>(out group))
                group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;
            canvas.enabled = false;

            Quad(rect, "Background", Glass);
            label = Text(rect, "Line", "", 28f, Ink);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Midline;
            label.textWrappingMode = TextWrappingModes.Normal;
        }

        static Image Quad(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Text(RectTransform parent, string name, string value, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(18f, 8f);
            r.offsetMax = new Vector2(-18f, -8f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
        }
    }
}
