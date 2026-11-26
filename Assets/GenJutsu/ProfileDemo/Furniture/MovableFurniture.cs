using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    public sealed class MovableFurniture : MonoBehaviour
    {
        public string FurnitureId;
        public string DisplayName;
        public Vector3 HomeLocal;
        public Quaternion HomeRot;
        Rigidbody body;
        XRGrabInteractable grab;
        bool grabWasEnabled;

        public void CaptureHome()
        {
            HomeLocal = transform.localPosition;
            HomeRot = transform.localRotation;
        }

        public void BeginDirectedMove()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grabWasEnabled = grab.enabled;
                grab.enabled = false;
            }

            if (body == null)
                return;
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void SettleAfterMove()
        {
            var rest = GetComponent<GrabRest>();
            if (rest != null)
            {
                rest.Park();
                if (grab != null)
                    grab.enabled = grabWasEnabled;
                return;
            }

            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (grab != null)
                grab.enabled = grabWasEnabled;
        }
    }
}
