using System.Collections;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Strike the singing bowls with the mallet. Score: strikes spaced at a calm breathing pace (3 s or more).
    /// </summary>
    public class ZenSingingBowls : ProfileActivity
    {
        public Transform[] bowls;
        public float bowlRadius = 0.13f;
        public float[] pitches = { 1f, 1.335f, 1.782f };
        public Transform[] ripples;
        public Transform malletHead;
        public float minStrikeSpeed = 0.35f;
        public float calmInterval = 3f;

        Vector3 previousHead;
        float[] cooldowns;
        float lastStrike = -100f;
        int calmStrikes;
        int streak;
        int bestStreak;

        void Awake()
        {
            cooldowns = new float[bowls.Length];
        }

        void OnEnable()
        {
            previousHead = malletHead.position;
        }

        public override void ResetActivity()
        {
            lastStrike = -100f;
            calmStrikes = 0;
            streak = 0;
            bestStreak = 0;
            previousHead = malletHead.position;
        }

        void Update()
        {
            var head = malletHead.position;
            var speed = Time.deltaTime > 0f ? (head - previousHead).magnitude / Time.deltaTime : 0f;
            previousHead = head;

            for (var i = 0; i < bowls.Length; i++)
            {
                cooldowns[i] -= Time.deltaTime;
                if (cooldowns[i] > 0f || speed < minStrikeSpeed)
                    continue;
                if (Vector3.Distance(head, bowls[i].position) > bowlRadius + 0.03f)
                    continue;
                cooldowns[i] = 0.5f;
                Strike(i, speed);
            }
        }

        void Strike(int i, float speed)
        {
            var volume = Mathf.Clamp(speed / 1.5f, 0.35f, 1f);
            DemoSfx.Play(DemoSfx.Kind.Bowl, bowls[i].position, volume, pitches[i % pitches.Length]);
            if (ripples != null && i < ripples.Length && ripples[i] != null)
                StartCoroutine(Ripple(ripples[i]));

            var interval = Time.time - lastStrike;
            lastStrike = Time.time;
            if (interval >= calmInterval)
            {
                calmStrikes++;
                streak++;
                bestStreak = Mathf.Max(bestStreak, streak);
                Report(calmStrikes, "toques calmos", true, "melhor sequência " + bestStreak);
                Popup(bowls[i].position + Vector3.up * 0.25f, "Calma +1");
            }
            else
            {
                streak = 0;
            }
        }

        static IEnumerator Ripple(Transform ring)
        {
            var baseScale = ring.localScale;
            ring.gameObject.SetActive(true);
            var t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                var u = t / 1.4f;
                ring.localScale = new Vector3(baseScale.x * (1f + u * 1.6f), baseScale.y, baseScale.z * (1f + u * 1.6f));
                yield return null;
            }

            ring.localScale = baseScale;
            ring.gameObject.SetActive(false);
        }
    }
}
