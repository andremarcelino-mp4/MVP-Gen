using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Remote, pad, ball, book, extra cushion. Kettle/mugs become grabbable.</summary>
    public static class DojoToys
    {
        public static void Ensure(ProfileLook look)
        {
            if (look == null)
                return;
            var home = look.transform.Find("Pieces/Home") ?? look.transform.Find("Home");
            if (home == null)
                return;
            AirConUnit.Ensure(home);
            GrabExisting(home, "Kitchen/Kettle", 0.2f);
            GrabExisting(home, "Kitchen/MugA", 0.08f);
            GrabExisting(home, "Kitchen/MugB", 0.08f);
            if (home.Find("Toys") != null)
                return;
            var toys = new GameObject("Toys");
            toys.transform.SetParent(home, false);
            var tv = TvSet.Ensure(home);
            AirConUnit.Ensure(home);
            Remote(toys.transform, tv);
            Pad(toys.transform, tv);
            if (FindNamed(home, "Console") == null)
                Prim(PrimitiveType.Cube, "Console", toys.transform, new Vector3(-0.05f, 0.48f, -1.9f), new Vector3(0.22f, 0.06f, 0.16f), new Color(0.15f, 0.15f, 0.17f));
            Ball(toys.transform);
            Book(toys.transform);
            Cushion(toys.transform);
        }

        public static void EnsureAll()
        {
            var looks = Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include);
            for (var i = 0; i < looks.Length; i++)
                Ensure(looks[i]);
        }

        static void Remote(Transform parent, TvSet tv)
        {
            var go = Prim(PrimitiveType.Cube, "TvRemote", parent, new Vector3(0.55f, 0.28f, -1.18f), new Vector3(0.05f, 0.02f, 0.14f), new Color(0.12f, 0.12f, 0.14f));
            var grab = GrabUtil.Make(go, 0.08f);
            var click = go.AddComponent<AudioSource>();
            click.playOnAwake = false;
            click.spatialBlend = 1f;
            click.clip = GrabUtil.Click(880, 0.08f);
            grab.activated.AddListener(_ =>
            {
                click.Play();
                if (tv != null)
                    tv.TogglePower();
            });
            grab.selectEntered.AddListener(_ => { });
        }

        static void Pad(Transform parent, TvSet tv)
        {
            var go = Prim(PrimitiveType.Cube, "GamePad", parent, new Vector3(0.72f, 0.27f, -0.88f), new Vector3(0.16f, 0.03f, 0.08f), new Color(0.18f, 0.18f, 0.2f));
            var grab = GrabUtil.Make(go, 0.12f);
            var click = go.AddComponent<AudioSource>();
            click.playOnAwake = false;
            click.clip = GrabUtil.Click(520, 0.1f);
            grab.selectEntered.AddListener(_ =>
            {
                click.Play();
                if (tv != null)
                    tv.PlayMiniGame();
            });
            grab.activated.AddListener(_ =>
            {
                if (tv != null)
                    tv.Next();
            });
        }

        static void Ball(Transform parent)
        {
            var go = Prim(PrimitiveType.Sphere, "Ball", parent, new Vector3(0.2f, 0.12f, -0.35f), Vector3.one * 0.14f, new Color(0.12f, 0.14f, 0.18f));
            var mat = new PhysicsMaterial("BallBounce") { bounciness = 0.72f, dynamicFriction = 0.25f, staticFriction = 0.25f };
            var col = go.GetComponent<Collider>();
            col.material = mat;
            GrabUtil.Make(go, 0.18f);
            var thud = go.AddComponent<AudioSource>();
            thud.playOnAwake = false;
            thud.spatialBlend = 1f;
            thud.clip = GrabUtil.Click(140, 0.12f);
            go.AddComponent<BallThud>().source = thud;
        }

        static void Book(Transform parent)
        {
            var go = Prim(PrimitiveType.Cube, "Book", parent, new Vector3(0.38f, 0.275f, -0.92f), new Vector3(0.16f, 0.03f, 0.12f), new Color(0.45f, 0.18f, 0.12f));
            GrabUtil.Make(go, 0.15f);
        }

        static void Cushion(Transform parent)
        {
            var go = Prim(PrimitiveType.Cube, "CushionLoose", parent, new Vector3(1.35f, 0.48f, -0.7f), new Vector3(0.28f, 0.08f, 0.28f), new Color(0.75f, 0.42f, 0.28f));
            GrabUtil.Make(go, 0.2f);
        }

        static void GrabExisting(Transform home, string path, float mass)
        {
            var t = home.Find(path);
            if (t == null)
                return;
            GrabUtil.Make(t.gameObject, mass);
        }

        static Transform FindNamed(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                    return all[i];
            }

            return null;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = false;
            return go;
        }
    }
}
