using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Tilt the watering can over the pots so the plants grow and bloom. Score: plants in bloom.
    /// </summary>
    public class PlantCare : ProfileActivity
    {
        public ResettablePose wateringCan;
        public Transform spout;
        public ParticleSystem water;
        public Transform[] pots;
        public Transform[] growParts;
        public GameObject[] flowers;
        public float waterNeeded = 3f;
        public float reach = 0.24f;

        float[] watered;
        bool[] bloomed;
        Vector3[] baseScales;
        int bloomCount;
        float sfxTimer;

        void Awake()
        {
            watered = new float[pots.Length];
            bloomed = new bool[pots.Length];
            baseScales = new Vector3[growParts.Length];
            for (var i = 0; i < growParts.Length; i++)
                baseScales[i] = growParts[i].localScale;
        }

        public override void ResetActivity()
        {
            bloomCount = 0;
            for (var i = 0; i < pots.Length; i++)
            {
                watered[i] = 0f;
                bloomed[i] = false;
                if (i < flowers.Length && flowers[i] != null)
                    flowers[i].SetActive(false);
                ApplyGrowth(i, 0f);
            }

            if (water != null)
                water.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void Update()
        {
            var pouring = Vector3.Dot(spout.forward, Vector3.down) > 0.35f && !wateringCan.IsFlying;
            if (water != null)
            {
                if (pouring && !water.isEmitting)
                    water.Play();
                else if (!pouring && water.isEmitting)
                    water.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (!pouring)
                return;

            sfxTimer -= Time.deltaTime;
            if (sfxTimer <= 0f)
            {
                sfxTimer = 0.45f;
                DemoSfx.Play(DemoSfx.Kind.Water, spout.position, 0.35f, Random.Range(0.9f, 1.1f));
            }

            var landing = spout.position + spout.forward * 0.12f;
            for (var i = 0; i < pots.Length; i++)
            {
                if (bloomed[i] || landing.y < pots[i].position.y)
                    continue;
                var flat = new Vector2(landing.x - pots[i].position.x, landing.z - pots[i].position.z);
                if (flat.magnitude > reach)
                    continue;
                watered[i] += Time.deltaTime;
                var growth = Mathf.Clamp01(watered[i] / waterNeeded);
                ApplyGrowth(i, growth);
                if (growth >= 1f)
                    Bloom(i);
            }
        }

        void ApplyGrowth(int i, float growth)
        {
            if (i >= growParts.Length || growParts[i] == null)
                return;
            growParts[i].localScale = baseScales[i] * Mathf.Lerp(0.35f, 1f, growth);
        }

        void Bloom(int i)
        {
            bloomed[i] = true;
            bloomCount++;
            if (i < flowers.Length && flowers[i] != null)
                flowers[i].SetActive(true);
            Report(bloomCount, "plantas floridas", true, bloomCount + "/" + pots.Length);
            Popup(pots[i].position + Vector3.up * 0.5f, "Floresceu!");
            DemoSfx.Play(DemoSfx.Kind.Success, pots[i].position, 0.6f, 1.2f);
        }
    }
}
