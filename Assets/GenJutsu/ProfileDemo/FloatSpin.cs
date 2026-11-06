using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public class FloatSpin : MonoBehaviour
    {
        public Vector3 spinAxis = Vector3.up;
        public float spinSpeed = 20f;
        public float bobHeight = 0.05f;
        public float bobSpeed = 0.6f;
        public float phase;

        Vector3 basePosition;
        Quaternion baseRotation;

        void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        void Update()
        {
            var t = Time.time + phase;
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(t * bobSpeed * Mathf.PI * 2f) * bobHeight);
            transform.localRotation = baseRotation * Quaternion.AngleAxis(t * spinSpeed, spinAxis);
        }
    }
}
