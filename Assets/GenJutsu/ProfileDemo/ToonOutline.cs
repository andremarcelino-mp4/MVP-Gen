using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Thin black inverted-hull outline on house meshes. Skips UI, XR hands/controllers, and TV/AC screens.
    /// </summary>
    public static class ToonOutline
    {
        const string Child = "ToonOutlineHull";
        static Material shared;

        public static void EnsureAll()
        {
            var looks = Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include);
            for (var i = 0; i < looks.Length; i++)
                Ensure(looks[i]);
        }

        public static void Ensure(ProfileLook look)
        {
            if (look == null)
                return;
            var renderers = look.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
                Apply(renderers[i]);
        }

        public static void Apply(MeshRenderer renderer)
        {
            if (renderer == null || !ShouldOutline(renderer.transform))
                return;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return;
            if (renderer.transform.Find(Child) != null)
                return;
            var go = new GameObject(Child);
            go.transform.SetParent(renderer.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var hull = go.AddComponent<MeshRenderer>();
            hull.sharedMaterial = Mat();
            hull.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hull.receiveShadows = false;
            hull.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            hull.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            go.layer = renderer.gameObject.layer;
        }

        public static bool ShouldOutline(Transform t)
        {
            while (t != null)
            {
                var n = t.name;
                if (n == Child || n == "Screen" || n == "Display" || n == "TempText" || n == "Airflow" ||
                    n.StartsWith("Device_", System.StringComparison.Ordinal) ||
                    t.GetComponent<Canvas>() != null || t.GetComponent<Graphic>() != null)
                    return false;
                if (n.IndexOf("Controller", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Interactor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.StartsWith("XR ", System.StringComparison.Ordinal))
                    return false;
                t = t.parent;
            }

            return true;
        }

        static Material Mat()
        {
            if (shared != null)
                return shared;
            var shader = Shader.Find("GenJutsu/ToonOutline") ?? Shader.Find("Hidden/InternalErrorShader");
            shared = new Material(shader) { name = "ToonOutlineShared", hideFlags = HideFlags.DontSave };
            shared.SetColor("_Color", Color.black);
            shared.SetFloat("_Width", 0.012f);
            return shared;
        }
    }
}
