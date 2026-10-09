using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GenJutsu.TableProps.EditorTools
{
    public static class JadeDragonBuilder
    {
        const string DragonName = "JadeDragon";
        const string TableSetRoot = "JapaneseTableSet";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string MaterialsFolder = "Assets/GenJutsu/TableProps/Materials";
        const string MeshesFolder = "Assets/GenJutsu/TableProps/Meshes";

        const int BodySegments = 40;

        static readonly Vector3[] Spine =
        {
            new Vector3(0.000f, 0.032f, -0.074f),
            new Vector3(-0.048f, 0.030f, -0.058f),
            new Vector3(-0.074f, 0.034f, -0.008f),
            new Vector3(-0.058f, 0.044f, 0.048f),
            new Vector3(0.000f, 0.058f, 0.074f),
            new Vector3(0.060f, 0.072f, 0.048f),
            new Vector3(0.076f, 0.094f, -0.004f),
            new Vector3(0.050f, 0.124f, -0.046f),
            new Vector3(0.010f, 0.154f, -0.050f),
            new Vector3(-0.014f, 0.174f, -0.018f),
            new Vector3(-0.010f, 0.168f, 0.024f),
            new Vector3(0.000f, 0.150f, 0.054f)
        };

        static readonly Vector3[][] Legs =
        {
            new[]
            {
                new Vector3(-0.048f, 0.048f, 0.046f), new Vector3(-0.076f, 0.036f, 0.060f),
                new Vector3(-0.058f, 0.024f, 0.078f)
            },
            new[]
            {
                new Vector3(0.050f, 0.058f, 0.048f), new Vector3(0.078f, 0.040f, 0.062f),
                new Vector3(0.060f, 0.024f, 0.080f)
            },
            new[]
            {
                new Vector3(-0.070f, 0.036f, -0.010f), new Vector3(-0.090f, 0.030f, -0.032f),
                new Vector3(-0.074f, 0.024f, -0.054f)
            },
            new[]
            {
                new Vector3(0.072f, 0.070f, -0.010f), new Vector3(0.092f, 0.042f, -0.032f),
                new Vector3(0.076f, 0.024f, -0.052f)
            }
        };

        static readonly Vector3[] FootForward =
        {
            new Vector3(-0.10f, -0.30f, 1f), new Vector3(0.10f, -0.30f, 1f),
            new Vector3(-0.55f, -0.25f, 0.8f), new Vector3(0.55f, -0.25f, 0.8f)
        };

        static readonly float[] LegRadius = { 0.017f, 0.017f, 0.021f, 0.021f };

        static Mesh _unitCone;

        [MenuItem("GenJutsu/Rebuild Jade Dragon")]
        static void MenuRebuild()
        {
            Debug.Log("[GenJutsu] " + RebuildInScene());
        }

        public static string RebuildInScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            Transform parent = null;
            var localPos = new Vector3(0f, 0.66f, 0.22f);

            var existing = GameObject.Find(DragonName);
            if (existing != null)
            {
                parent = existing.transform.parent;
                localPos = existing.transform.localPosition;
                Object.DestroyImmediate(existing);
            }
            else
            {
                var set = GameObject.Find(TableSetRoot);
                if (set != null)
                    parent = set.transform;
            }

            var jade = LoadOrCreate("Jade", new Color(0.25f, 0.75f, 0.45f), true, new Color(0.15f, 0.55f, 0.3f));
            var gold = LoadOrCreate("ScrollGold", new Color(0.85f, 0.7f, 0.25f), false, Color.black);

            var root = Build(parent, localPos, jade, gold);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Rebuilt " + DragonName + " parts=" + (root.GetComponentsInChildren<Transform>(true).Length - 1);
        }

        public static GameObject Build(Transform parent, Vector3 localPos, Material jade, Material gold)
        {
            var dark = LoadOrCreate("JadeDark", new Color(0.07f, 0.30f, 0.18f), false, Color.black);

            var root = new GameObject(DragonName);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = localPos;
            }
            else
            {
                root.transform.position = localPos;
            }

            var body = root.transform;

            BuildPlinth(body, jade);
            BuildSpine(body, jade, dark);
            BuildLegs(body, jade, dark);
            BuildHead(body, jade, dark, gold);
            BuildPearl(body, gold);
            FitCollider(root);

            JapaneseTableBuilder.MakeGrabbable(root, 0.35f);
            JapaneseTableBuilder.AddRespawn(root);
            return root;
        }

        static void BuildPlinth(Transform body, Material jade)
        {
            Part(body, "Plinth", PrimitiveType.Cylinder, new Vector3(0f, 0.007f, 0f), Quaternion.identity,
                new Vector3(0.22f, 0.007f, 0.22f), jade);
            Part(body, "PlinthTop", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f), Quaternion.identity,
                new Vector3(0.196f, 0.006f, 0.196f), jade);
        }

        static void BuildSpine(Transform body, Material jade, Material dark)
        {
            for (var i = 0; i < BodySegments; i++)
            {
                var t = i / (BodySegments - 1f);
                var r = SpineRadius(t);
                Part(body, i == 0 ? "Tail" : "Body" + i, PrimitiveType.Sphere, SpinePoint(t),
                    Quaternion.LookRotation(SpineTangent(t)),
                    new Vector3(r * 1.9f, r * 1.9f, r * 3f), jade);
            }

            for (var k = 0; k < 11; k++)
            {
                var t = 0.12f + k * 0.078f;
                var pos = SpinePoint(t);
                var dir = SpineTangent(t);
                var up = OrthoUp(dir);
                var r = SpineRadius(t);
                var h = 0.008f + 0.014f * Mathf.Sin(Mathf.PI * t);
                Cone(body, "Spine" + k, pos + up * (r * 0.78f), Quaternion.LookRotation((up - dir * 0.45f).normalized),
                    r * 0.42f, h, dark);
            }

            var tail = SpinePoint(0f);
            var tailDir = -SpineTangent(0f);
            var tailUp = OrthoUp(tailDir);
            var tailRight = Vector3.Cross(tailUp, tailDir).normalized;
            for (var i = -1; i <= 1; i++)
            {
                var dir = (tailDir + tailRight * (i * 0.55f) + tailUp * 0.35f).normalized;
                Cone(body, "TailFin" + (i + 1), tail + tailUp * 0.004f, Quaternion.LookRotation(dir), 0.007f, 0.026f, jade);
            }
        }

        static void BuildLegs(Transform body, Material jade, Material dark)
        {
            for (var i = 0; i < Legs.Length; i++)
            {
                var leg = Legs[i];
                var r = LegRadius[i];
                Limb(body, "UpperLeg" + i, leg[0], leg[1], r, r * 0.78f, jade);
                Limb(body, "LowerLeg" + i, leg[1], leg[2], r * 0.74f, r * 0.55f, jade);
                Part(body, "Knee" + i, PrimitiveType.Sphere, leg[1], Quaternion.identity,
                    Vector3.one * (r * 1.5f), jade);
                Foot(body, "Foot" + i, leg[2], FootForward[i].normalized, jade, dark);
            }
        }

        static void Foot(Transform body, string name, Vector3 pos, Vector3 forward, Material jade, Material dark)
        {
            var rot = Quaternion.LookRotation(forward);
            Part(body, name, PrimitiveType.Sphere, pos, rot, new Vector3(0.046f, 0.022f, 0.056f), jade);
            for (var i = -1; i <= 1; i++)
            {
                var clawPos = pos + rot * new Vector3(i * 0.015f, -0.003f, 0.024f);
                var dir = (rot * new Vector3(i * 0.35f, -0.55f, 1f)).normalized;
                Cone(body, name + "Claw" + (i + 1), clawPos, Quaternion.LookRotation(dir), 0.0065f, 0.020f, dark);
            }
        }

        static void BuildHead(Transform body, Material jade, Material dark, Material gold)
        {
            var head = new GameObject("Head");
            head.transform.SetParent(body, false);
            head.transform.localPosition = SpinePoint(1f);
            var gaze = (SpineTangent(1f) * 0.45f + Vector3.forward * 0.85f + Vector3.down * 0.12f).normalized;
            head.transform.localRotation = Quaternion.LookRotation(gaze);

            var h = head.transform;
            Part(h, "Neck", PrimitiveType.Sphere, new Vector3(0f, 0.004f, -0.016f), Quaternion.identity,
                new Vector3(0.042f, 0.042f, 0.048f), jade);
            Part(h, "Skull", PrimitiveType.Sphere, new Vector3(0f, 0.006f, 0.006f), Quaternion.identity,
                new Vector3(0.046f, 0.042f, 0.052f), jade);
            Part(h, "Snout", PrimitiveType.Sphere, new Vector3(0f, -0.006f, 0.040f), Quaternion.identity,
                new Vector3(0.034f, 0.028f, 0.044f), jade);
            Part(h, "Nose", PrimitiveType.Sphere, new Vector3(0f, -0.004f, 0.064f), Quaternion.identity,
                new Vector3(0.028f, 0.024f, 0.026f), jade);
            Part(h, "Jaw", PrimitiveType.Sphere, new Vector3(0f, -0.030f, 0.038f), Quaternion.identity,
                new Vector3(0.028f, 0.017f, 0.046f), dark);

            for (var side = -1; side <= 1; side += 2)
            {
                var s = side < 0 ? "L" : "R";
                Part(h, "Cheek" + s, PrimitiveType.Sphere, new Vector3(side * 0.015f, -0.006f, 0.018f),
                    Quaternion.identity, new Vector3(0.026f, 0.030f, 0.036f), jade);
                Part(h, "Brow" + s, PrimitiveType.Sphere, new Vector3(side * 0.021f, 0.026f, 0.026f),
                    Quaternion.identity, new Vector3(0.017f, 0.014f, 0.024f), dark);
                Part(h, "Eye" + s, PrimitiveType.Sphere, new Vector3(side * 0.026f, 0.017f, 0.032f),
                    Quaternion.identity, Vector3.one * 0.011f, gold);
                Part(h, "Nostril" + s, PrimitiveType.Sphere, new Vector3(side * 0.011f, 0.007f, 0.074f),
                    Quaternion.identity, Vector3.one * 0.006f, dark);

                BuildHorn(h, s, side, dark);
                BuildEar(h, s, side, dark);
                BuildWhisker(h, s, side, dark);
            }

            var maneBase = new Vector3(0f, 0.030f, -0.014f);
            for (var i = -2; i <= 2; i++)
            {
                var x = i * 0.011f;
                var pos = maneBase + new Vector3(x, -Mathf.Abs(x) * 0.35f, 0f);
                var dir = new Vector3(x * 4f, 0.75f, -0.75f).normalized;
                Cone(h, "Mane" + (i + 2), pos, Quaternion.LookRotation(dir), 0.0075f, 0.028f, dark);
            }
        }

        static void BuildHorn(Transform head, string suffix, int side, Material dark)
        {
            var p0 = new Vector3(side * 0.017f, 0.032f, -0.004f);
            var p1 = p0 + new Vector3(side * 0.022f, 0.044f, -0.038f);
            var p2 = p1 + new Vector3(side * 0.010f, 0.038f, -0.030f);
            var p3 = p2 + new Vector3(side * -0.010f, 0.030f, -0.018f);

            Limb(head, "HornA" + suffix, p0, p1, 0.0075f, 0.0055f, dark);
            Limb(head, "HornB" + suffix, p1, p2, 0.0055f, 0.0038f, dark);
            Limb(head, "HornC" + suffix, p2, p3, 0.0038f, 0.0024f, dark);
            Cone(head, "HornTip" + suffix, p3, Quaternion.LookRotation((p3 - p2).normalized), 0.0026f, 0.016f, dark);
        }

        static void BuildEar(Transform head, string suffix, int side, Material dark)
        {
            var pos = new Vector3(side * 0.033f, 0.014f, -0.014f);
            var dir = new Vector3(side * 0.75f, 0.35f, -0.55f).normalized;
            Cone(head, "Ear" + suffix, pos, Quaternion.LookRotation(dir), 0.011f, 0.030f, dark);
        }

        static void BuildWhisker(Transform head, string suffix, int side, Material dark)
        {
            var p0 = new Vector3(side * 0.022f, -0.012f, 0.058f);
            var p1 = p0 + new Vector3(side * 0.030f, -0.004f, -0.034f);
            var p2 = p1 + new Vector3(side * 0.026f, 0.004f, -0.030f);
            var p3 = p2 + new Vector3(side * 0.014f, -0.014f, -0.024f);
            Limb(head, "WhiskerA" + suffix, p0, p1, 0.0026f, 0.0021f, dark);
            Limb(head, "WhiskerB" + suffix, p1, p2, 0.0021f, 0.0015f, dark);
            Limb(head, "WhiskerC" + suffix, p2, p3, 0.0015f, 0.0008f, dark);
        }

        static void BuildPearl(Transform body, Material gold)
        {
            Part(body, "Pearl", PrimitiveType.Sphere, new Vector3(0f, 0.030f, 0.088f), Quaternion.identity,
                Vector3.one * 0.028f, gold);
        }

        static void FitCollider(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var rootT = root.transform;
            var size = bounds.size;
            var lossy = rootT.lossyScale;
            var localSize = new Vector3(
                lossy.x > 0.0001f ? size.x / lossy.x : size.x,
                lossy.y > 0.0001f ? size.y / lossy.y : size.y,
                lossy.z > 0.0001f ? size.z / lossy.z : size.z);

            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
                collider = root.AddComponent<BoxCollider>();
            collider.center = rootT.InverseTransformPoint(bounds.center);
            collider.size = localSize;
        }

        static float SpineRadius(float t)
        {
            return t < 0.45f
                ? Mathf.Lerp(0.007f, 0.031f, Smooth(t / 0.45f))
                : Mathf.Lerp(0.031f, 0.017f, Smooth((t - 0.45f) / 0.55f));
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        static Vector3 SpinePoint(float t)
        {
            t = Mathf.Clamp01(t);
            var x = t * (Spine.Length - 1);
            var i = Mathf.Clamp(Mathf.FloorToInt(x), 0, Spine.Length - 2);
            var u = x - i;
            var p0 = Spine[Mathf.Max(i - 1, 0)];
            var p1 = Spine[i];
            var p2 = Spine[i + 1];
            var p3 = Spine[Mathf.Min(i + 2, Spine.Length - 1)];
            return 0.5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (u * u) +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * (u * u * u));
        }

        static Vector3 SpineTangent(float t)
        {
            const float e = 0.004f;
            var a = SpinePoint(Mathf.Max(0f, t - e));
            var b = SpinePoint(Mathf.Min(1f, t + e));
            var d = b - a;
            return d.sqrMagnitude > 1e-10f ? d.normalized : Vector3.forward;
        }

        static Vector3 OrthoUp(Vector3 dir)
        {
            var up = Vector3.up - dir * Vector3.Dot(Vector3.up, dir);
            if (up.sqrMagnitude < 1e-6f)
                up = Vector3.forward - dir * Vector3.Dot(Vector3.forward, dir);
            return up.normalized;
        }

        static void Limb(Transform parent, string name, Vector3 a, Vector3 b, float rA, float rB, Material mat)
        {
            var d = b - a;
            var len = d.magnitude;
            if (len < 1e-5f)
                return;
            var r = (rA + rB) * 0.5f;
            Part(parent, name, PrimitiveType.Capsule, (a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, d / len),
                new Vector3(r * 2f, len * 0.5f, r * 2f), mat);
        }

        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPos,
            Quaternion localRot, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
            var col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
            return go;
        }

        static GameObject Cone(Transform parent, string name, Vector3 localPos, Quaternion localRot,
            float radius, float height, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = new Vector3(radius, radius, height);
            go.AddComponent<MeshFilter>().sharedMesh = UnitCone();
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Mesh UnitCone()
        {
            if (_unitCone != null)
                return _unitCone;

            const string path = MeshesFolder + "/JadeDragonCone.asset";
            _unitCone = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (_unitCone != null)
                return _unitCone;

            const int segments = 12;
            var vertices = new List<Vector3>(segments * 2 + 2);
            var triangles = new List<int>(segments * 6);

            vertices.Add(new Vector3(0f, 0f, 1f));
            for (var i = 0; i < segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
            }

            var capCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            var capRing = vertices.Count;
            for (var i = 0; i < segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
            }

            for (var i = 0; i < segments; i++)
            {
                var v1 = 1 + i;
                var v2 = 1 + (i + 1) % segments;
                triangles.Add(0);
                triangles.Add(v1);
                triangles.Add(v2);
                triangles.Add(capCenter);
                triangles.Add(capRing + (i + 1) % segments);
                triangles.Add(capRing + i);
            }

            _unitCone = new Mesh { name = "JadeDragonCone" };
            _unitCone.SetVertices(vertices);
            _unitCone.SetTriangles(triangles, 0);
            _unitCone.RecalculateNormals();
            _unitCone.RecalculateBounds();

            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "TableProps");
            if (!AssetDatabase.IsValidFolder(MeshesFolder))
                AssetDatabase.CreateFolder("Assets/GenJutsu/TableProps", "Meshes");
            AssetDatabase.CreateAsset(_unitCone, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        static Material LoadOrCreate(string name, Color color, bool emission, Color emissionColor)
        {
            var path = MaterialsFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;

            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "TableProps");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder("Assets/GenJutsu/TableProps", "Materials");

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(mat, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
