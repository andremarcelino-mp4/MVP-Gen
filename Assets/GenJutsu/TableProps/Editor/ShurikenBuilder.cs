using System.Collections.Generic;
using GenJutsu.TableProps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GenJutsu.TableProps.EditorTools
{
    public static class ShurikenBuilder
    {
        const string RootName = "JapaneseTableSet";
        static readonly Vector3 StandLocalPos = new Vector3(-0.444f, 0.65f, -0.26f);
        static readonly Vector3 ShurikenLocalPos = new Vector3(-0.452f, 0.674f, -0.258f);
        static readonly Vector3 TargetWorldPos = new Vector3(0f, 1.05f, -4.4f);

        [MenuItem("GenJutsu/Rebuild Shuriken Set")]
        static void MenuRebuildShurikenSet()
        {
            Debug.Log("[GenJutsu] " + RebuildShurikenSet());
        }

        internal static string RebuildShurikenSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var rootGo = GameObject.Find(RootName);
            if (rootGo == null)
                return "Root " + RootName + " not found — run GenJutsu/Rebuild Japanese Table Set first";

            foreach (var childName in new[] { "ShurikenStand", "Shuriken" })
            {
                var existing = rootGo.transform.Find(childName);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
            }
            var oldTarget = GameObject.Find("FloatingTarget");
            if (oldTarget != null)
                Object.DestroyImmediate(oldTarget);

            var red = LoadMat("ScrollRed", new Color(0.58f, 0.13f, 0.10f));
            var gold = LoadMat("ScrollGold", new Color(0.85f, 0.70f, 0.25f));
            var white = LoadMat("TableWhite", new Color(0.96f, 0.96f, 0.96f));
            var steel = MakeMat("ShurikenSteel", new Color(0.62f, 0.64f, 0.68f), metallic: 0.85f, smoothness: 0.55f);
            var straw = MakeMat("MatoStraw", new Color(0.82f, 0.72f, 0.48f), smoothness: 0.3f);
            var rope = MakeMat("MatoRope", new Color(0.55f, 0.45f, 0.28f), smoothness: 0.25f);

            BuildStand(rootGo.transform, red, gold);
            var shuriken = BuildShuriken(rootGo.transform, steel, red);
            JapaneseTableBuilder.MakeGrabbable(shuriken, 0.06f);
            shuriken.AddComponent<ShurikenReturn>();
            BuildFloatingTarget(straw, white, red, rope);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Rebuilt ShurikenStand (lacquer tray, gold rim) + Shuriken (4-point star, throw spin, auto-return) + FloatingTarget (mato, bob/drift/sway)";
        }

        static void BuildStand(Transform parent, Material red, Material gold)
        {
            var stand = new GameObject("ShurikenStand");
            stand.transform.SetParent(parent, false);
            stand.transform.localPosition = StandLocalPos;

            var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Tray";
            tray.transform.SetParent(stand.transform, false);
            tray.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            tray.transform.localScale = new Vector3(0.20f, 0.02f, 0.20f);
            tray.GetComponent<Renderer>().sharedMaterial = red;
            Object.DestroyImmediate(tray.GetComponent<Collider>());

            for (var i = 0; i < 4; i++)
            {
                var alongX = i < 2;
                var sign = i % 2 == 0 ? 1f : -1f;
                var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rim.name = "Rim" + (i + 1);
                rim.transform.SetParent(stand.transform, false);
                rim.transform.localPosition = alongX
                    ? new Vector3(0f, 0.0275f, 0.0925f * sign)
                    : new Vector3(0.0925f * sign, 0.0275f, 0f);
                rim.transform.localScale = alongX
                    ? new Vector3(0.20f, 0.015f, 0.015f)
                    : new Vector3(0.015f, 0.015f, 0.17f);
                rim.GetComponent<Renderer>().sharedMaterial = gold;
                Object.DestroyImmediate(rim.GetComponent<Collider>());
            }

            var col = stand.AddComponent<BoxCollider>();
            col.size = new Vector3(0.20f, 0.02f, 0.20f);
            col.center = new Vector3(0f, 0.01f, 0f);
        }

        static GameObject BuildShuriken(Transform parent, Material steel, Material red)
        {
            var go = new GameObject("Shuriken");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = ShurikenLocalPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 45f, 0f);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = LoadOrCreateShurikenMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = steel;

            var mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mark.name = "CenterMark";
            mark.transform.SetParent(go.transform, false);
            mark.transform.localPosition = new Vector3(0f, 0f, 0.0055f);
            mark.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mark.transform.localScale = new Vector3(0.022f, 0.001f, 0.022f);
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.GetComponent<Renderer>().sharedMaterial = red;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.14f, 0.14f, 0.008f);
            return go;
        }

        static void BuildFloatingTarget(Material straw, Material white, Material red, Material rope)
        {
            var root = new GameObject("FloatingTarget");
            root.transform.position = TargetWorldPos;

            AddCylinder(root.transform, "MatoBody", new Vector3(0.36f, 0.02f, 0.36f), straw);
            AddCylinder(root.transform, "WhiteRing", new Vector3(0.26f, 0.021f, 0.26f), white);
            AddCylinder(root.transform, "RedCenter", new Vector3(0.13f, 0.023f, 0.13f), red);

            for (var i = 0; i < 2; i++)
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = i == 0 ? "RopeL" : "RopeR";
                band.transform.SetParent(root.transform, false);
                band.transform.localPosition = new Vector3(i == 0 ? -0.14f : 0.14f, 0f, 0f);
                band.transform.localScale = new Vector3(0.016f, 0.22f, 0.048f);
                band.GetComponent<Renderer>().sharedMaterial = rope;
                Object.DestroyImmediate(band.GetComponent<Collider>());
            }

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.36f, 0.36f, 0.3f);

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            root.AddComponent<ShurikenTarget>();
            root.AddComponent<FloatingTarget>();
        }

        static void AddCylinder(Transform parent, string name, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static Mesh LoadOrCreateShurikenMesh()
        {
            const string path = "Assets/GenJutsu/TableProps/Meshes/Shuriken.asset";
            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "TableProps");
            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps/Meshes"))
                AssetDatabase.CreateFolder("Assets/GenJutsu/TableProps", "Meshes");

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            const float outer = 0.07f;
            const float inner = 0.026f;
            const float hole = 0.009f;
            const float half = 0.004f;

            var ring = new Vector2[8];
            var holeRing = new Vector2[8];
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 0.25f;
                var radius = i % 2 == 0 ? outer : inner;
                ring[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                holeRing[i] = new Vector2(Mathf.Cos(angle) * hole, Mathf.Sin(angle) * hole);
            }

            var verts = new List<Vector3>(96);
            var uvs = new List<Vector2>(96);
            var tris = new List<int>(192);

            Vector2 Uv(Vector2 p) => new Vector2(p.x / (2f * outer) + 0.5f, p.y / (2f * outer) + 0.5f);

            for (var i = 0; i < 8; i++)
            {
                verts.Add(new Vector3(ring[i].x, ring[i].y, half));
                uvs.Add(Uv(ring[i]));
            }
            for (var i = 0; i < 8; i++)
            {
                verts.Add(new Vector3(holeRing[i].x, holeRing[i].y, half));
                uvs.Add(Uv(holeRing[i]));
            }
            for (var i = 0; i < 8; i++)
            {
                verts.Add(new Vector3(ring[i].x, ring[i].y, -half));
                uvs.Add(Uv(ring[i]));
            }
            for (var i = 0; i < 8; i++)
            {
                verts.Add(new Vector3(holeRing[i].x, holeRing[i].y, -half));
                uvs.Add(Uv(holeRing[i]));
            }

            for (var i = 0; i < 8; i++)
            {
                var j = (i + 1) % 8;
                tris.Add(i);
                tris.Add(j);
                tris.Add(8 + j);
                tris.Add(i);
                tris.Add(8 + j);
                tris.Add(8 + i);

                tris.Add(16 + i);
                tris.Add(24 + j);
                tris.Add(16 + j);
                tris.Add(16 + i);
                tris.Add(24 + i);
                tris.Add(24 + j);
            }

            for (var i = 0; i < 8; i++)
            {
                var j = (i + 1) % 8;
                var b = verts.Count;
                verts.Add(new Vector3(ring[i].x, ring[i].y, half));
                verts.Add(new Vector3(ring[i].x, ring[i].y, -half));
                verts.Add(new Vector3(ring[j].x, ring[j].y, -half));
                verts.Add(new Vector3(ring[j].x, ring[j].y, half));
                uvs.Add(Uv(ring[i]));
                uvs.Add(Uv(ring[i]));
                uvs.Add(Uv(ring[j]));
                uvs.Add(Uv(ring[j]));
                tris.Add(b);
                tris.Add(b + 1);
                tris.Add(b + 2);
                tris.Add(b);
                tris.Add(b + 2);
                tris.Add(b + 3);
            }

            for (var i = 0; i < 8; i++)
            {
                var j = (i + 1) % 8;
                var b = verts.Count;
                verts.Add(new Vector3(holeRing[i].x, holeRing[i].y, half));
                verts.Add(new Vector3(holeRing[i].x, holeRing[i].y, -half));
                verts.Add(new Vector3(holeRing[j].x, holeRing[j].y, -half));
                verts.Add(new Vector3(holeRing[j].x, holeRing[j].y, half));
                uvs.Add(Uv(holeRing[i]));
                uvs.Add(Uv(holeRing[i]));
                uvs.Add(Uv(holeRing[j]));
                uvs.Add(Uv(holeRing[j]));
                tris.Add(b);
                tris.Add(b + 2);
                tris.Add(b + 1);
                tris.Add(b);
                tris.Add(b + 3);
                tris.Add(b + 2);
            }

            var mesh = new Mesh { name = "Shuriken" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Material LoadMat(string name, Color fallback)
        {
            var path = "Assets/GenJutsu/TableProps/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;
            return MakeMat(name, fallback);
        }

        static Material MakeMat(string name, Color color, float metallic = -1f, float smoothness = -1f)
        {
            var dir = "Assets/GenJutsu/TableProps/Materials";
            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "TableProps");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/GenJutsu/TableProps", "Materials");
            var path = dir + "/" + name + ".mat";

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (metallic >= 0f && mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (smoothness >= 0f && mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
