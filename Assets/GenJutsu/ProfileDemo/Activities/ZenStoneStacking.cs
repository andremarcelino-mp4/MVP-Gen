using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Stack river stones on the sand disc. Score: tallest pile that stays still for a few seconds.
    /// </summary>
    public class ZenStoneStacking : ProfileActivity
    {
        public Transform baseTop;
        public float baseRadius = 0.22f;
        public ResettablePose[] stones;
        public float stableSeconds = 2f;

        Renderer[] renderers;
        float candidate;
        float stableTimer;
        float best;

        void Awake()
        {
            renderers = new Renderer[stones.Length];
            for (var i = 0; i < stones.Length; i++)
                renderers[i] = stones[i].GetComponentInChildren<Renderer>();
        }

        public override void ResetActivity()
        {
            candidate = 0f;
            stableTimer = 0f;
            best = 0f;
        }

        void Update()
        {
            var top = baseTop.position.y;
            var height = 0f;
            var count = 0;
            var topPoint = baseTop.position;
            for (var i = 0; i < stones.Length; i++)
            {
                var stone = stones[i];
                if (stone.IsHeld || stone.IsFlying || renderers[i] == null)
                    continue;
                var body = stone.Body;
                if (body != null && !body.isKinematic && body.linearVelocity.sqrMagnitude > 0.0025f)
                    continue;
                var bounds = renderers[i].bounds;
                var flat = new Vector2(bounds.center.x - baseTop.position.x, bounds.center.z - baseTop.position.z);
                if (flat.magnitude > baseRadius || bounds.min.y < top - 0.02f)
                    continue;
                count++;
                var h = bounds.max.y - top;
                if (h > height)
                {
                    height = h;
                    topPoint = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                }
            }

            if (Mathf.Abs(height - candidate) < 0.01f)
            {
                stableTimer += Time.deltaTime;
            }
            else
            {
                candidate = height;
                stableTimer = 0f;
            }

            if (count < 2 || stableTimer < stableSeconds || candidate < best + 0.01f)
                return;

            best = candidate;
            var cm = Mathf.Round(best * 100f);
            Report(cm, "cm", true, count + " pedras");
            Popup(topPoint + Vector3.up * 0.08f, "Pilha " + cm + " cm");
            DemoSfx.Play(DemoSfx.Kind.Success, topPoint, 0.6f, 0.9f);
        }
    }
}
