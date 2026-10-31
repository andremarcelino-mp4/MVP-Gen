using System.Collections;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Feed logs into the fireplace to keep it bright. Score: seconds the fire stayed strong.
    /// </summary>
    public class Fireplace : ProfileActivity
    {
        public Transform hearth;
        public Vector3 hearthHalfExtents = new Vector3(0.45f, 0.35f, 0.25f);
        public ParticleSystem fire;
        public Light fireLight;
        public ResettablePose[] logs;
        public float startFuel = 50f;
        public float burnRate = 1.5f;
        public float fuelPerLog = 30f;
        public float strongThreshold = 60f;

        float fuel;
        float strongSeconds;
        float nextReport;
        float crackleTimer;
        bool[] burning;
        Vector3[] logScales;

        void Awake()
        {
            burning = new bool[logs.Length];
            logScales = new Vector3[logs.Length];
            for (var i = 0; i < logs.Length; i++)
                logScales[i] = logs[i].transform.localScale;
        }

        public override void ResetActivity()
        {
            StopAllCoroutines();
            fuel = startFuel;
            strongSeconds = 0f;
            nextReport = 5f;
            for (var i = 0; i < logs.Length; i++)
            {
                burning[i] = false;
                logs[i].transform.localScale = logScales[i];
                logs[i].gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (burning == null)
                return;
            for (var i = 0; i < logs.Length; i++)
            {
                burning[i] = false;
                logs[i].transform.localScale = logScales[i];
                logs[i].gameObject.SetActive(true);
            }
        }

        void Update()
        {
            fuel = Mathf.Max(0f, fuel - burnRate * Time.deltaTime);

            for (var i = 0; i < logs.Length; i++)
            {
                if (burning[i] || logs[i].IsHeld || logs[i].IsFlying || !logs[i].gameObject.activeSelf)
                    continue;
                var local = hearth.InverseTransformPoint(logs[i].transform.position);
                if (Mathf.Abs(local.x) > hearthHalfExtents.x || Mathf.Abs(local.y) > hearthHalfExtents.y || Mathf.Abs(local.z) > hearthHalfExtents.z)
                    continue;
                StartCoroutine(Burn(i));
            }

            var f = fuel / 100f;
            if (fire != null)
            {
                var emission = fire.emission;
                emission.rateOverTime = Mathf.Lerp(3f, 45f, f);
                var main = fire.main;
                main.startSize = Mathf.Lerp(0.08f, 0.28f, f);
            }

            if (fireLight != null)
                fireLight.intensity = Mathf.Lerp(0.25f, 2.4f, f) * (0.85f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.3f);

            crackleTimer -= Time.deltaTime;
            if (crackleTimer <= 0f)
            {
                crackleTimer = Random.Range(0.8f, 2.2f) / Mathf.Max(0.3f, f);
                if (fuel > 5f)
                    DemoSfx.Play(DemoSfx.Kind.Crackle, hearth.position, Mathf.Lerp(0.2f, 0.7f, f), Random.Range(0.8f, 1.2f));
            }

            if (fuel < strongThreshold)
                return;
            strongSeconds += Time.deltaTime;
            if (strongSeconds < nextReport)
                return;
            nextReport += 5f;
            var seconds = Mathf.Floor(strongSeconds);
            Report(seconds, "s de fogo forte");
            Popup(hearth.position + Vector3.up * 0.6f, "Lareira acesa " + seconds + " s");
        }

        IEnumerator Burn(int i)
        {
            burning[i] = true;
            var log = logs[i];
            fuel = Mathf.Min(100f, fuel + fuelPerLog);
            DemoSfx.Play(DemoSfx.Kind.Crackle, hearth.position, 0.9f, 0.8f);
            Popup(hearth.position + Vector3.up * 0.5f, "+ lenha");

            var t = 0f;
            while (t < 2.5f)
            {
                t += Time.deltaTime;
                log.transform.localScale = logScales[i] * Mathf.Lerp(1f, 0.2f, t / 2.5f);
                yield return null;
            }

            log.gameObject.SetActive(false);
            yield return new WaitForSeconds(3f);
            log.transform.localScale = logScales[i];
            log.gameObject.SetActive(true);
            log.ResetNow();
            burning[i] = false;
        }
    }
}
