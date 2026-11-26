using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Quest grab: Velocity Tracking, throw on detach, gravity on, not kinematic.
    /// Floors and table tops get non-trigger colliders so props land instead of hanging or falling through.
    /// </summary>
    public static class HouseGrabPhysics
    {
        static readonly string[] FloorNames =
        {
            "Deck", "Tatami", "TatamiField", "Lip", "Platform", "HubFloor"
        };

        static readonly string[] TableNames = { "Table", "Top", "Coffee" };

        public static void Apply()
        {
            EnsureFloorsAndTables();
            var grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            for (var i = 0; i < grabs.Length; i++)
                ConfigureGrab(grabs[i]);
        }

        public static bool IsPhysicalProp(XRGrabInteractable grab)
        {
            if (grab == null)
                return false;
            if (grab.GetComponent<ProfileToken>() != null)
                return false;
            if (grab.GetComponent<ReturnCube>() != null)
                return false;
            var n = grab.gameObject.name;
            if (n == "JapaneseTableSet" || n == "TableGroup")
                return false;
            return true;
        }

        public static void ConfigureGrab(XRGrabInteractable grab)
        {
            if (!IsPhysicalProp(grab))
                return;
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.forceGravityOnDetach = true;
            grab.retainTransformParent = false;
            var body = grab.GetComponent<Rigidbody>();
            if (body == null)
                body = grab.gameObject.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.None;
            if (grab.GetComponent<Collider>() == null)
                grab.gameObject.AddComponent<BoxCollider>().isTrigger = false;
            foreach (var col in grab.GetComponents<Collider>())
                col.isTrigger = false;
        }

        public static void EnsureFloorsAndTables()
        {
            EnsureHubFloor();
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null)
                    continue;
                if (IsFloorName(t.name) || IsTableName(t.name))
                    EnsureSolidCollider(t.gameObject);
            }
        }

        public static bool HasSolidCollider(GameObject go)
        {
            if (go == null)
                return false;
            var cols = go.GetComponents<Collider>();
            for (var i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && cols[i].enabled && !cols[i].isTrigger)
                    return true;
            }

            return false;
        }

        static void EnsureHubFloor()
        {
            if (GameObject.Find("HubFloor") != null)
                return;
            var parent = GameObject.Find("ProfileDemo");
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HubFloor";
            if (parent != null)
                go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            go.transform.localScale = new Vector3(14f, 0.12f, 14f);
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
                rend.enabled = false;
            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
        }

        static void EnsureSolidCollider(GameObject go)
        {
            if (HasSolidCollider(go))
            {
                foreach (var col in go.GetComponents<Collider>())
                    col.isTrigger = false;
                return;
            }

            go.AddComponent<BoxCollider>().isTrigger = false;
        }

        static bool IsFloorName(string n)
        {
            for (var i = 0; i < FloorNames.Length; i++)
            {
                if (n == FloorNames[i])
                    return true;
            }

            return false;
        }

        static bool IsTableName(string n)
        {
            for (var i = 0; i < TableNames.Length; i++)
            {
                if (n == TableNames[i])
                    return true;
            }

            return false;
        }
    }
}
