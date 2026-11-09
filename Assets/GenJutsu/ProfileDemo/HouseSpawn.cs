using GenJutsu.GenCore;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Places the XR Origin on the house floor, facing the TV, not inside furniture.</summary>
    public static class HouseSpawn
    {
        public static void Align(Transform house)
        {
            if (house == null)
                return;
            var origin = FindOrigin();
            var head = Camera.main != null ? Camera.main.transform : origin;
            if (origin == null || head == null)
                return;

            var feet = house.TransformPoint(HouseSpawnLogic.LocalFeet);
            var look = house.TransformPoint(HouseSpawnLogic.LocalLook);
            var want = look - feet;
            want.y = 0f;
            var have = head.forward;
            have.y = 0f;
            if (want.sqrMagnitude > 0.0001f && have.sqrMagnitude > 0.0001f)
            {
                var yaw = Quaternion.FromToRotation(have.normalized, want.normalized);
                origin.rotation = yaw * origin.rotation;
            }

            var now = head.position;
            origin.position += new Vector3(feet.x - now.x, 0f, feet.z - now.z);
            var p = origin.position;
            origin.position = new Vector3(p.x, feet.y, p.z);
        }

        public static Transform FindOrigin()
        {
            var xr = Object.FindFirstObjectByType<XROrigin>();
            if (xr != null)
                return xr.Origin != null ? xr.Origin.transform : xr.transform;
            var named = GameObject.Find("XR Origin Hands (XR Rig)") ?? GameObject.Find("XR Origin");
            return named != null ? named.transform : Camera.main != null ? Camera.main.transform.root : null;
        }

        public static bool SceneClear(Transform house, out string hit)
        {
            hit = null;
            if (house == null)
                return true;
            var feet = house.TransformPoint(HouseSpawnLogic.LocalFeet);
            var probe = feet + Vector3.up * 0.9f;
            var cols = house.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null || !c.enabled)
                    continue;
                var n = c.transform.name;
                if (IsFloor(n))
                    continue;
                if (c.bounds.Contains(probe) || (c.ClosestPoint(probe) - probe).sqrMagnitude < HouseSpawnLogic.ClearRadius * HouseSpawnLogic.ClearRadius)
                {
                    hit = n;
                    return false;
                }
            }

            var rends = house.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                if (r == null || !r.enabled)
                    continue;
                var n = r.transform.name;
                if (IsFloor(n))
                    continue;
                if (r.bounds.Contains(probe))
                {
                    hit = n;
                    return false;
                }
            }

            return true;
        }

        static bool IsFloor(string n)
        {
            return n == "Deck" || n == "Tatami" || n == "TatamiField" || n == "Lip" || n == "Platform" || n == "Pieces" || n == "Home";
        }
    }
}
