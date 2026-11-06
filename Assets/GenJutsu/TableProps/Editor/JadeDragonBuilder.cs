using System.Collections.Generic;
using System.IO;
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
        const string VeinPath = MaterialsFolder + "/JadeVeins.png";
        const string SculptureName = "JadeDragonSculpture";

        public static readonly Color JadeBase = new Color(0.10f, 0.38f, 0.20f, 1f);
        public static readonly Color JadeGlow = new Color(0.02f, 0.05f, 0.025f, 1f);
        public static readonly Color JadeDarkBase = new Color(0.04f, 0.11f, 0.07f, 1f);
        public const float JadeSmoothness = 0.92f;
        public const float JadeDarkSmoothness = 0.45f;

        const int BodyStations = 52;
        const int BodyRadial = 20;

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

        enum UvMode
        {
            Scales,
            Cloud
        }

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

            var jade = LoadOrCreate("Jade", JadeBase, true, JadeGlow);
            var gold = LoadOrCreate("ScrollGold", new Color(0.85f, 0.7f, 0.25f), false, Color.black);

            var root = Build(parent, localPos, jade, gold);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Rebuilt " + DragonName + " parts=" + (root.GetComponentsInChildren<Transform>(true).Length - 1);
        }

        public static GameObject Build(Transform parent, Vector3 localPos, Material jade, Material gold)
        {
            jade = ApplyJade(jade);
            var dark = ApplyJadeDark();

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
            var draft = new Draft();
            var neck = BuildCoil(draft);
            BuildHead(draft, body, neck, gold, dark);
            BuildLegs(draft, body, dark);
            BuildPlinth(draft);

            var mesh = SaveMesh(draft.FinalizeMesh(), SculptureName);
            var sculpture = new GameObject("Sculpture");
            sculpture.transform.SetParent(body, false);
            sculpture.AddComponent<MeshFilter>().sharedMesh = mesh;
            sculpture.AddComponent<MeshRenderer>().sharedMaterials = new[] { jade, dark };

            BuildPearl(body, gold);
            FitCollider(root);

            JapaneseTableBuilder.MakeGrabbable(root, 0.35f);
            JapaneseTableBuilder.AddRespawn(root);
            return root;
        }

        static Frame BuildCoil(Draft draft)
        {
            var frames = new Frame[BodyStations];
            var tangent0 = SpineTangent(0f);
            var up0 = OrthoUp(tangent0);
            for (var i = 0; i < BodyStations; i++)
            {
                var t = i / (BodyStations - 1f) * 0.985f;
                frames[i] = FrameAt(t, i == 0 ? up0 : frames[i - 1].up, i == 0 ? tangent0 : frames[i - 1].tangent);
            }

            var centers = new Vector3[BodyStations];
            var rx = new float[BodyStations];
            var ry = new float[BodyStations];
            for (var i = 0; i < BodyStations; i++)
            {
                centers[i] = frames[i].position;
                var radius = frames[i].radius * 1.12f;
                rx[i] = radius * 1.06f;
                ry[i] = radius * 0.78f;
            }

            draft.Loft(draft.Jade, centers, rx, ry, BodyRadial, UvMode.Scales, true, true, true, frames[0].up, 0f);

            var ridgeCount = 0;
            for (var i = 0; i < BodyStations; i++)
            {
                var t = i / (BodyStations - 1f);
                if (t >= 0.05f && t <= 0.93f)
                    ridgeCount++;
            }

            var ridgeC = new Vector3[ridgeCount];
            var ridgeR = new float[ridgeCount];
            var ridgeU = new float[ridgeCount];
            var ridgeUp = frames[0].up;
            var n = 0;
            for (var i = 0; i < BodyStations; i++)
            {
                var t = i / (BodyStations - 1f);
                if (t < 0.05f || t > 0.93f)
                    continue;
                var height = 0.004f + 0.010f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
                ridgeC[n] = frames[i].position + frames[i].up * (ry[i] + height * 0.45f);
                ridgeR[n] = Mathf.Max(0.0016f, rx[i] * 0.14f);
                ridgeU[n] = height * 0.55f;
                if (n == 0)
                    ridgeUp = frames[i].up;
                n++;
            }

            draft.Loft(draft.Dark, ridgeC, ridgeR, ridgeU, 4, UvMode.Cloud, true, true, false, ridgeUp, 0.2f);

            var tail = frames[0];
            var tailDir = -tail.tangent;
            for (var i = -1; i <= 1; i++)
            {
                var dir = (tailDir + tail.right * (i * 0.55f) + tail.up * 0.35f).normalized;
                var side = Vector3.Cross(dir, tail.up).normalized;
                draft.AddBlade(draft.Jade, tail.position + tail.up * (ry[0] * 0.2f), dir, side, 0.0045f, 0.028f);
            }

            return frames[BodyStations - 1];
        }

        static void BuildHead(Draft draft, Transform body, Frame neck, Material gold, Material dark)
        {
            var origin = SpinePoint(1f);
            var gaze = (SpineTangent(1f) * 0.45f + Vector3.forward * 0.85f + Vector3.down * 0.12f).normalized;
            var rot = Quaternion.LookRotation(gaze, neck.up);
            var headUp = rot * Vector3.up;

            var head = new GameObject("Head");
            head.transform.SetParent(body, false);
            head.transform.localPosition = origin;
            head.transform.localRotation = rot;

            LoftLocal(draft, draft.Jade, origin, rot, headUp, new[]
            {
                new Vector3(0f, 0.004f, -0.036f),
                new Vector3(0f, 0.009f, -0.008f),
                new Vector3(0f, 0.011f, 0.014f),
                new Vector3(0f, 0.002f, 0.034f),
                new Vector3(0f, -0.004f, 0.052f),
                new Vector3(0f, -0.001f, 0.070f)
            }, new[] { 0.021f, 0.027f, 0.031f, 0.018f, 0.013f, 0.016f }, new[] { 0.018f, 0.022f, 0.024f, 0.014f, 0.011f, 0.013f }, 16, true);

            LoftLocal(draft, draft.Jade, origin, rot, headUp, new[]
            {
                new Vector3(0f, -0.018f, 0.008f),
                new Vector3(0f, -0.030f, 0.030f),
                new Vector3(0f, -0.022f, 0.050f),
                new Vector3(0f, -0.012f, 0.064f)
            }, new[] { 0.017f, 0.015f, 0.011f, 0.008f }, new[] { 0.008f, 0.007f, 0.005f, 0.004f }, 12, true);

            LoftLocal(draft, draft.Dark, origin, rot, headUp, new[]
            {
                new Vector3(0f, -0.010f, 0.020f),
                new Vector3(0f, -0.012f, 0.040f),
                new Vector3(0f, -0.008f, 0.054f)
            }, new[] { 0.012f, 0.010f, 0.006f }, new[] { 0.004f, 0.003f, 0.002f }, 10, true);

            for (var side = -1; side <= 1; side += 2)
            {
                var s = side < 0 ? "L" : "R";
                Part(head.transform, "Eye" + s, PrimitiveType.Sphere, new Vector3(side * 0.026f, 0.016f, 0.030f),
                    Quaternion.identity, Vector3.one * 0.010f, gold);

                var brow = new[]
                {
                    new Vector3(side * 0.010f, 0.026f, 0.010f),
                    new Vector3(side * 0.024f, 0.024f, 0.028f),
                    new Vector3(side * 0.016f, 0.016f, 0.046f)
                };
                LoftLocal(draft, draft.Dark, origin, rot, headUp, brow,
                    new[] { 0.0055f, 0.0045f, 0.0028f }, new[] { 0.004f, 0.0034f, 0.0022f }, 8, true);

                var horn = new[]
                {
                    new Vector3(side * 0.017f, 0.032f, -0.004f),
                    new Vector3(side * 0.039f, 0.076f, -0.042f),
                    new Vector3(side * 0.049f, 0.114f, -0.072f),
                    new Vector3(side * 0.039f, 0.144f, -0.090f)
                };
                LoftLocal(draft, draft.Dark, origin, rot, headUp, horn,
                    new[] { 0.0075f, 0.0055f, 0.0038f, 0.0022f }, null, 8, true);

                var whisker = new[]
                {
                    new Vector3(side * 0.022f, -0.012f, 0.058f),
                    new Vector3(side * 0.052f, -0.016f, 0.024f),
                    new Vector3(side * 0.078f, -0.012f, -0.006f),
                    new Vector3(side * 0.092f, -0.026f, -0.030f)
                };
                LoftLocal(draft, draft.Dark, origin, rot, headUp, whisker,
                    new[] { 0.0026f, 0.0021f, 0.0015f, 0.0009f }, null, 6, true);

                var nostrilPos = new Vector3(side * 0.008f, 0.007f, 0.066f);
                var nostrilDir = new Vector3(side * 0.45f, 0.35f, 1f).normalized;
                LoftLocal(draft, draft.Dark, origin, rot, headUp, new[]
                {
                    nostrilPos,
                    nostrilPos + nostrilDir * 0.010f
                }, new[] { 0.0042f, 0.0012f }, null, 7, true);
            }

            for (var i = -2; i <= 2; i++)
            {
                var x = i * 0.011f;
                var pos = origin + rot * (new Vector3(0f, 0.030f, -0.014f) + new Vector3(x, -Mathf.Abs(x) * 0.35f, 0f));
                var dir = rot * new Vector3(x * 4f, 0.75f, -0.75f).normalized;
                var side = rot * Vector3.right;
                draft.AddBlade(draft.Dark, pos, dir, side, 0.004f, 0.030f);
            }
        }

        static void BuildLegs(Draft draft, Transform body, Material dark)
        {
            for (var i = 0; i < Legs.Length; i++)
            {
                var leg = Legs[i];
                var hip = PullIntoBody(leg[0]);
                var keys = new[] { hip, leg[1], leg[2] };
                var r0 = LegRadius[i];
                var radii = new[] { r0, r0 * 0.72f, r0 * 0.48f };
                var centers = new List<Vector3>();
                var rad = new List<float>();
                AppendSmooth(centers, rad, keys, radii, 4);
                var up = OrthoUp((leg[1] - hip).normalized);
                draft.Loft(draft.Jade, centers.ToArray(), rad.ToArray(), rad.ToArray(), 10, UvMode.Cloud, true, false, false, up, 0.05f * i);

                var forward = FootForward[i].normalized;
                var footUp = OrthoUp(forward);
                const int footRings = 4;
                var footC = new Vector3[footRings];
                var footX = new float[footRings];
                var footY = new float[footRings];
                for (var s = 0; s < footRings; s++)
                {
                    var t = s / (footRings - 1f);
                    footC[s] = leg[2] + forward * (t * 0.046f) - footUp * (0.002f * t);
                    footX[s] = Mathf.Lerp(r0 * 0.62f, 0.012f, t);
                    footY[s] = Mathf.Lerp(r0 * 0.42f, 0.005f, t);
                    if (s == 1)
                    {
                        footX[s] = 0.024f;
                        footY[s] = 0.009f;
                    }
                }

                draft.Loft(draft.Jade, footC, footX, footY, 10, UvMode.Cloud, false, true, false, footUp, 0.1f + i * 0.04f);

                var rot = Quaternion.LookRotation(forward, footUp);
                for (var c = -1; c <= 1; c++)
                {
                    var clawPos = leg[2] + rot * new Vector3(c * 0.015f, -0.003f, 0.030f);
                    var dir = (rot * new Vector3(c * 0.35f, -0.55f, 1f)).normalized;
                    Cone(body, "Foot" + i + "Claw" + (c + 1), clawPos, Quaternion.LookRotation(dir), 0.0065f, 0.020f, dark);
                }
            }
        }

        static void BuildPlinth(Draft draft)
        {
            var y = new[] { 0f, 0.003f, 0.010f, 0.015f, 0.019f };
            var r = new[] { 0.100f, 0.112f, 0.110f, 0.098f, 0.094f };
            var centers = new Vector3[y.Length];
            for (var i = 0; i < y.Length; i++)
                centers[i] = new Vector3(0f, y[i], 0f);
            draft.Loft(draft.Jade, centers, r, r, 28, UvMode.Cloud, true, true, false, Vector3.forward, 0.4f);
        }

        static void LoftLocal(Draft draft, List<int> sub, Vector3 origin, Quaternion rot, Vector3 up, Vector3[] local, float[] rx, float[] ry, int radial, bool cap)
        {
            var centers = new List<Vector3>();
            var radX = new List<float>();
            var radY = new List<float>();
            var srcY = ry ?? rx;
            AppendSmooth(centers, radX, local, rx, 3);
            var tmp = new List<Vector3>();
            AppendSmooth(tmp, radY, local, srcY, 3);
            var world = new Vector3[centers.Count];
            for (var i = 0; i < centers.Count; i++)
                world[i] = origin + rot * centers[i];
            draft.Loft(sub, world, radX.ToArray(), radY.ToArray(), radial, UvMode.Cloud, cap, cap, false, up, 0.15f);
        }

        static void BuildPearl(Transform body, Material gold)
        {
            Part(body, "Pearl", PrimitiveType.Sphere, new Vector3(0f, 0.030f, 0.088f), Quaternion.identity,
                Vector3.one * 0.028f, gold);
        }

        static Vector3 PullIntoBody(Vector3 p)
        {
            var best = float.MaxValue;
            var bestT = 0.5f;
            for (var i = 0; i <= 24; i++)
            {
                var t = i / 24f * 0.985f;
                var d = (SpinePoint(t) - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestT = t;
                }
            }

            var center = SpinePoint(bestT);
            var delta = p - center;
            var dist = delta.magnitude;
            if (dist < 1e-5f)
                return p;
            var embed = SpineRadius(bestT) * 0.55f;
            return dist > embed ? center + delta / dist * embed : p;
        }

        static Frame FrameAt(float t, Vector3 previousUp, Vector3 previousTangent)
        {
            var position = SpinePoint(t);
            var tangent = SpineTangent(t);
            var up = previousUp;
            var axis = Vector3.Cross(previousTangent, tangent);
            var mag = axis.magnitude;
            if (mag > 1e-5f)
                up = Quaternion.AngleAxis(Mathf.Asin(Mathf.Clamp(mag, 0f, 1f)) * Mathf.Rad2Deg, axis / mag) * up;
            up = up - tangent * Vector3.Dot(up, tangent);
            if (up.sqrMagnitude < 1e-6f)
                up = OrthoUp(tangent);
            up.Normalize();
            var right = Vector3.Cross(up, tangent).normalized;
            up = Vector3.Cross(tangent, right).normalized;
            return new Frame
            {
                position = position,
                tangent = tangent,
                up = up,
                right = right,
                radius = SpineRadius(t)
            };
        }

        struct Frame
        {
            public Vector3 position;
            public Vector3 tangent;
            public Vector3 up;
            public Vector3 right;
            public float radius;
        }

        static void AppendSmooth(List<Vector3> dst, List<float> dstR, Vector3[] pts, float[] radii, int divisions)
        {
            var segs = pts.Length - 1;
            for (var s = 0; s < segs; s++)
            {
                var p0 = pts[Mathf.Max(s - 1, 0)];
                var p1 = pts[s];
                var p2 = pts[s + 1];
                var p3 = pts[Mathf.Min(s + 2, pts.Length - 1)];
                var start = s == 0 ? 0 : 1;
                for (var i = start; i <= divisions; i++)
                {
                    var u = i / (float)divisions;
                    dst.Add(Catmull(p0, p1, p2, p3, u));
                    dstR.Add(Mathf.Lerp(radii[s], radii[s + 1], u));
                }
            }
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

        static Vector3 Catmull(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            var u2 = u * u;
            var u3 = u2 * u;
            return 0.5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
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
            return Catmull(p0, p1, p2, p3, u);
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

            EnsureFolder(MeshesFolder, "Meshes");
            AssetDatabase.CreateAsset(_unitCone, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        static Material ApplyJade(Material mat)
        {
            if (mat == null)
                mat = LoadOrCreate("Jade", JadeBase, true, JadeGlow);
            var veins = LoadOrCreateVeins();
            ApplyLit(mat, JadeBase, 0f, JadeSmoothness, true, JadeGlow, veins);
            return mat;
        }

        static Material ApplyJadeDark()
        {
            var mat = LoadOrCreate("JadeDark", JadeDarkBase, false, Color.black);
            ApplyLit(mat, JadeDarkBase, 0f, JadeDarkSmoothness, false, Color.black, null);
            return mat;
        }

        static void ApplyLit(Material mat, Color color, float metallic, float smoothness, bool emission, Color emissionColor, Texture albedo)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader != null && mat.shader != shader)
                mat.shader = shader;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);

            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", albedo);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", albedo);
            }

            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            EditorUtility.SetDirty(mat);
        }

        static Texture2D LoadOrCreateVeins()
        {
            EnsureFolder(MaterialsFolder, "Materials");
            var full = Path.Combine(Application.dataPath, "GenJutsu/TableProps/Materials/JadeVeins.png");
            File.WriteAllBytes(full, EncodeVeins());
            AssetDatabase.ImportAsset(VeinPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(VeinPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(VeinPath);
        }

        static byte[] EncodeVeins()
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color32[n * n];
            for (var y = 0; y < n; y++)
            {
                var v = y / (float)n;
                var edgeV = Mathf.SmoothStep(0.14f, 0f, Mathf.Min(v, 1f - v));
                for (var x = 0; x < n; x++)
                {
                    var u = x / (float)n;
                    var cloud = Noise(u * 3.5f, v * 3.5f) * 0.7f + Noise(u * 8f + 4.2f, v * 8f + 1.7f) * 0.3f;
                    var shade = Mathf.Lerp(0.82f, 1f, cloud);
                    var edgeU = Mathf.SmoothStep(0.10f, 0f, Mathf.Min(u, 1f - u));
                    var groove = Mathf.Clamp01(Mathf.Max(edgeU, edgeV));
                    shade = Mathf.Lerp(shade, 0.58f, groove);
                    pixels[y * n + x] = new Color(shade * 0.94f, shade, shade * 0.90f, 1f);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false);
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        static float Noise(float x, float y)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = x - x0;
            var ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            var a = Hash(x0, y0);
            var b = Hash(x0 + 1, y0);
            var c = Hash(x0, y0 + 1);
            var d = Hash(x0 + 1, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                var n = (uint)(x * 374761393 + y * 668265263);
                n = (n ^ (n >> 13)) * 1274126177u;
                return (n & 0x00ffffff) / 16777215f;
            }
        }

        static Material LoadOrCreate(string name, Color color, bool emission, Color emissionColor)
        {
            var path = MaterialsFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;

            EnsureFolder(MaterialsFolder, "Materials");
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

        static Mesh SaveMesh(Mesh source, string assetName)
        {
            source.name = assetName;
            EnsureFolder(MeshesFolder, "Meshes");
            var path = MeshesFolder + "/" + assetName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, path);
                return source;
            }

            existing.Clear(false);
            existing.name = assetName;
            existing.SetVertices(source.vertices);
            existing.SetNormals(source.normals);
            existing.uv = source.uv;
            existing.subMeshCount = source.subMeshCount;
            for (var i = 0; i < source.subMeshCount; i++)
                existing.SetTriangles(source.GetTriangles(i), i, false);
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(source);
            return existing;
        }

        static void EnsureFolder(string path, string leaf)
        {
            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/TableProps"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "TableProps");
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/GenJutsu/TableProps", leaf);
        }

        sealed class Draft
        {
            public readonly List<int> Jade = new List<int>();
            public readonly List<int> Dark = new List<int>();
            readonly List<Vector3> _verts = new List<Vector3>();
            readonly List<Vector2> _uvs = new List<Vector2>();
            readonly List<(int a, int b)> _seams = new List<(int, int)>();
            readonly List<(int index, Vector3 outward)> _probes = new List<(int, Vector3)>();

            public void Loft(List<int> sub, Vector3[] centers, float[] rx, float[] ry, int radial, UvMode uv,
                bool capStart, bool capEnd, bool grooves, Vector3 initialUp, float uvShift)
            {
                var n = centers.Length;
                if (n < 2 || radial < 3)
                    return;

                var tangents = new Vector3[n];
                for (var i = 0; i < n; i++)
                {
                    var a = centers[Mathf.Max(i - 1, 0)];
                    var b = centers[Mathf.Min(i + 1, n - 1)];
                    var d = b - a;
                    tangents[i] = d.sqrMagnitude > 1e-10f ? d.normalized : Vector3.forward;
                }

                var up = new Vector3[n];
                var right = new Vector3[n];
                up[0] = initialUp.sqrMagnitude > 1e-6f ? initialUp : OrthoUp(tangents[0]);
                up[0] = up[0] - tangents[0] * Vector3.Dot(up[0], tangents[0]);
                if (up[0].sqrMagnitude < 1e-6f)
                    up[0] = OrthoUp(tangents[0]);
                up[0].Normalize();
                right[0] = Vector3.Cross(up[0], tangents[0]).normalized;
                up[0] = Vector3.Cross(tangents[0], right[0]).normalized;

                for (var i = 1; i < n; i++)
                {
                    var axis = Vector3.Cross(tangents[i - 1], tangents[i]);
                    var mag = axis.magnitude;
                    up[i] = up[i - 1];
                    if (mag > 1e-5f)
                        up[i] = Quaternion.AngleAxis(Mathf.Asin(Mathf.Clamp(mag, 0f, 1f)) * Mathf.Rad2Deg, axis / mag) * up[i];
                    up[i] = up[i] - tangents[i] * Vector3.Dot(up[i], tangents[i]);
                    if (up[i].sqrMagnitude < 1e-6f)
                        up[i] = OrthoUp(tangents[i]);
                    up[i].Normalize();
                    right[i] = Vector3.Cross(up[i], tangents[i]).normalized;
                    up[i] = Vector3.Cross(tangents[i], right[i]).normalized;
                }

                var ring = radial + 1;
                var baseVert = _verts.Count;
                for (var s = 0; s < n; s++)
                {
                    var t = s / (float)(n - 1);
                    for (var i = 0; i <= radial; i++)
                    {
                        var a = i / (float)radial * Mathf.PI * 2f;
                        var scale = 1f;
                        if (grooves)
                        {
                            var dorsal = Mathf.Clamp01(Mathf.Sin(a));
                            var along = Mathf.Repeat(t * 18f, 1f);
                            var dome = Mathf.Sin(along * Mathf.PI);
                            var fade = Mathf.SmoothStep(0f, 0.08f, t) * (1f - Mathf.SmoothStep(0.88f, 0.98f, t));
                            scale = 1f - 0.08f * dorsal * (1f - dome) * fade;
                        }

                        var p = centers[s] + (right[s] * Mathf.Cos(a) * rx[s] + up[s] * Mathf.Sin(a) * ry[s]) * scale;
                        _verts.Add(p);
                        _uvs.Add(Uv(uv, a, t, i, radial, uvShift));
                    }

                    _seams.Add((baseVert + s * ring, baseVert + s * ring + radial));
                }

                for (var s = 0; s < n - 1; s++)
                {
                    var r0 = baseVert + s * ring;
                    var r1 = r0 + ring;
                    for (var i = 0; i < radial; i++)
                    {
                        var a = r0 + i;
                        var b = r0 + i + 1;
                        var c = r1 + i;
                        var d = r1 + i + 1;
                        AddTri(sub, a, c, b);
                        AddTri(sub, b, c, d);
                    }
                }

                _probes.Add((baseVert, right[0]));
                if (capStart)
                    Cap(sub, baseVert, radial, centers[0], true);
                if (capEnd)
                    Cap(sub, baseVert + (n - 1) * ring, radial, centers[n - 1], false);
            }

            public void AddBlade(List<int> sub, Vector3 origin, Vector3 dir, Vector3 side, float halfWidth, float length)
            {
                if (dir.sqrMagnitude < 1e-8f || side.sqrMagnitude < 1e-8f)
                    return;
                dir.Normalize();
                side.Normalize();
                var p0 = origin - side * halfWidth;
                var p1 = origin + side * halfWidth;
                var tip = origin + dir * length;
                var a = AddVert(p0, new Vector2(0.42f, 0.42f));
                var b = AddVert(p1, new Vector2(0.58f, 0.42f));
                var c = AddVert(tip, new Vector2(0.5f, 0.62f));
                AddTri(sub, a, b, c);
                var d = AddVert(p0, new Vector2(0.42f, 0.42f));
                var e = AddVert(p1, new Vector2(0.58f, 0.42f));
                var f = AddVert(tip, new Vector2(0.5f, 0.62f));
                AddTri(sub, d, f, e);
            }

            public Mesh FinalizeMesh()
            {
                var mesh = new Mesh { name = SculptureName };
                mesh.SetVertices(_verts);
                mesh.SetUVs(0, _uvs);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(Jade, 0, false);
                mesh.SetTriangles(Dark, 1, false);
                mesh.RecalculateNormals();
                if (ProbeScore(mesh.normals) < 0f)
                {
                    Flip(mesh);
                    mesh.RecalculateNormals();
                }

                Weld(mesh);
                mesh.RecalculateBounds();
                return mesh;
            }

            float ProbeScore(Vector3[] normals)
            {
                if (_probes.Count == 0 || normals == null || normals.Length == 0)
                    return 1f;
                var score = 0f;
                foreach (var probe in _probes)
                {
                    if (probe.index >= 0 && probe.index < normals.Length)
                        score += Vector3.Dot(normals[probe.index], probe.outward);
                }

                return score;
            }

            void Weld(Mesh mesh)
            {
                var normals = mesh.normals;
                foreach (var seam in _seams)
                {
                    if (seam.a < 0 || seam.b < 0 || seam.a >= normals.Length || seam.b >= normals.Length)
                        continue;
                    var nrm = (normals[seam.a] + normals[seam.b]).normalized;
                    normals[seam.a] = nrm;
                    normals[seam.b] = nrm;
                }

                mesh.normals = normals;
            }

            static void Flip(Mesh mesh)
            {
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    var tris = mesh.GetTriangles(s);
                    for (var i = 0; i < tris.Length; i += 3)
                    {
                        var tmp = tris[i + 1];
                        tris[i + 1] = tris[i + 2];
                        tris[i + 2] = tmp;
                    }

                    mesh.SetTriangles(tris, s, false);
                }
            }

            void Cap(List<int> sub, int ringStart, int radial, Vector3 center, bool start)
            {
                var c = AddVert(center, new Vector2(0.5f, 0.5f));
                for (var i = 0; i < radial; i++)
                {
                    var a = ringStart + i;
                    var b = ringStart + i + 1;
                    if (start)
                        AddTri(sub, c, b, a);
                    else
                        AddTri(sub, c, a, b);
                }
            }

            int AddVert(Vector3 p, Vector2 uv)
            {
                _verts.Add(p);
                _uvs.Add(uv);
                return _verts.Count - 1;
            }

            static void AddTri(List<int> sub, int a, int b, int c)
            {
                sub.Add(a);
                sub.Add(b);
                sub.Add(c);
            }

            static Vector2 Uv(UvMode mode, float a, float t, int i, int radial, float uvShift)
            {
                if (mode == UvMode.Scales)
                    return new Vector2(i / (float)radial * 7f, t * 18f + uvShift);
                return new Vector2(0.5f + 0.2f * Mathf.Cos(a), 0.5f + 0.12f * Mathf.Sin(a) + (t - 0.5f) * 0.16f + uvShift);
            }
        }
    }
}
