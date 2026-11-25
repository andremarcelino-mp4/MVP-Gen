using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public sealed class FurnitureMoveFx : MonoBehaviour
    {
        ParticleSystem burst;

        public void PlayAt(Vector3 world)
        {
            Ensure();
            transform.position = world + Vector3.up * 0.2f;
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.Play();
        }

        void Ensure()
        {
            if (burst != null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.95f, 0.9f, 0.7f, 0.85f);
            burst = gameObject.AddComponent<ParticleSystem>();
            var main = burst.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.35f;
            main.startSize = 0.04f;
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = burst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            var shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
