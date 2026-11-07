using GenJutsu.GenCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Small status line near the head. It is not a profile card; the welcome card stays as it is.
    /// </summary>
    public sealed class GenStatusView : MonoBehaviour
    {
        public bool FollowHead = true;
        public TMP_Text phaseLabel;
        public TMP_Text caption;
        Image background;
        RectTransform meterFill;

        public void EnsureVisual()
        {
            if (phaseLabel != null)
                return;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(820f, 280f);
            transform.localScale = Vector3.one * 0.00125f;

            var group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bg = (RectTransform)bgGo.transform;
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background = bgGo.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.12f, 0.88f);
            background.raycastTarget = false;

            phaseLabel = Text(rect, "Phase", GenVoicePhases.Label(GenVoicePhase.Idle), 42f,
                new Color(0.96f, 0.94f, 0.88f),
                new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(28f, 0f), new Vector2(-28f, -12f));
            phaseLabel.fontStyle = FontStyles.Bold;
            caption = Text(rect, "Caption", "", 26f,
                new Color(0.86f, 0.88f, 0.9f),
                new Vector2(0f, 0.28f), new Vector2(1f, 0.55f), new Vector2(28f, 0f), new Vector2(-28f, 0f));
            caption.textWrappingMode = TextWrappingModes.Normal;

            var trackGo = new GameObject("MicTrack", typeof(RectTransform));
            trackGo.transform.SetParent(transform, false);
            var track = (RectTransform)trackGo.transform;
            Stretch(track, new Vector2(0f, 0f), new Vector2(1f, 0.26f), new Vector2(28f, 18f), new Vector2(-28f, -8f));
            var trackImage = trackGo.AddComponent<Image>();
            trackImage.color = new Color(0f, 0f, 0f, 0.45f);
            trackImage.raycastTarget = false;

            var fillGo = new GameObject("MicLevel", typeof(RectTransform));
            fillGo.transform.SetParent(track, false);
            meterFill = (RectTransform)fillGo.transform;
            Stretch(meterFill, Vector2.zero, new Vector2(0.04f, 1f), Vector2.zero, Vector2.zero);
            var fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.25f, 0.85f, 0.4f, 1f);
            fill.raycastTarget = false;
            trackGo.SetActive(false);
        }

        public void Show(GenVoicePhase phase, string reply)
        {
            EnsureVisual();
            var listening = phase == GenVoicePhase.Listening;
            if (phaseLabel != null)
                phaseLabel.text = listening ? "OUVINDO" : GenVoicePhases.Label(phase);
            if (caption != null)
                caption.text = listening && string.IsNullOrEmpty(reply) ? "Segure B e fale" : (reply ?? "");
            if (background != null)
                background.color = listening
                    ? new Color(0.05f, 0.28f, 0.1f, 0.94f)
                    : new Color(0.08f, 0.09f, 0.12f, 0.88f);
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
            var fill = meterFill.GetComponent<Image>();
            if (fill != null)
                fill.color = Color.Lerp(new Color(0.2f, 0.8f, 0.35f), new Color(0.95f, 0.72f, 0.15f), level);
        }

        void LateUpdate()
        {
            if (!FollowHead)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;
            var head = cam.transform;
            var pos = head.position + head.forward * 1.15f - head.up * 0.48f;
            transform.position = pos;
            var toCam = head.position - pos;
            if (toCam.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-toCam, head.up);
        }

        static TMP_Text Text(RectTransform parent, string name, string value, float size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
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
