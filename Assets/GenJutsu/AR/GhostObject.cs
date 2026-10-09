using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GenJutsu.AR
{
    public class GhostObject : MonoBehaviour
    {
        public enum GhostState
        {
            Ghost,
            Solidifying,
            Solid,
            Ghostifying
        }

        [SerializeField] float solidifyDistance = 0.25f;
        [SerializeField] float ghostifyDistance = 0.5f;
        [SerializeField] float ghostifyDelay = 3f;
        [SerializeField] float fadeDuration = 0.35f;
        [SerializeField] float propTableDelay = 0.15f;

        public GhostState State { get; private set; } = GhostState.Ghost;
        public bool IsTable { get; set; }
        public GhostObject TableRef { get; set; }
        public GhostSystem SystemRef { get; set; }

        Renderer[] renderers;
        Material[][] originalMaterials;
        Color[][] baseTints;
        Collider[] colliders;
        bool[] colliderEnabled;
        Rigidbody[] rigidbodies;
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] grabs;
        Material ghostMaterial;
        MaterialPropertyBlock propertyBlock;
        Coroutine fadeRoutine;
        float awayTime;

        public void Initialize(Material ghost)
        {
            ghostMaterial = ghost;
            propertyBlock = new MaterialPropertyBlock();
            renderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[renderers.Length][];
            baseTints = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                var mats = renderers[i].sharedMaterials;
                originalMaterials[i] = mats;
                baseTints[i] = new Color[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                    baseTints[i][m] = CaptureBaseColor(mats[m]);
            }
            colliders = GetComponentsInChildren<Collider>(true);
            colliderEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
                colliderEnabled[i] = colliders[i].enabled;
            rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            grabs = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(true);
            ApplyGhostVisualImmediate();
        }

        static Color CaptureBaseColor(Material mat)
        {
            if (mat == null)
                return Color.white;
            if (mat.HasProperty("_BaseColor"))
                return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color"))
                return mat.GetColor("_Color");
            return Color.white;
        }

        public void Tick(Transform[] sources, float deltaTime)
        {
            if (ghostMaterial == null)
                return;

            float nearestSqr = float.MaxValue;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null)
                    continue;
                var closest = GetComponentBounds().ClosestPoint(sources[i].position);
                float sqr = (sources[i].position - closest).sqrMagnitude;
                if (sqr < nearestSqr)
                    nearestSqr = sqr;
            }

            if (State == GhostState.Ghost)
            {
                if (nearestSqr <= solidifyDistance * solidifyDistance)
                    RequestSolidify();
            }
            else if (State == GhostState.Solid)
            {
                if (nearestSqr > ghostifyDistance * ghostifyDistance && !AnyGrabbed())
                {
                    awayTime += deltaTime;
                    if (awayTime >= ghostifyDelay)
                    {
                        if (IsTable)
                        {
                            if (SystemRef == null || SystemRef.AllPropsGhosted())
                                RequestGhostify();
                        }
                        else
                        {
                            RequestGhostify();
                        }
                    }
                }
                else
                {
                    awayTime = 0f;
                }
            }
            else if (State == GhostState.Ghostifying)
            {
                if (nearestSqr <= solidifyDistance * solidifyDistance)
                    CancelGhostifyAndSolidify();
            }
        }

        bool AnyGrabbed()
        {
            for (int i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null && grabs[i].enabled && grabs[i].isSelected)
                    return true;
            }
            return false;
        }

        Bounds GetComponentBounds()
        {
            if (renderers != null && renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                return b;
            }
            return new Bounds(transform.position, Vector3.one * 0.2f);
        }

        public void RequestSolidify()
        {
            if (State == GhostState.Solidifying || State == GhostState.Solid)
                return;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            if (!IsTable && TableRef != null && TableRef.State != GhostState.Solid)
            {
                TableRef.RequestSolidify();
                fadeRoutine = StartCoroutine(SolidifyAfterDelay());
                return;
            }
            fadeRoutine = StartCoroutine(SolidifyRoutine());
        }

        IEnumerator SolidifyAfterDelay()
        {
            yield return new WaitForSeconds(propTableDelay);
            fadeRoutine = null;
            if (State == GhostState.Ghost)
                fadeRoutine = StartCoroutine(SolidifyRoutine());
        }

        IEnumerator SolidifyRoutine()
        {
            State = GhostState.Solidifying;
            awayTime = 0f;
            SetColliders(true);
            ApplyGhostVisual();

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                SetSolidMix(Mathf.Clamp01(t / fadeDuration));
                yield return null;
            }

            RestoreOriginalVisual();
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                    rigidbodies[i].isKinematic = false;
            }
            for (int i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null)
                    grabs[i].enabled = true;
            }
            State = GhostState.Solid;
            awayTime = 0f;
            fadeRoutine = null;
        }

        public void RequestGhostify()
        {
            if (State != GhostState.Solid)
                return;
            if (AnyGrabbed())
                return;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            fadeRoutine = StartCoroutine(GhostifyRoutine());
        }

        void CancelGhostifyAndSolidify()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            RequestSolidify();
        }

        IEnumerator GhostifyRoutine()
        {
            State = GhostState.Ghostifying;
            awayTime = 0f;
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                    rigidbodies[i].isKinematic = true;
            }
            for (int i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null)
                    grabs[i].enabled = false;
            }
            SetColliders(false);
            ApplyGhostVisual();
            SetSolidMix(1f);

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                SetSolidMix(1f - Mathf.Clamp01(t / fadeDuration));
                yield return null;
            }

            ApplyGhostVisualImmediate();
            State = GhostState.Ghost;
            fadeRoutine = null;
            if (SystemRef != null)
                SystemRef.NotifyPropGhosted();
        }

        public void ForceSolid()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            State = GhostState.Solidifying;
            awayTime = 0f;
            SetColliders(true);
            RestoreOriginalVisual();
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                    rigidbodies[i].isKinematic = false;
            }
            for (int i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null)
                    grabs[i].enabled = true;
            }
            State = GhostState.Solid;
        }

        public void ForceGhost()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            awayTime = 0f;
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                    rigidbodies[i].isKinematic = true;
            }
            for (int i = 0; i < grabs.Length; i++)
            {
                if (grabs[i] != null)
                    grabs[i].enabled = false;
            }
            SetColliders(false);
            ApplyGhostVisualImmediate();
            State = GhostState.Ghost;
        }

        void SetColliders(bool active)
        {
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = active ? colliderEnabled[i] : false;
        }

        void ApplyGhostVisualImmediate()
        {
            if (renderers == null || ghostMaterial == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = ghostMaterial;
                renderers[i].SetPropertyBlock(null);
            }
        }

        void ApplyGhostVisual()
        {
            if (renderers == null || ghostMaterial == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = ghostMaterial;
        }

        void SetSolidMix(float value)
        {
            if (renderers == null || ghostMaterial == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color tint = baseTints[i].Length > 0 ? baseTints[i][0] : Color.white;
                for (int m = 1; m < baseTints[i].Length; m++)
                    tint = Color.Lerp(tint, baseTints[i][m], 0.5f);
                propertyBlock.Clear();
                propertyBlock.SetFloat("_SolidMix", value);
                propertyBlock.SetColor("_BaseTint", tint);
                renderers[i].SetPropertyBlock(propertyBlock);
            }
        }

        void RestoreOriginalVisual()
        {
            if (renderers == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(null);
                renderers[i].sharedMaterials = originalMaterials[i];
            }
        }
    }
}
