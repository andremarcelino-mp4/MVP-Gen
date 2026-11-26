using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    public sealed class GrabRest : MonoBehaviour
    {
        XRGrabInteractable grab;
        Rigidbody body;
        bool bound;

        public static void SettleUnder(Transform root)
        {
            if (root == null)
                return;
            var rest = root.GetComponentsInChildren<GrabRest>(true);
            for (var i = 0; i < rest.Length; i++)
            {
                rest[i].Park();
                var reset = rest[i].GetComponent<BoundsReset>();
                if (reset != null)
                    reset.CaptureIfMissing();
                var pose = rest[i].GetComponent<ResettablePose>();
                if (pose != null)
                    pose.Capture();
            }
        }

        public void Bind(XRGrabInteractable interactable, Rigidbody rigid)
        {
            grab = interactable;
            body = rigid;
            if (!bound && grab != null)
            {
                bound = true;
                grab.selectEntered.AddListener(_ => Wake());
                grab.selectExited.AddListener(_ => Release());
            }

            Park();
        }

        public void Park()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.None;
            if (grab != null && HouseGrabPhysics.IsPhysicalProp(grab))
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                return;
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.Sleep();
        }

        void Wake()
        {
            if (body == null)
                return;
            body.isKinematic = false;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.None;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.WakeUp();
        }

        void Release()
        {
            if (body == null)
                return;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.None;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.WakeUp();
        }
    }
}
