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

        public void EnsureVisual()
        {
            if (phaseLabel != null)
                return;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(760f, 220f);
            transform.localScale = Vector3.one * 0.00115f;

            var group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bg = (RectTransform)bgGo.transform;
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = bgGo.AddComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.12f, 0.82f);
            image.raycastTarget = false;

            phaseLabel = Text(rect, "Phase", GenVoicePhases.Label(GenVoicePhase.Idle), 34f,
                new Color(0.96f, 0.94f, 0.88f),
                new Vector2(0f, 0.48f), new Vector2(1f, 1f), new Vector2(28f, 0f), new Vector2(-28f, -16f));
            phaseLabel.fontStyle = FontStyles.Bold;
            caption = Text(rect, "Caption", "", 26f,
                new Color(0.86f, 0.88f, 0.9f),
                new Vector2(0f, 0f), new Vector2(1f, 0.52f), new Vector2(28f, 16f), new Vector2(-28f, 0f));
            caption.textWrappingMode = TextWrappingModes.Normal;
        }

        public void Show(GenVoicePhase phase, string reply)
        {
            EnsureVisual();
            if (phaseLabel != null)
                phaseLabel.text = GenVoicePhases.Label(phase);
            if (caption != null)
                caption.text = reply ?? "";
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
