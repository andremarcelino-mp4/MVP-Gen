using UnityEngine;

namespace GenJutsu.ProfileDemo
{
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
