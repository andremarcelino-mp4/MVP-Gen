using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Throw paint balls at the canvas. Score: splats and colours used. The painting is saved as an image.
    /// </summary>
    public class PaintSplatWall : ProfileActivity
    {
        public Renderer canvasRenderer;
        [Tooltip("Centre of the painted face, unscaled, with +Z pointing into the wall.")]
        public Transform canvasFace;
        public Vector2 canvasSize = new Vector2(2f, 1f);
        public ResettablePose[] balls;
        public Color[] ballColors;
        public float ballRadius = 0.05f;

        PaintableTexture paint;
        Vector3[] previousLocal;
        float[] lastSplat;
        CollisionRelay[] relays;
        readonly HashSet<int> colorsUsed = new HashSet<int>();
        int splats;
        float nextApply;

        void Awake()
        {
            paint = new PaintableTexture(512, 256, "PaintWall", (x, y) =>
            {
                var grain = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.04f;
                return new Color(0.97f - grain, 0.96f - grain, 0.93f - grain);
            });
            var material = canvasRenderer.material;
            material.mainTexture = paint.Texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", paint.Texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            previousLocal = new Vector3[balls.Length];
            lastSplat = new float[balls.Length];
            relays = new CollisionRelay[balls.Length];
            for (var i = 0; i < balls.Length; i++)
            {
                relays[i] = balls[i].GetComponent<CollisionRelay>();
                if (relays[i] == null)
                    relays[i] = balls[i].gameObject.AddComponent<CollisionRelay>();
            }
        }

        void OnEnable()
        {
            foreach (var relay in relays)
                relay.Collided += OnBallCollided;
        }

        void OnDisable()
        {
            foreach (var relay in relays)
                relay.Collided -= OnBallCollided;
        }

        // Fast throws can bounce off the wall between two frames, so contacts also count as hits.
        void OnBallCollided(CollisionRelay relay, Collision collision)
        {
            var index = System.Array.IndexOf(relays, relay);
            if (index < 0 || balls[index].IsHeld || collision.contactCount == 0)
                return;
            var local = canvasFace.InverseTransformPoint(collision.GetContact(0).point);
            if (Mathf.Abs(local.z) > ballRadius + 0.06f)
                return;
            if (Mathf.Abs(local.x) > canvasSize.x * 0.5f || Mathf.Abs(local.y) > canvasSize.y * 0.5f)
                return;
            Splat(index, new Vector2(local.x / canvasSize.x + 0.5f, local.y / canvasSize.y + 0.5f));
        }

        public override void ResetActivity()
        {
            paint.Clear();
            colorsUsed.Clear();
            splats = 0;
        }

        public override void OnHouseExit()
        {
            if (splats > 0)
                SaveTexture(paint.Texture, "tela_pintura");
        }

        void Update()
        {
            for (var i = 0; i < balls.Length; i++)
            {
                var ball = balls[i];
                var local = canvasFace.InverseTransformPoint(ball.transform.position);
                var before = previousLocal[i];
                previousLocal[i] = local;
                if (ball.IsHeld || ball.IsFlying)
                    continue;
                var reach = ballRadius + 0.02f;
                if (-local.z > reach || -before.z <= reach)
                    continue;
                if (Mathf.Abs(local.x) > canvasSize.x * 0.5f || Mathf.Abs(local.y) > canvasSize.y * 0.5f)
                    continue;
                Splat(i, new Vector2(local.x / canvasSize.x + 0.5f, local.y / canvasSize.y + 0.5f));
            }

            if (Time.time >= nextApply)
            {
                nextApply = Time.time + 0.05f;
                paint.ApplyIfDirty();
            }
        }

        void Splat(int ballIndex, Vector2 uv)
        {
            if (Time.time - lastSplat[ballIndex] < 0.3f)
                return;
            lastSplat[ballIndex] = Time.time;
            var color = ballColors[ballIndex % ballColors.Length];
            var body = balls[ballIndex].Body;
            var speed = body != null && !body.isKinematic ? body.linearVelocity.magnitude : 2f;
            var size = Mathf.Lerp(14f, 30f, Mathf.InverseLerp(1f, 6f, speed));
            paint.Stamp(uv, size, color, 1f);
            var droplets = Random.Range(5, 10);
            for (var d = 0; d < droplets; d++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var distance = size * Random.Range(1.1f, 2.4f);
                var offset = new Vector2(direction.x * distance / paint.Width, direction.y * distance / paint.Height);
                paint.Stamp(uv + offset, size * Random.Range(0.12f, 0.35f), color, 1f);
                paint.Line(uv, uv + offset * 0.8f, size * 0.12f, color, 0.9f);
            }

            paint.ApplyIfDirty();
            splats++;
            colorsUsed.Add(ballIndex % ballColors.Length);
            Report(splats, "respingos", true, colorsUsed.Count + " cores");
            var world = canvasFace.TransformPoint(new Vector3((uv.x - 0.5f) * canvasSize.x, (uv.y - 0.5f) * canvasSize.y, -0.05f));
            DemoSfx.Play(DemoSfx.Kind.Splat, world, 0.9f, Random.Range(0.85f, 1.2f));
            if (splats % 3 == 1)
                Popup(world + Vector3.up * 0.15f, "Splash! " + splats);
            balls[ballIndex].ReturnHome();
        }
    }
}
