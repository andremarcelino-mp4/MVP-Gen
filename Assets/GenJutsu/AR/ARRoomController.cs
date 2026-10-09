using System.Collections;
using UnityEngine;

namespace GenJutsu.AR
{
    public class ARRoomController : MonoBehaviour
    {
        [SerializeField] Renderer floorRenderer;
        [SerializeField] Renderer ceilingRenderer;
        [SerializeField] Material roomMaterialTemplate;
        [SerializeField] float targetAlpha = 0.85f;
        [SerializeField] float fadeDuration = 0.7f;

        public bool Visible { get; private set; }

        Material fadeMaterial;
        Coroutine fadeRoutine;

        void Awake()
        {
            if (roomMaterialTemplate != null)
            {
                fadeMaterial = new Material(roomMaterialTemplate);
                if (floorRenderer != null)
                    floorRenderer.sharedMaterial = fadeMaterial;
                if (ceilingRenderer != null)
                    ceilingRenderer.sharedMaterial = fadeMaterial;
            }
            ApplyAlpha(0f);
            SetRenderersActive(false);
        }

        public void Toggle()
        {
            if (Visible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            Visible = true;
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(targetAlpha));
        }

        public void Hide()
        {
            Visible = false;
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(0f));
        }

        public void ForceHide()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            Visible = false;
            ApplyAlpha(0f);
            SetRenderersActive(false);
        }

        IEnumerator FadeTo(float target)
        {
            SetRenderersActive(true);
            float start = CurrentAlpha();
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                ApplyAlpha(Mathf.Lerp(start, target, t / fadeDuration));
                yield return null;
            }
            ApplyAlpha(target);
            if (target <= 0.001f)
                SetRenderersActive(false);
            fadeRoutine = null;
        }

        float CurrentAlpha()
        {
            if (fadeMaterial == null)
                return 0f;
            var c = fadeMaterial.GetColor("_BaseColor");
            return c.a;
        }

        void ApplyAlpha(float alpha)
        {
            if (fadeMaterial == null)
                return;
            var c = fadeMaterial.GetColor("_BaseColor");
            c.a = alpha;
            fadeMaterial.SetColor("_BaseColor", c);
        }

        void SetRenderersActive(bool active)
        {
            if (floorRenderer != null)
                floorRenderer.gameObject.SetActive(active);
            if (ceilingRenderer != null)
                ceilingRenderer.gameObject.SetActive(active);
        }
    }
}
