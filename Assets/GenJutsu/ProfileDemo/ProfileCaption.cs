using System.Collections;
using TMPro;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// World-space caption that appears in front of the user, follows the head gently and fades away.
    /// </summary>
    public class ProfileCaption : MonoBehaviour
    {
        public CanvasGroup group;
        public TMP_Text title;
        public TMP_Text body;
        public UnityEngine.UI.Image accentBar;
        public float distance = 1.6f;
        public float heightOffset = -0.12f;
        public float holdSeconds = 6f;
        public float fadeSeconds = 0.6f;
        public float followSharpness = 2.5f;

        Coroutine routine;

        void Awake()
        {
            if (group != null)
                group.alpha = 0f;
        }

        public void Show(string heading, string text, Color accent)
        {
            if (title != null)
                title.text = heading;
            if (body != null)
                body.text = text;
            if (accentBar != null)
                accentBar.color = accent;
            if (routine != null)
                StopCoroutine(routine);
            gameObject.SetActive(true);
            SnapToHead(1f);
            routine = StartCoroutine(ShowRoutine());
        }

        IEnumerator ShowRoutine()
        {
            yield return Fade(0f, 1f);
            var t = 0f;
            while (t < holdSeconds)
            {
                t += Time.deltaTime;
                SnapToHead(1f - Mathf.Exp(-followSharpness * Time.deltaTime));
                yield return null;
            }

            yield return Fade(1f, 0f);
            routine = null;
        }

        IEnumerator Fade(float from, float to)
        {
            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                if (group != null)
                    group.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                SnapToHead(1f - Mathf.Exp(-followSharpness * Time.deltaTime));
                yield return null;
            }

            if (group != null)
                group.alpha = to;
        }

        void SnapToHead(float blend)
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            var head = cam.transform;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.forward;
            forward.Normalize();
            var target = head.position + forward * distance + Vector3.up * heightOffset;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target, blend),
                Quaternion.Slerp(transform.rotation, rotation, blend));
        }
    }
}
