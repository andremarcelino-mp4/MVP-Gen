using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Mixed lights, a light-probe grid and one boxed reflection probe per look.
    /// Runtime house_state still drives the same Mixed lights on top of baked GI.
    /// </summary>
    public static class QuestGiRig
    {
        public static void Ensure(ProfileDemoDirector director)
        {
            if (director == null)
                return;
            PrepareLight(director.mainLight, true);
            var looks = Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include);
            for (var i = 0; i < looks.Length; i++)
                RigLook(looks[i]);
            RigHub();
        }

        static void RigHub()
        {
            var table = GameObject.Find("JapaneseTableSet");
            if (table == null)
                return;
            MarkStatic(table.transform);
            AddProbes(table.transform, "WhiteRoomGi", new Vector3(4.2f, 2.6f, 4.2f), new Vector3(0f, 1.1f, 0f));
        }

        static void RigLook(ProfileLook look)
        {
            if (look == null)
                return;
            MarkStatic(look.transform);
            var lights = look.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
                PrepareLight(lights[i], lights[i].type == LightType.Directional);
            AddProbes(look.transform, look.name + "Gi", new Vector3(7.2f, 3.4f, 7.2f), new Vector3(0f, 1.4f, 0f));
        }

        static void PrepareLight(Light light, bool shadows)
        {
            if (light == null)
                return;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            if (shadows && light.type == LightType.Directional)
                light.shadows = LightShadows.Soft;
        }

        static void AddProbes(Transform root, string name, Vector3 box, Vector3 center)
        {
            var existing = root.Find(name);
            Transform holder;
            if (existing != null)
                holder = existing;
            else
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                holder = go.transform;
            }

            holder.localPosition = center;
            var probes = holder.GetComponent<LightProbeGroup>() ?? holder.gameObject.AddComponent<LightProbeGroup>();
            probes.probePositions = Grid(5, 3, 5, box);
            var probe = holder.GetComponent<ReflectionProbe>() ?? holder.gameObject.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.size = box;
            probe.center = Vector3.zero;
            probe.intensity = 0.85f;
            probe.nearClipPlane = 0.08f;
            probe.farClipPlane = 12f;
            probe.resolution = 128;
        }

        static Vector3[] Grid(int nx, int ny, int nz, Vector3 size)
        {
            var list = new Vector3[nx * ny * nz];
            var i = 0;
            for (var y = 0; y < ny; y++)
            for (var z = 0; z < nz; z++)
            for (var x = 0; x < nx; x++)
            {
                list[i++] = new Vector3(
                    Mathf.Lerp(-size.x * 0.4f, size.x * 0.4f, nx == 1 ? 0.5f : x / (float)(nx - 1)),
                    Mathf.Lerp(0.15f, size.y * 0.7f, ny == 1 ? 0.5f : y / (float)(ny - 1)),
                    Mathf.Lerp(-size.z * 0.4f, size.z * 0.4f, nz == 1 ? 0.5f : z / (float)(nz - 1)));
            }

            return list;
        }

        static void MarkStatic(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r.GetComponent<Rigidbody>() != null)
                    continue;
                r.gameObject.isStatic = true;
            }
        }
    }
}
