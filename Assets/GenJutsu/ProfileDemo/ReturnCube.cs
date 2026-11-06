using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// White cube inside each house: grabbing it folds the house back into the white void.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ReturnCube : MonoBehaviour
    {
        public float idleSpin = 25f;
        public float idleBob = 0.02f;
        public float releaseDelay = 0.4f;

        XRGrabInteractable grab;
        Rigidbody body;
        Transform homeParent;
        Vector3 homePosition;
        Quaternion homeRotation;

        void Awake()
        {
            homeParent = transform.parent;
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
            transform.SetPositionAndRotation(homePosition, homeRotation);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            DemoSfx.Play(DemoSfx.Kind.Chime, transform.position, 0.9f, 0.75f);
            var director = ProfileDemoDirector.Instance;
            if (director != null)
                director.ReturnToWhiteSpace();
            StartCoroutine(ReleaseRoutine());
        }

        IEnumerator ReleaseRoutine()
        {
            yield return new WaitForSeconds(releaseDelay);
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            yield return null;
            if (body != null)
                body.isKinematic = true;
            // XRI will not re-parent onto the already deactivated house, so the cube would stay behind in the white space.
            if (transform.parent != homeParent)
                transform.SetParent(homeParent, true);
            transform.SetPositionAndRotation(homePosition, homeRotation);
        }

        void Update()
        {
            if (grab.isSelected)
                return;
            var t = Time.time;
            transform.SetPositionAndRotation(homePosition + Vector3.up * (Mathf.Sin(t * 1.1f) * idleBob),
                homeRotation * Quaternion.Euler(0f, t * idleSpin, 0f));
        }
    }
}
