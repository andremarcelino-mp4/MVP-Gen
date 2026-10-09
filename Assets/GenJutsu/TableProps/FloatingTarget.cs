using UnityEngine;

namespace GenJutsu.TableProps
{
    public class FloatingTarget : MonoBehaviour
    {
        [SerializeField] float bobAmplitude = 0.1f;
        [SerializeField] float bobFrequency = 0.45f;
        [SerializeField] float driftAmplitude = 0.35f;
        [SerializeField] float driftFrequency = 0.22f;
        [SerializeField] float swayDegrees = 10f;
        [SerializeField] float swayFrequency = 0.3f;

        Vector3 basePosition;
        float seed;
        Rigidbody body;

        void Start()
        {
            basePosition = transform.position;
            seed = Random.value * 100f;
            body = GetComponent<Rigidbody>();
        }

        void Update()
        {
            var t = (Time.time + seed) * Mathf.PI * 2f;
            var pos = basePosition;
            pos.y += Mathf.Sin(t * bobFrequency) * bobAmplitude;
            pos.x += Mathf.Sin(t * driftFrequency + 1.7f) * driftAmplitude;
            var yaw = Mathf.Sin(t * swayFrequency + 0.9f) * swayDegrees;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            transform.SetPositionAndRotation(pos, rot);
            if (body != null)
            {
                body.position = pos;
                body.rotation = rot;
            }
        }
    }
}
