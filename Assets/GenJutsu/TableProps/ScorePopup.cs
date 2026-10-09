using System.Collections;
using UnityEngine;

namespace GenJutsu.TableProps
{
    public class ScorePopup : MonoBehaviour
    {
        static AudioClip pingClip;

        [SerializeField] float lifetime = 1f;
        [SerializeField] float riseHeight = 0.28f;
        [SerializeField] float startScale = 1.2f;
        [SerializeField] float peakScale = 2.4f;

        TextMesh textMesh;
        Renderer textRenderer;
        float startY;
        Color baseColor;

        public static void Spawn(Vector3 worldPos, string text, Color color)
        {
            var go = new GameObject("ScorePopup");
            go.transform.position = worldPos;
            var popup = go.AddComponent<ScorePopup>();
            popup.Setup(text, color);
        }

        void Setup(string text, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = font;
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.012f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            textRenderer = GetComponent<Renderer>();
            if (font != null)
                textRenderer.sharedMaterial = font.material;

            baseColor = color;
            startY = transform.position.y;
            transform.localScale = Vector3.one * startScale;

            var audio = gameObject.AddComponent<AudioSource>();
            audio.clip = PingClip();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.volume = 0.7f;
            audio.minDistance = 0.5f;
            audio.maxDistance = 8f;
            audio.Play();

            StartCoroutine(AnimateRoutine());
        }

        IEnumerator AnimateRoutine()
        {
            var t = 0f;
            while (t < lifetime)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / lifetime);

                var pos = transform.position;
                pos.y = startY + riseHeight * (1f - (1f - u) * (1f - u));
                transform.position = pos;

                float scale;
                if (u < 0.22f)
                {
                    var k = u / 0.22f;
                    scale = Mathf.Lerp(startScale, peakScale, 1f - (1f - k) * (1f - k));
                }
                else if (u < 0.45f)
                {
                    var k = (u - 0.22f) / 0.23f;
                    scale = Mathf.Lerp(peakScale, 1f, k);
                }
                else
                {
                    scale = 1f;
                }
                transform.localScale = Vector3.one * scale;

                var alpha = u < 0.62f ? 1f : 1f - (u - 0.62f) / 0.38f;
                textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                yield return null;
            }
            Destroy(gameObject);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        static AudioClip PingClip()
        {
            if (pingClip != null)
                return pingClip;

            const int rate = 44100;
            const float duration = 0.18f;
            var samples = Mathf.CeilToInt(rate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)rate;
                var attack = Mathf.Min(1f, t / 0.004f);
                var env = Mathf.Exp(-t * 24f) * attack;
                var s = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.65f
                      + Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.35f;
                data[i] = s * env * 0.5f;
            }
            pingClip = AudioClip.Create("HitPing", samples, 1, rate, false);
            pingClip.SetData(data, 0);
            return pingClip;
        }
    }
}
