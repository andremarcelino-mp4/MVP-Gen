using System.Collections;
using GenJutsu.AR;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.TableProps
{
    public class ShurikenReturn : MonoBehaviour
    {
        [SerializeField] float floorY = 0.12f;
        [SerializeField] float returnDuration = 0.6f;
        [SerializeField] float arcHeight = 0.7f;
        [SerializeField] float extraSpinDegrees = 540f;
        [SerializeField] float throwSpinSpeed = 40f;
        [SerializeField] float minThrowSpeed = 0.8f;

        XRGrabInteractable grab;
        Rigidbody body;
        Vector3 homePosition;
        Quaternion homeRotation;
        bool returning;
        Coroutine returnRoutine;

        public bool Returning => returning;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            if (grab != null)
            {
                grab.selectExited.AddListener(OnSelectExited);
                grab.selectEntered.AddListener(OnSelectEntered);
            }
        }

        void Start()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        void OnDestroy()
        {
            if (grab == null)
                return;
            grab.selectExited.RemoveListener(OnSelectExited);
            grab.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (returning || body == null)
                return;
            if (body.linearVelocity.magnitude < minThrowSpeed)
                return;
            var spinAxis = transform.forward;
            body.angularVelocity += spinAxis * throwSpinSpeed;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (!returning)
                return;
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
            returning = false;
            if (body != null && !IsGhosted())
                body.isKinematic = false;
        }

        void Update()
        {
            if (returning)
                return;
            if (grab != null && grab.isSelected)
                return;
            if (transform.position.y < floorY)
                BeginReturn();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (returning)
                return;
            if (collision.contactCount > 0 && collision.GetContact(0).point.y < floorY)
                BeginReturn();
        }

        public void BeginReturn()
        {
            if (returning)
                return;
            if (grab != null && grab.isSelected)
                return;
            if (IsGhosted())
                return;
            returning = true;
            returnRoutine = StartCoroutine(ReturnRoutine());
        }

        bool IsGhosted()
        {
            var ghost = GetComponent<GhostObject>();
            return ghost != null && ghost.State != GhostObject.GhostState.Solid;
        }

        IEnumerator ReturnRoutine()
        {
            var startPos = transform.position;
            var startRot = transform.rotation;
            var spinAxis = homeRotation * Vector3.forward;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            var t = 0f;
            while (t < returnDuration)
            {
                if (grab != null && grab.isSelected)
                {
                    FinishReturn(true);
                    yield break;
                }
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / returnDuration);
                var e = u * u * (3f - 2f * u);
                var point = Vector3.Lerp(startPos, homePosition, e);
                point += Vector3.up * (arcHeight * Mathf.Sin(e * Mathf.PI));
                transform.position = point;
                var baseRot = Quaternion.Slerp(startRot, homeRotation, e);
                transform.rotation = Quaternion.AngleAxis(extraSpinDegrees * e, spinAxis) * baseRot;
                yield return null;
            }

            FinishReturn(false);
        }

        void FinishReturn(bool interrupted)
        {
            returnRoutine = null;
            returning = false;
            if (interrupted)
                return;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            if (body == null)
                return;
            body.position = homePosition;
            body.rotation = homeRotation;
            if (!IsGhosted())
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
