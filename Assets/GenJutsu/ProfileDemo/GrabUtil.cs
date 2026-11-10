using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace GenJutsu.ProfileDemo
{
    public static class GrabUtil
    {
        public static XRGrabInteractable Make(GameObject go, float mass)
        {
            if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
                go.AddComponent<BoxCollider>();
            var body = go.GetComponent<Rigidbody>();
            if (body == null)
                body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.sleepThreshold = 0.02f;
            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
                grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = mass < 4f;
            grab.useDynamicAttach = true;
            if (go.GetComponent<XRGeneralGrabTransformer>() == null)
                go.AddComponent<XRGeneralGrabTransformer>();
            var rest = go.GetComponent<GrabRest>();
            if (rest == null)
                rest = go.AddComponent<GrabRest>();
            rest.Bind(grab, body);
            if (go.GetComponent<BoundsReset>() == null)
                go.AddComponent<BoundsReset>().Capture();
            if (go.GetComponent<ResettablePose>() == null)
                go.AddComponent<ResettablePose>();
            if (go.GetComponent<GrabHaptics>() == null)
                go.AddComponent<GrabHaptics>();
            go.isStatic = false;
            return grab;
        }

        public static AudioClip Click(int hz, float seconds)
        {
            const int rate = 22050;
            var n = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)rate;
                data[i] = Mathf.Exp(-t * 18f) * 0.28f * Mathf.Sin(2f * Mathf.PI * hz * t);
            }

            var clip = AudioClip.Create("Click" + hz, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

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
                    reset.Capture();
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
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.Sleep();
        }

        void Wake()
        {
            if (body == null)
                return;
            body.isKinematic = false;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.WakeUp();
        }

        void Release()
        {
            if (body == null)
                return;
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    public sealed class BoundsReset : MonoBehaviour
    {
        Vector3 homeLocal;
        Quaternion homeRot;
        Transform homeParent;
        Rigidbody body;

        public void Capture()
        {
            homeParent = transform.parent;
            homeLocal = transform.localPosition;
            homeRot = transform.localRotation;
            body = GetComponent<Rigidbody>();
        }

        public void Restore()
        {
            if (homeParent != null)
                transform.SetParent(homeParent, false);
            transform.localPosition = homeLocal;
            transform.localRotation = homeRot;
            var rest = GetComponent<GrabRest>();
            if (rest != null)
                rest.Park();
            else if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        void LateUpdate()
        {
            if (transform.localPosition.y > -0.4f && transform.position.y < 4f)
                return;
            Restore();
        }
    }
}
