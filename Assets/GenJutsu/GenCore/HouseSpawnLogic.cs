using UnityEngine;

namespace GenJutsu.GenCore
{
    /// <summary>
    /// House-local feet: room center, ~2 m from the TV, clear of furniture. Y is floor.
    /// </summary>
    public static class HouseSpawnLogic
    {
        public static readonly Vector3 LocalFeet = new Vector3(0.35f, 0f, 0.95f);
        public static readonly Vector3 LocalLook = new Vector3(-0.15f, 0.4f, -1.05f);
        public const float ClearRadius = 0.32f;

        public struct Box
        {
            public string Name;
            public Vector3 Center;
            public Vector3 Half;
        }

        public static readonly Box[] Furniture =
        {
            new Box { Name = "TvStand", Center = new Vector3(-0.15f, 0.4f, -1.05f), Half = new Vector3(0.28f, 0.5f, 0.55f) },
            new Box { Name = "Sofa", Center = new Vector3(1.45f, 0.3f, -1.05f), Half = new Vector3(0.45f, 0.4f, 0.9f) },
            new Box { Name = "Coffee", Center = new Vector3(0.55f, 0.2f, -1.05f), Half = new Vector3(0.4f, 0.25f, 0.4f) },
            new Box { Name = "Bed", Center = new Vector3(-1.62f, 0.25f, 0.35f), Half = new Vector3(0.55f, 0.3f, 1.05f) },
            new Box { Name = "Kitchen", Center = new Vector3(1.85f, 0.5f, 1.95f), Half = new Vector3(0.75f, 0.55f, 0.35f) },
            new Box { Name = "Ofuro", Center = new Vector3(-1.75f, 0.3f, 1.95f), Half = new Vector3(0.6f, 0.4f, 0.6f) },
            new Box { Name = "AirCon", Center = new Vector3(-0.15f, 1.7f, -1.05f), Half = new Vector3(0.5f, 0.2f, 0.2f) }
        };

        public static readonly string[] RequiredProps =
        {
            "TvStand", "TvRemote", "GamePad", "Console", "Ball", "Book", "CushionLoose",
            "Kitchen", "Kettle", "MugA", "AirCon", "AcRemote", "Sofa", "Bed"
        };

        public static bool Overlaps(Vector3 localPoint, float radius, out string hit)
        {
            hit = null;
            for (var i = 0; i < Furniture.Length; i++)
            {
                var b = Furniture[i];
                var d = localPoint - b.Center;
                var qx = Mathf.Max(0f, Mathf.Abs(d.x) - b.Half.x);
                var qy = Mathf.Max(0f, Mathf.Abs(d.y) - b.Half.y);
                var qz = Mathf.Max(0f, Mathf.Abs(d.z) - b.Half.z);
                if (qx * qx + qy * qy + qz * qz <= radius * radius)
                {
                    hit = b.Name;
                    return true;
                }
            }

            return false;
        }

        public static float DistanceToLook()
        {
            var a = LocalFeet;
            var b = LocalLook;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
