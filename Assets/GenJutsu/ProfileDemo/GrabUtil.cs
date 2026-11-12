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
}
