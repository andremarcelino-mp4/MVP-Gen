using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.TableProps
{
    /// <summary>
    /// Returns a grabbable prop to its table pose after resting below the floor threshold.
    /// </summary>
    public class TablePropRespawn : MonoBehaviour
    {
        [SerializeField] float floorY = 0.15f;
        [SerializeField] float respawnDelay = 3f;

        XRGrabInteractable grab;
        Rigidbody body;
        Vector3 homePosition;
        Quaternion homeRotation;
        float belowFloorTimer;
        bool homeCaptured;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
        }

        void Start()
        {
            CaptureHome();
        }

        public void CaptureHome()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homeCaptured = true;
            belowFloorTimer = 0f;
        }

        void Update()
        {
            if (!homeCaptured)
                return;

            if (grab != null && grab.isSelected)
            {
                belowFloorTimer = 0f;
                return;
            }

            if (transform.position.y >= floorY)
            {
                belowFloorTimer = 0f;
                return;
            }

            belowFloorTimer += Time.deltaTime;
            if (belowFloorTimer < respawnDelay)
                return;

            Respawn();
        }

        void Respawn()
        {
            belowFloorTimer = 0f;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = homePosition;
                body.rotation = homeRotation;
            }
        }
    }
}
