using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Magnetic shapes stick together on contact. Score: largest connected sculpture; big sculptures are photographed.
    /// </summary>
    public class MagnetSculpture : ProfileActivity
    {
        public Rigidbody[] shapes;
        public XRSimpleInteractable releaseButton;
        public float breakForce = 600f;
        public int maxJoints = 24;

        readonly List<FixedJoint> joints = new List<FixedJoint>();
        readonly HashSet<long> pairs = new HashSet<long>();
        CollisionRelay[] relays;
        int best;
        int photographedSize;

        void Awake()
        {
            relays = new CollisionRelay[shapes.Length];
            for (var i = 0; i < shapes.Length; i++)
            {
                relays[i] = shapes[i].GetComponent<CollisionRelay>();
                if (relays[i] == null)
                    relays[i] = shapes[i].gameObject.AddComponent<CollisionRelay>();
            }
        }

        void OnEnable()
        {
            foreach (var relay in relays)
                relay.Collided += OnContact;
            if (releaseButton != null)
                releaseButton.selectEntered.AddListener(OnRelease);
        }

        void OnDisable()
        {
            foreach (var relay in relays)
                relay.Collided -= OnContact;
            if (releaseButton != null)
                releaseButton.selectEntered.RemoveListener(OnRelease);
        }

        public override void ResetActivity()
        {
            BreakAll();
            best = 0;
            photographedSize = 0;
        }

        void OnRelease(SelectEnterEventArgs args)
        {
            if (joints.Count == 0)
                return;
            BreakAll();
            DemoSfx.Play(DemoSfx.Kind.Whoosh, releaseButton.transform.position, 0.5f, 1.4f);
            Popup(releaseButton.transform.position + Vector3.up * 0.2f, "Peças soltas");
        }

        void BreakAll()
        {
            foreach (var joint in joints)
            {
                if (joint != null)
                    Destroy(joint);
            }

            joints.Clear();
            pairs.Clear();
        }

        void OnContact(CollisionRelay relay, Collision collision)
        {
            var a = System.Array.IndexOf(relays, relay);
            var b = collision.rigidbody != null ? System.Array.IndexOf(shapes, collision.rigidbody) : -1;
            if (a < 0 || b < 0 || a == b)
                return;
            PruneBroken();
            var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (pairs.Contains(key) || joints.Count >= maxJoints)
                return;

            var joint = shapes[a].gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = shapes[b];
            joint.breakForce = breakForce;
            joint.breakTorque = breakForce;
            joints.Add(joint);
            pairs.Add(key);

            var contact = collision.contactCount > 0 ? collision.GetContact(0).point : shapes[a].position;
            DemoSfx.Play(DemoSfx.Kind.Pop, contact, 0.7f, DemoSfx.Semitones(Random.Range(-3, 8)));

            var largest = LargestGroup();
            if (largest <= best)
                return;
            best = largest;
            Report(best, "peças unidas");
            Popup(contact + Vector3.up * 0.15f, "Escultura " + best);
            if (best >= 3 && best >= photographedSize + 2)
            {
                photographedSize = best;
                SaveView("escultura_" + best);
            }
        }

        void PruneBroken()
        {
            for (var i = joints.Count - 1; i >= 0; i--)
            {
                if (joints[i] != null)
                    continue;
                joints.RemoveAt(i);
            }

            if (joints.Count == pairs.Count)
                return;
            pairs.Clear();
            foreach (var joint in joints)
            {
                var a = System.Array.IndexOf(shapes, joint.GetComponent<Rigidbody>());
                var b = System.Array.IndexOf(shapes, joint.connectedBody);
                if (a >= 0 && b >= 0)
                    pairs.Add(a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a);
            }
        }

        int LargestGroup()
        {
            var parent = new int[shapes.Length];
            for (var i = 0; i < parent.Length; i++)
                parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x)
                    x = parent[x] = parent[parent[x]];
                return x;
            }

            foreach (var key in pairs)
            {
                var a = Find((int)(key >> 32));
                var b = Find((int)(key & 0xffffffff));
                if (a != b)
                    parent[a] = b;
            }

            var counts = new Dictionary<int, int>();
            var largest = 1;
            for (var i = 0; i < parent.Length; i++)
            {
                var root = Find(i);
                counts.TryGetValue(root, out var c);
                counts[root] = ++c;
                largest = Mathf.Max(largest, c);
            }

            return largest;
        }
    }
}
