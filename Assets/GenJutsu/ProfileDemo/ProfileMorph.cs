using System;
using System.Collections;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Gradual white → look on the same shell: light → materials/env → props. Total ≤ ~2–3 s.
    /// </summary>
    public class ProfileMorph : MonoBehaviour
    {
        public float duration = 2.6f;
        public bool playWhoosh = true;

        public IEnumerator MorphIn(
            ProfileInfo info,
            HouseTransition look,
            Action<ProfileInfo, float> applyEnvironment,
            HubPropsDissolve hub,
            Vector3 origin)
        {
            var t = 0f;
            var span = Mathf.Clamp(duration, 0.4f, 3f);
            if (playWhoosh)
                DemoSfx.Play(DemoSfx.Kind.Whoosh, origin, 0.35f, 1f);

            Coroutine build = null;
            var propsStarted = false;
            var hubStarted = false;
            Coroutine hubCo = null;

            while (t < span)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / span);
                var lightU = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.38f, u));
                applyEnvironment?.Invoke(info, lightU);

                if (!hubStarted && u >= 0.05f && hub != null)
                {
                    hubStarted = true;
                    hubCo = StartCoroutine(hub.Dissolve());
                }

                if (!propsStarted && u >= 0.42f && look != null)
                {
                    propsStarted = true;
                    build = StartCoroutine(look.Build(origin));
                }

                yield return null;
            }

            applyEnvironment?.Invoke(info, 1f);
            if (build != null)
                yield return build;
            if (hubCo != null)
                yield return hubCo;
        }
    }
}
