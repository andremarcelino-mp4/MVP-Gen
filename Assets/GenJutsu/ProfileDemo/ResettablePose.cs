using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Remembers where a house prop starts and brings it back when dropped on the floor or after a throw.
    /// </summary>
    public class ResettablePose : MonoBehaviour
    {
        public float floorReturnHeight = 0.12f;
        public float floorReturnDelay = 2.5f;
        public float autoReturnDelay;
        public float flightDuration = 0.6f;

        static readonly System.Collections.Generic.List<ResettablePose> all = new System.Collections.Generic.List<ResettablePose>();

        XRGrabInteractable grab;
        Rigidbody body;
        Transform homeParent;
        Vector3 homePosition;
        Quaternion homeRotation;
        bool captured;
        bool homeKinematic;
        float awayTimer;
        float floorTimer;
        Coroutine flight;

        public bool IsHeld => grab != null && grab.isSelected;
        public bool IsFlying => flight != null;
        public Rigidbody Body => body;
        public Vector3 HomePosition => homePosition;

        /// <summary>Every pose whose home is under <paramref name="root"/>, including props currently held outside it.</summary>
        public static System.Collections.Generic.List<ResettablePose> OwnedBy(Transform root)
        {
            var owned = new System.Collections.Generic.List<ResettablePose>();
            foreach (var pose in all)
            {
                if (pose != null && pose.homeParent != null && pose.homeParent.IsChildOf(root))
                    owned.Add(pose);
            }

            return owned;
        }

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            all.Add(this);
            Capture();
        }

        void OnDestroy()
        {
            all.Remove(this);
        }

        public void Capture()
        {
            homeParent = transform.parent;
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homeKinematic = body != null && body.isKinematic;
            captured = true;
        }

        void Update()
        {
            if (!captured || IsHeld || flight != null)
            {
                awayTimer = 0f;
                floorTimer = 0f;
                return;
            }

            if (transform.position.y < floorReturnHeight)
            {
                floorTimer += Time.deltaTime;
                if (floorTimer >= floorReturnDelay)
                    ReturnHome();
            }
            else
            {
                floorTimer = 0f;
            }

            if (autoReturnDelay <= 0f)
                return;
            if ((transform.position - homePosition).sqrMagnitude < 0.0025f)
            {
                awayTimer = 0f;
                return;
            }

            awayTimer += Time.deltaTime;
            if (awayTimer >= autoReturnDelay)
                ReturnHome();
        }

        public void ResetNow()
        {
            if (!captured)
                return;
            if (flight != null)
            {
                StopCoroutine(flight);
                flight = null;
            }

            ForceRelease();
            if (transform.parent != homeParent)
                transform.SetParent(homeParent, true);
            transform.SetPositionAndRotation(homePosition, homeRotation);
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = homeKinematic;
                body.position = homePosition;
                body.rotation = homeRotation;
            }

            awayTimer = 0f;
            floorTimer = 0f;
        }

        public void ReturnHome()
        {
            if (!isActiveAndEnabled)
            {
                ResetNow();
                return;
            }

            ForceRelease();
            if (flight != null)
                StopCoroutine(flight);
            flight = StartCoroutine(FlightRoutine());
        }

        public void ForceRelease()
        {
            if (grab != null && grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }

        IEnumerator FlightRoutine()
        {
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
            }

            var startPos = transform.position;
            var startRot = transform.rotation;
            var t = 0f;
            while (t < flightDuration)
            {
                t += Time.deltaTime;
                var u = Mathf.SmoothStep(0f, 1f, t / flightDuration);
                var arc = Mathf.Sin(u * Mathf.PI) * 0.15f;
                transform.SetPositionAndRotation(Vector3.Lerp(startPos, homePosition, u) + Vector3.up * arc,
                    Quaternion.Slerp(startRot, homeRotation, u));
                yield return null;
            }

            flight = null;
            ResetNow();
        }

        void OnDisable()
        {
            if (flight != null)
            {
                StopCoroutine(flight);
                flight = null;
            }
        }
    }
}
