using System.Collections;
using UnityEngine;

namespace GenJutsu.TableProps
{
    public class ShurikenTarget : MonoBehaviour
    {
        [SerializeField] float pulseScale = 1.08f;
        [SerializeField] float pulseDuration = 0.25f;
        [SerializeField] float hitCooldown = 0.4f;
        [SerializeField] string popupText = "+1";
        [SerializeField] Color popupColor = new Color(0.2f, 0.95f, 0.35f, 1f);

        Vector3 baseScale;
        Coroutine pulseRoutine;
        float lastHitTime = -10f;

        public int Hits { get; private set; }

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void OnCollisionEnter(Collision collision)
        {
            var body = collision.rigidbody;
            if (body == null || body.isKinematic)
                return;
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            RegisterHit(point);
            var shurikenReturn = collision.collider.GetComponentInParent<ShurikenReturn>();
            if (shurikenReturn != null)
                shurikenReturn.BeginReturn();
        }

        public void OnHit()
        {
            RegisterHit(transform.position + Vector3.up * 0.15f);
        }

        void RegisterHit(Vector3 point)
        {
            if (Time.time - lastHitTime < hitCooldown)
                return;
            lastHitTime = Time.time;
            Hits++;
            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
            ScorePopup.Spawn(point + Vector3.up * 0.04f, popupText, popupColor);
        }

        IEnumerator PulseRoutine()
        {
            var t = 0f;
            while (t < pulseDuration)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / pulseDuration);
                var k = Mathf.Sin(u * Mathf.PI);
                transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, k);
                yield return null;
            }
            transform.localScale = baseScale;
            pulseRoutine = null;
        }
    }
}
