using UnityEngine;

namespace GenJutsu.TableProps
{
    /// <summary>
    /// Keeps a simple liquid visual upright enough for the VR table demo.
    /// </summary>
    public class TeaCup : MonoBehaviour
    {
        [SerializeField] Transform liquid;
        [SerializeField] float maxTilt = 55f;

        void LateUpdate()
        {
            if (liquid == null)
                return;

            var up = Vector3.up;
            var tilt = Vector3.Angle(transform.up, up);
            if (tilt > maxTilt)
                liquid.gameObject.SetActive(false);
            else
            {
                liquid.gameObject.SetActive(true);
                liquid.rotation = Quaternion.LookRotation(transform.forward, up);
            }
        }
    }
}
