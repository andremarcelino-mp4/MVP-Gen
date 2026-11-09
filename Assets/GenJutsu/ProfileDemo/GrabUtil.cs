using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace GenJutsu.ProfileDemo
{
    public static class GrabUtil
    {
        public static XRGrabInteractable Make(GameObject go, float mass)
        {
            if (go.GetComponent<Collider>() == null)
                go.AddComponent<BoxCollider>();
            var body = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var grab = go.GetComponent<XRGrabInteractable>() ?? go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.useDynamicAttach = true;
            if (go.GetComponent<XRGeneralGrabTransformer>() == null)
                go.AddComponent<XRGeneralGrabTransformer>();
            if (go.GetComponent<BoundsReset>() == null)
                go.AddComponent<BoundsReset>().Capture();
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

    public sealed class BoundsReset : MonoBehaviour
    {
        Vector3 homePos;
        Quaternion homeRot;
        Rigidbody body;

        public void Capture()
        {
            homePos = transform.position;
            homeRot = transform.rotation;
            body = GetComponent<Rigidbody>();
        }

        void LateUpdate()
        {
            if (transform.position.y > -0.4f && transform.position.y < 3.4f)
                return;
            transform.SetPositionAndRotation(homePos, homeRot);
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
