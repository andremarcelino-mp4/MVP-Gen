using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Profile object on the table: grabbing it opens its house, releasing it floats it back to its slot.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ProfileToken : MonoBehaviour
    {
        public ProfileId profile;
        [Tooltip("Product flow never opens a house from the token. Leave false on device builds.")]
        public bool opensHouse;
        public float returnDuration = 0.8f;
        public float idleSpin = 16f;
        public float idleBob = 0.006f;

        XRGrabInteractable grab;
        Rigidbody body;
        Vector3 homePosition;
        Quaternion homeRotation;
        bool atHome = true;
        Coroutine returning;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            if (returning != null)
            {
                StopCoroutine(returning);
                returning = null;
            }

            atHome = false;
            DemoSfx.Play(DemoSfx.Kind.Chime, transform.position, 0.8f, 1f + (int)profile * 0.12f);
            if (!opensHouse)
                return;
            var director = ProfileDemoDirector.Instance;
            if (director != null)
                director.EnterProfile(profile);
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (isActiveAndEnabled)
                returning = StartCoroutine(ReturnRoutine());
        }

        IEnumerator ReturnRoutine()
        {
            yield return null;
            if (body != null)
                body.isKinematic = true;

            var startPos = transform.position;
            var startRot = transform.rotation;
            var t = 0f;
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                var u = Mathf.SmoothStep(0f, 1f, t / returnDuration);
                transform.SetPositionAndRotation(Vector3.Lerp(startPos, homePosition, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 0.08f),
                    Quaternion.Slerp(startRot, homeRotation, u));
                yield return null;
            }

            transform.SetPositionAndRotation(homePosition, homeRotation);
            atHome = true;
            returning = null;
        }

        void Update()
        {
            if (!atHome || grab.isSelected)
                return;
            var t = Time.time + (int)profile * 0.7f;
            transform.SetPositionAndRotation(homePosition + Vector3.up * (Mathf.Sin(t * 1.4f) * idleBob),
                homeRotation * Quaternion.Euler(0f, t * idleSpin, 0f));
        }
    }
}
