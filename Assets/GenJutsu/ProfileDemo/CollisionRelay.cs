using System;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public class CollisionRelay : MonoBehaviour
    {
        public event Action<CollisionRelay, Collision> Collided;

        void OnCollisionEnter(Collision collision)
        {
            Collided?.Invoke(this, collision);
        }
    }
}
