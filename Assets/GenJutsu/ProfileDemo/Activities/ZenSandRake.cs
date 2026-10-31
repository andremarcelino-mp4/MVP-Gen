using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Rake grooves into the sand garden. Score: how much of the garden was drawn. The garden is saved as an image.
    /// </summary>
    public class ZenSandRake : ProfileActivity
    {
        public Renderer sandRenderer;
        public Transform sandSurface;
        public Vector2 sandSize = new Vector2(1.1f, 0.7f);
        public Transform[] tines;
        public Color sandColor = new Color(0.86f, 0.8f, 0.68f);
        public Color grooveColor = new Color(0.62f, 0.55f, 0.43f);
        public float grooveRadius = 2.2f;

        PaintableTexture paint;
        Vector2[] previous;
        bool[] wasTouching;
        float nextApply;
        float reportedCoverage;

        void Awake()
        {
            var noiseSeed = Random.Range(0f, 100f);
            paint = new PaintableTexture(320, 200, "ZenSand", (x, y) =>
            {
                var n = Mathf.PerlinNoise(x * 0.11f + noiseSeed, y * 0.11f) * 0.08f + Mathf.PerlinNoise(x * 0.6f, y * 0.6f) * 0.04f;
                return sandColor * (0.94f + n);
            });
            var material = sandRenderer.material;
            material.mainTexture = paint.Texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", paint.Texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            previous = new Vector2[tines.Length];
            wasTouching = new bool[tines.Length];
        }

        public override void ResetActivity()
        {
            paint.Clear();
            reportedCoverage = 0f;
        }

        public override void OnHouseExit()
        {
            if (paint.Coverage > 0.01f)
                SaveTexture(paint.Texture, "jardim_areia");
        }

        void Update()
        {
            for (var i = 0; i < tines.Length; i++)
            {
                var local = sandSurface.InverseTransformPoint(tines[i].position);
                var touching = local.y < 0.035f && local.y > -0.05f && Mathf.Abs(local.x) < sandSize.x * 0.5f && Mathf.Abs(local.z) < sandSize.y * 0.5f;
                var uv = new Vector2(local.x / sandSize.x + 0.5f, local.z / sandSize.y + 0.5f);
                if (touching)
                {
                    var from = wasTouching[i] ? previous[i] : uv;
                    paint.Line(from, uv, grooveRadius * 1.6f, sandColor * 1.05f, 0.5f);
                    paint.Line(from, uv, grooveRadius, grooveColor, 0.8f);
                }

                previous[i] = uv;
                wasTouching[i] = touching;
            }

            if (Time.time >= nextApply)
            {
                nextApply = Time.time + 0.05f;
                paint.ApplyIfDirty();
            }

            var coverage = paint.Coverage;
            if (coverage < reportedCoverage + 0.05f)
                return;
            reportedCoverage = Mathf.Floor(coverage * 20f) / 20f;
            var percent = Mathf.Round(coverage * 100f);
            Report(percent, "% do jardim");
            Popup(sandSurface.position + Vector3.up * 0.25f, "Jardim " + percent + "%");
            DemoSfx.Play(DemoSfx.Kind.Chime, sandSurface.position, 0.3f, 0.7f);
        }
    }
}
