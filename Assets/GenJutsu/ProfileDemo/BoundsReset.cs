using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Guarda a pose de projeto de um objeto pegável (local ao pai) e devolve o objeto para ela.
    /// A pose é serializada na cena: antes ficava só em memória do Editor e, no aparelho,
    /// Restore() mandava todos os móveis para (0,0,0) do pai.
    /// </summary>
    public sealed class BoundsReset : MonoBehaviour
    {
        [SerializeField, HideInInspector] Vector3 homeLocal;
        [SerializeField, HideInInspector] Quaternion homeRot = Quaternion.identity;
        [SerializeField, HideInInspector] Transform homeParent;
        [SerializeField, HideInInspector] bool captured;
        Rigidbody body;

        public bool Captured => captured;
        public Vector3 HomeLocal => homeLocal;
        public Transform HomeParent => homeParent;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (!captured)
                Capture();
        }

        public void Capture()
        {
            homeParent = transform.parent;
            homeLocal = transform.localPosition;
            homeRot = transform.localRotation;
            body = GetComponent<Rigidbody>();
            captured = true;
        }

        public void CaptureIfMissing()
        {
            if (!captured)
                Capture();
        }

        public void Restore()
        {
            if (!captured)
                return;
            if (homeParent != null && transform.parent != homeParent)
                transform.SetParent(homeParent, false);
            var rot = homeRot;
            if (rot.x == 0f && rot.y == 0f && rot.z == 0f && rot.w == 0f)
                rot = Quaternion.identity;
            transform.localPosition = homeLocal;
            transform.localRotation = rot;
            var rest = GetComponent<GrabRest>();
            if (rest != null)
                rest.Park();
            else if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        void LateUpdate()
        {
            if (!captured)
                return;
            if (transform.localPosition.y > -0.4f && transform.position.y < 4f)
                return;
            Restore();
        }
    }
}
