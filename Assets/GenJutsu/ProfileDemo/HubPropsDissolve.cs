using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Mesa + pergaminho fade out during morph. Never SetActive(false) on frame 0.
    /// </summary>
    public class HubPropsDissolve : MonoBehaviour
    {
        public GameObject[] roots;
        public float duration = 1.4f;
        public ParticleSystem sparkles;

        struct Slot
        {
            public Renderer renderer;
            public Material[] instances;
            public Color[] bases;
            public Collider[] colliders;
        }

        Slot[] slots;
        bool dissolved;

        void Cache()
        {
            if (slots != null)
                return;
            var list = new List<Slot>();
            if (roots == null)
                return;
            foreach (var root in roots)
            {
                if (root == null)
                    continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null)
                        continue;
                    var mats = r.materials;
                    var cols = new Color[mats.Length];
                    for (var i = 0; i < mats.Length; i++)
                    {
                        cols[i] = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : mats[i].color;
                        PrepareFade(mats[i]);
                    }

                    list.Add(new Slot
                    {
                        renderer = r,
                        instances = mats,
                        bases = cols,
                        colliders = r.GetComponentsInChildren<Collider>(true)
                    });
                }
            }

            slots = list.ToArray();
        }

        public IEnumerator Dissolve()
        {
            if (dissolved)
                yield break;
            Cache();
            if (sparkles != null)
                sparkles.Play();

            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var u = Mathf.SmoothStep(0f, 1f, t / duration);
                Apply(1f - u);
                yield return null;
            }

            Apply(0f);
            if (roots != null)
            {
                foreach (var root in roots)
                {
                    if (root != null)
                        root.SetActive(false);
                }
            }

            dissolved = true;
        }

        public void RestoreImmediate()
        {
            if (roots != null)
            {
                foreach (var root in roots)
                {
                    if (root != null)
                        root.SetActive(true);
                }
            }

            Cache();
            Apply(1f);
            dissolved = false;
        }

        void Apply(float alpha)
        {
            if (slots == null)
                return;
            foreach (var slot in slots)
            {
                if (slot.renderer == null)
                    continue;
                for (var i = 0; i < slot.instances.Length; i++)
                {
                    var c = slot.bases[i];
                    c.a *= alpha;
                    var mat = slot.instances[i];
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", c);
                    else
                        mat.color = c;
                }

                if (slot.colliders == null)
                    continue;
                var on = alpha > 0.08f;
                foreach (var col in slot.colliders)
                {
                    if (col != null)
                        col.enabled = on;
                }
            }
        }

        static void PrepareFade(Material mat)
        {
            if (mat == null)
                return;
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
        }
    }
}
