using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Bounce sound for the dojo ball. Lives in its own file so Unity serializes it with a real script asset.</summary>
    public sealed class BallThud : MonoBehaviour
    {
        public AudioSource source;

        void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (source == null || collision.relativeVelocity.magnitude < 0.4f)
                return;
            source.pitch = Random.Range(0.9f, 1.1f);
            source.Play();
        }
    }
}
