using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Instantiates Kenney CC0 furniture from Resources/ZceaKenney.</summary>
    public static class ZceaModels
    {
        public static Transform Place(string id, Transform parent, Vector3 pos, float yaw, float scale = 1f)
        {
            var prefab = Resources.Load<GameObject>("ZceaKenney/" + id);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.name = id;
                StripImporterExtras(go);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = id;
                go.transform.SetParent(parent, false);
                go.transform.localScale = Vector3.one * 0.35f;
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * (scale * 0.1f);
            return go.transform;
        }

        static void StripImporterExtras(GameObject go)
        {
            var cam = go.GetComponent<Camera>();
            if (cam != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(cam);
                else
                    Object.DestroyImmediate(cam);
            }
            var lights = go.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                if (Application.isPlaying)
                    Object.Destroy(lights[i]);
                else
                    Object.DestroyImmediate(lights[i]);
            }
        }

        public static void Tint(Transform root, Color color)
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                if (r == null || r.sharedMaterial == null)
                    continue;
                var mat = new Material(r.sharedMaterial);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                mat.color = color;
                r.sharedMaterial = mat;
            }
        }
    }
}
