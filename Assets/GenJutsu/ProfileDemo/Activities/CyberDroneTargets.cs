using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Throw the light discs at the orbiting drones. Score: hits, with accuracy as detail.
    /// </summary>
    public class CyberDroneTargets : ProfileActivity
    {
        public Transform orbitCenter;
        public Transform[] drones;
        public float[] orbitRadius = { 0.7f, 1.0f, 1.3f };
        public float[] orbitSpeed = { 0.6f, -0.45f, 0.35f };
        public ResettablePose[] discs;
        public float hitRadius = 0.17f;

        Vector3[] previous;
        bool[] live;
        XRGrabInteractable[] grabs;
        float[] droneCooldown;
        int throws;
        int hits;

        void Awake()
        {
            previous = new Vector3[discs.Length];
            live = new bool[discs.Length];
            grabs = new XRGrabInteractable[discs.Length];
            droneCooldown = new float[drones.Length];
            for (var i = 0; i < discs.Length; i++)
                grabs[i] = discs[i].GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            for (var i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null)
                    grabs[i].selectExited.AddListener(OnReleased);
            }
        }

        void OnDisable()
        {
            for (var i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null)
                    grabs[i].selectExited.RemoveListener(OnReleased);
            }
        }

        public override void ResetActivity()
        {
            throws = 0;
            hits = 0;
            for (var i = 0; i < live.Length; i++)
                live[i] = false;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (args.isCanceled)
                return;
            for (var i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != (Object)args.interactableObject)
                    continue;
                throws++;
                live[i] = true;
                previous[i] = discs[i].transform.position;
            }
        }

        void Update()
        {
            var t = Time.time;
            for (var d = 0; d < drones.Length; d++)
            {
                droneCooldown[d] -= Time.deltaTime;
                var radius = orbitRadius[d % orbitRadius.Length];
                var angle = t * orbitSpeed[d % orbitSpeed.Length] + d * 2.1f;
                var height = Mathf.Sin(t * 0.8f + d) * 0.25f;
                drones[d].position = orbitCenter.position + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                drones[d].rotation = Quaternion.Euler(0f, t * 90f + d * 40f, 0f);
            }

            for (var i = 0; i < discs.Length; i++)
            {
                var current = discs[i].transform.position;
                if (!live[i] || discs[i].IsHeld || discs[i].IsFlying)
                {
                    previous[i] = current;
                    continue;
                }

                var body = discs[i].Body;
                if (body != null && !body.isKinematic && body.linearVelocity.sqrMagnitude < 0.04f && (current - discs[i].HomePosition).sqrMagnitude > 0.01f)
                    live[i] = false;

                for (var d = 0; d < drones.Length && live[i]; d++)
                {
                    if (droneCooldown[d] > 0f)
                        continue;
                    if (SegmentDistance(previous[i], current, drones[d].position) > hitRadius)
                        continue;
                    Hit(i, d);
                }

                previous[i] = current;
            }
        }

        void Hit(int disc, int drone)
        {
            live[disc] = false;
            droneCooldown[drone] = 0.8f;
            hits++;
            var accuracy = throws > 0 ? Mathf.RoundToInt(100f * hits / throws) : 100;
            Report(hits, "acertos", true, hits + "/" + throws + " (" + accuracy + "%)");
            Popup(drones[drone].position + Vector3.up * 0.2f, "Alvo +1");
            DemoSfx.Play(DemoSfx.Kind.Pop, drones[drone].position, 0.9f, 1.4f);
            StartCoroutine(Pulse(drones[drone]));
            discs[disc].ReturnHome();
        }

        static IEnumerator Pulse(Transform drone)
        {
            var t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                drone.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.6f);
                yield return null;
            }

            drone.localScale = Vector3.one;
        }

        static float SegmentDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 1e-6f)
                return Vector3.Distance(a, p);
            var u = Mathf.Clamp01(Vector3.Dot(p - a, ab) / lengthSq);
            return Vector3.Distance(a + ab * u, p);
        }
    }
}
