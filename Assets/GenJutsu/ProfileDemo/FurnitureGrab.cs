using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Makes Home / Accents / lanterns / toys grabbable without nesting rigidbodies.</summary>
    public static class FurnitureGrab
    {
        public static void Ensure(ProfileLook look)
        {
            if (look == null)
                return;
            var roots = new[]
            {
                look.transform.Find("Pieces/Home"),
                look.transform.Find("Home"),
                look.transform.Find("Pieces/Accents"),
                look.transform.Find("Accents"),
                look.transform.Find("Pieces/Lanterns"),
                look.transform.Find("Lanterns"),
                look.transform.Find("Pieces/LowTable")
            };
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                    Walk(roots[i]);
            }
        }

        static void Walk(Transform root)
        {
            foreach (Transform child in root)
            {
                if (child.name == "ToonOutlineHull" || child.name == "Device_sala_luz" ||
                    child.name == "Device_quarto_luz" || child.name == "Device_tv" || child.name == "Device_ambiente")
                    continue;
                if (child.GetComponent<Canvas>() != null)
                    continue;
                if (HasGrabAncestor(child))
                    continue;
                if (child.GetComponent<XRGrabInteractable>() != null)
                {
                    EnsureHaptics(child.gameObject);
                    continue;
                }

                if (child.name == "Kitchen" || child.name == "Toys")
                {
                    Walk(child);
                    continue;
                }

                if (child.GetComponent<Renderer>() != null || child.childCount > 0)
                    GrabUtil.Make(child.gameObject, Mass(child.name));
            }
        }

        static bool HasGrabAncestor(Transform t)
        {
            var p = t.parent;
            while (p != null)
            {
                if (p.GetComponent<XRGrabInteractable>() != null)
                    return true;
                if (p.GetComponent<ProfileLook>() != null)
                    return false;
                p = p.parent;
            }

            return false;
        }

        static void EnsureHaptics(GameObject go)
        {
            if (go.GetComponent<GrabHaptics>() == null)
                go.AddComponent<GrabHaptics>();
        }

        static float Mass(string name)
        {
            switch (name)
            {
                case "Sofa":
                case "Bed":
                case "Kotatsu":
                case "Chabudai":
                case "Bookshelf":
                case "Arcade":
                    return 16f;
                case "TvStand":
                case "Desk":
                case "Chair":
                case "Fireplace":
                case "Byobu":
                    return 10f;
                case "AirCon":
                case "BeanBag":
                    return 6f;
                default:
                    return 1.2f;
            }
        }
    }
}
