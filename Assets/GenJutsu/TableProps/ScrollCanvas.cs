using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.TableProps
{
    /// <summary>
    /// Texture on the scroll surface that receives brush strokes.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ScrollCanvas : MonoBehaviour
    {
        [SerializeField] int resolution = 512;
        [SerializeField] Color paperColor = new Color(0.93f, 0.88f, 0.72f, 1f);
        [SerializeField] Color inkColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        [SerializeField] bool sculptSheet = true;
        [SerializeField] float sheetRestLocalY = -1.25f;

        const float SheetThicknessLocal = 0.15f;
        const float SheetMaxTopLocal = 0.71f;
        const float ColliderBottomClearLocal = 0.006f;
        const float ColliderZMinLocal = -0.29f;
        const float ColliderZMaxLocal = 0.5f;
        const int Seed = 1907;

        static readonly Color32 AgedBrown = new Color32(176, 146, 100, 255);

        Texture2D texture;
        Texture2D normalTexture;
        Mesh sheetMesh;
        Color32[] paperPixels;
        Color32[] pixels;
        float[] heightField;
        Color32 ink32;
        Renderer targetRenderer;
        Bounds localBounds;
        MaterialPropertyBlock paintBlock;
        MaterialPropertyBlock probeBlock;

        public float WorldWidth { get; private set; } = 0.55f;
        public float WorldHeight { get; private set; } = 0.32f;

        void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            var scale = transform.lossyScale;
            WorldWidth = Mathf.Abs(scale.x);
            WorldHeight = Mathf.Abs(scale.z > 0.01f ? scale.z : scale.y);
            ink32 = inkColor;

            if (sculptSheet)
            {
                SculptSheet();
                FitCollider();
            }
            localBounds = ResolveLocalBounds();

            BuildPaper();
            texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false);

            normalTexture = BuildNormalMap();

            paintBlock = new MaterialPropertyBlock();
            probeBlock = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(paintBlock);
            paintBlock.SetTexture("_BaseMap", texture);
            paintBlock.SetColor("_BaseColor", Color.white);
            paintBlock.SetTexture("_BumpMap", normalTexture);
            paintBlock.SetFloat("_BumpScale", 0.45f);
            targetRenderer.SetPropertyBlock(paintBlock);

            var material = targetRenderer.material;
            if (material != null)
                material.EnableKeyword("_NORMALMAP");
        }

        void Update()
        {
            if (targetRenderer == null || paintBlock == null)
                return;
            probeBlock.Clear();
            targetRenderer.GetPropertyBlock(probeBlock);
            if (probeBlock.isEmpty)
                targetRenderer.SetPropertyBlock(paintBlock);
        }

        void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
            if (normalTexture != null)
                Destroy(normalTexture);
            if (sheetMesh != null)
                Destroy(sheetMesh);
        }

        public void Clear()
        {
            if (pixels == null || paperPixels == null || texture == null)
                return;
            System.Array.Copy(paperPixels, pixels, paperPixels.Length);
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        public bool TryPaintWorldPoint(Vector3 worldPoint, float brushRadiusWorld)
        {
            if (pixels == null)
                return false;

            var local = transform.InverseTransformPoint(worldPoint);
            float u;
            float v;
            if (Mathf.Abs(transform.lossyScale.z) >= Mathf.Abs(transform.lossyScale.y) * 0.5f)
            {
                u = (localBounds.max.x - local.x) / localBounds.size.x;
                v = (local.z - localBounds.min.z) / localBounds.size.z;
            }
            else
            {
                u = (localBounds.max.x - local.x) / localBounds.size.x;
                v = (local.y - localBounds.min.y) / localBounds.size.y;
            }

            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return false;

            var px = Mathf.Clamp(Mathf.RoundToInt(u * (resolution - 1)), 0, resolution - 1);
            var py = Mathf.Clamp(Mathf.RoundToInt(v * (resolution - 1)), 0, resolution - 1);
            var radiusPx = Mathf.Max(1, Mathf.RoundToInt(brushRadiusWorld / Mathf.Max(0.001f, WorldWidth) * resolution));
            StampInk(px, py, radiusPx);
            texture.SetPixels32(pixels);
            texture.Apply(false);
            return true;
        }

        void StampInk(int cx, int cy, int radius)
        {
            var res = resolution;
            var reach = Mathf.CeilToInt(radius * 1.4f);
            var coreR = Mathf.Max(1f, radius);
            for (var dy = -reach; dy <= reach; dy++)
            {
                var py = cy + dy;
                if (py < 0 || py >= res)
                    continue;
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var px = cx + dx;
                    if (px < 0 || px >= res)
                        continue;

                    var t = Mathf.Sqrt(dx * dx + dy * dy) / coreR;
                    float alpha;
                    if (t <= 1f)
                        alpha = 1f - SmoothStep(0.4f, 1f, t);
                    else if (t <= 1.4f)
                        alpha = (1.4f - t) / 0.4f * 0.16f;
                    else
                        continue;

                    var fiber = 0.72f + 0.56f * ValueNoise(px * 0.4f, py * 0.4f, Seed + 101);
                    alpha = Mathf.Clamp01(alpha * fiber);
                    if (alpha < 0.004f)
                        continue;

                    var i = py * res + px;
                    var dst = pixels[i];
                    pixels[i] = new Color32(
                        (byte)(dst.r + (ink32.r - dst.r) * alpha),
                        (byte)(dst.g + (ink32.g - dst.g) * alpha),
                        (byte)(dst.b + (ink32.b - dst.b) * alpha),
                        255);
                }
            }
        }

        void SculptSheet()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter == null)
                return;
            sheetMesh = BuildSheetMesh(36, 52);
            filter.sharedMesh = sheetMesh;
        }

        void FitCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
                return;
            var bottom = sheetRestLocalY + ColliderBottomClearLocal;
            var top = sheetRestLocalY + SheetMaxTopLocal;
            box.center = new Vector3(0f, (bottom + top) * 0.5f, (ColliderZMinLocal + ColliderZMaxLocal) * 0.5f);
            box.size = new Vector3(1f, top - bottom, ColliderZMaxLocal - ColliderZMinLocal);
        }

        Bounds ResolveLocalBounds()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                return filter.sharedMesh.bounds;
            return new Bounds(Vector3.zero, Vector3.one);
        }

        float SheetHeight(float x, float z)
        {
            var ax = Mathf.Abs(x) * 2f;
            var edge = SmoothStep(0f, 1f, (ax - 0.68f) / 0.32f);
            var curl = 0.45f * edge * edge;
            var wave = 0.07f * Mathf.Sin(x * 8.1f + 1.3f) * Mathf.Sin(z * 6.3f)
                     + 0.04f * Mathf.Sin(z * 11.7f + 0.6f) * (0.4f + ax * 0.6f);
            return sheetRestLocalY + SheetThicknessLocal + wave + curl;
        }

        Mesh BuildSheetMesh(int nx, int nz)
        {
            const int gridTris = 6;
            var cols = nx + 1;
            var rows = nz + 1;
            var gridCount = cols * rows;
            var perimeter = PerimeterIndices(nx, nz, cols);
            var skirtCount = perimeter.Length;

            var vertices = new Vector3[gridCount * 2 + skirtCount * 2];
            var uvs = new Vector2[vertices.Length];

            for (var iz = 0; iz < rows; iz++)
            {
                var z = (float)iz / nz - 0.5f;
                for (var ix = 0; ix < cols; ix++)
                {
                    var x = (float)ix / nx - 0.5f;
                    var i = iz * cols + ix;
                    var y = SheetHeight(x, z);
                    vertices[i] = new Vector3(x, y, z);
                    vertices[i + gridCount] = new Vector3(x, y - SheetThicknessLocal, z);
                    var uv = new Vector2(0.5f - x, z + 0.5f);
                    uvs[i] = uv;
                    uvs[i + gridCount] = uv;
                }
            }

            var skirtTop = gridCount * 2;
            var skirtBottom = skirtTop + skirtCount;
            var probeSkirt = 0;
            var probeZ = float.MinValue;
            for (var p = 0; p < skirtCount; p++)
            {
                var grid = perimeter[p];
                vertices[skirtTop + p] = vertices[grid];
                vertices[skirtBottom + p] = vertices[grid + gridCount];
                uvs[skirtTop + p] = uvs[grid];
                uvs[skirtBottom + p] = uvs[grid];
                if (vertices[grid].z > probeZ)
                {
                    probeZ = vertices[grid].z;
                    probeSkirt = p;
                }
            }

            var topTris = new int[nx * nz * gridTris];
            var t = 0;
            for (var iz = 0; iz < nz; iz++)
            {
                for (var ix = 0; ix < nx; ix++)
                {
                    var i = iz * cols + ix;
                    topTris[t++] = i;
                    topTris[t++] = i + cols + 1;
                    topTris[t++] = i + 1;
                    topTris[t++] = i;
                    topTris[t++] = i + cols;
                    topTris[t++] = i + cols + 1;
                }
            }

            var bottomTris = new int[topTris.Length];
            for (var i = 0; i < topTris.Length; i++)
                bottomTris[i] = topTris[i] + gridCount;

            var skirtTris = new int[skirtCount * 6];
            t = 0;
            for (var p = 0; p < skirtCount; p++)
            {
                var a = p;
                var b = (p + 1) % skirtCount;
                skirtTris[t++] = skirtTop + a;
                skirtTris[t++] = skirtTop + b;
                skirtTris[t++] = skirtBottom + b;
                skirtTris[t++] = skirtTop + a;
                skirtTris[t++] = skirtBottom + b;
                skirtTris[t++] = skirtBottom + a;
            }

            var topCount = topTris.Length;
            var bottomCount = bottomTris.Length;
            var triangles = new int[topCount + bottomCount + skirtTris.Length];
            System.Array.Copy(topTris, 0, triangles, 0, topCount);
            System.Array.Copy(bottomTris, 0, triangles, topCount, bottomCount);
            System.Array.Copy(skirtTris, 0, triangles, topCount + bottomCount, skirtTris.Length);

            var mesh = new Mesh { name = "ScrollSheet" };
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            var probeGrid = (nz / 2) * cols + nx / 2;
            if (mesh.normals[probeGrid].y < 0f)
                FlipTris(triangles, 0, topCount);
            if (mesh.normals[probeGrid + gridCount].y > 0f)
                FlipTris(triangles, topCount, bottomCount);
            var skirtStart = topCount + bottomCount;
            if (Vector3.Dot(mesh.normals[skirtTop + probeSkirt], Vector3.forward) < 0f)
                FlipTris(triangles, skirtStart, skirtTris.Length);

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static int[] PerimeterIndices(int nx, int nz, int cols)
        {
            var list = new List<int>(2 * nx + 2 * nz);
            for (var ix = 0; ix <= nx; ix++)
                list.Add(ix);
            for (var iz = 1; iz <= nz; iz++)
                list.Add(iz * cols + nx);
            for (var ix = nx - 1; ix >= 0; ix--)
                list.Add(nz * cols + ix);
            for (var iz = nz - 1; iz >= 1; iz--)
                list.Add(iz * cols);
            return list.ToArray();
        }

        static void FlipTris(int[] tris, int start, int count)
        {
            for (var i = start; i < start + count; i += 3)
            {
                var tmp = tris[i + 1];
                tris[i + 1] = tris[i + 2];
                tris[i + 2] = tmp;
            }
        }

        void BuildPaper()
        {
            var res = resolution;
            paperPixels = new Color32[res * res];
            pixels = new Color32[res * res];
            heightField = new float[res * res];
            var inv = 1f / (res - 1);

            for (var y = 0; y < res; y++)
            {
                var v = y * inv;
                for (var x = 0; x < res; x++)
                {
                    var u = x * inv;
                    var i = y * res + x;

                    var mottle = Fbm(u * 6.5f, v * 6.5f, 2, Seed);
                    var fiberH = Fbm(u * 20f, v * 240f, 2, Seed + 7);
                    var fiberV = Fbm(u * 260f, v * 18f, 2, Seed + 13);
                    var grain = fiberH * 0.62f + fiberV * 0.38f;
                    heightField[i] = (grain - 0.5f) + (mottle - 0.5f) * 0.6f;

                    var shade = 1f + (mottle - 0.5f) * 0.16f + (grain - 0.5f) * 0.12f;

                    var dist = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    var borderNoise = Fbm(u * 30f, v * 30f, 1, Seed + 29);
                    var edge = 1f - Mathf.Clamp01(dist / (0.045f + 0.03f * borderNoise));
                    edge *= edge;

                    var blotch = Fbm(u * 3.2f + 11f, v * 3.2f + 7f, 2, Seed + 21);
                    var age = Mathf.Clamp01((blotch - 0.58f) * 3.4f);

                    var speck = Fbm(u * 150f, v * 150f, 1, Seed + 53);
                    var speckAmt = Mathf.Clamp01((speck - 0.86f) * 5f) * 0.55f;

                    var c = new Color(paperColor.r * shade, paperColor.g * shade, paperColor.b * shade, 1f);
                    var brown = Mathf.Clamp01(age * 0.3f + edge * 0.35f + speckAmt * 0.5f);
                    c.r = Mathf.Lerp(c.r, AgedBrown.r / 255f, brown);
                    c.g = Mathf.Lerp(c.g, AgedBrown.g / 255f, brown);
                    c.b = Mathf.Lerp(c.b, AgedBrown.b / 255f, brown);
                    paperPixels[i] = c;
                }
            }

            System.Array.Copy(paperPixels, pixels, paperPixels.Length);
        }

        Texture2D BuildNormalMap()
        {
            var res = resolution;
            var map = new Texture2D(res, res, TextureFormat.RGBA32, false, true);
            map.filterMode = FilterMode.Bilinear;
            map.wrapMode = TextureWrapMode.Clamp;

            const float strength = 2.2f;
            var data = new Color32[res * res];
            for (var y = 0; y < res; y++)
            {
                var up = Mathf.Min(y + 1, res - 1) * res;
                var down = Mathf.Max(y - 1, 0) * res;
                var row = y * res;
                for (var x = 0; x < res; x++)
                {
                    var left = Mathf.Max(x - 1, 0);
                    var right = Mathf.Min(x + 1, res - 1);
                    var nx = (heightField[row + left] - heightField[row + right]) * strength;
                    var ny = (heightField[down + x] - heightField[up + x]) * strength;
                    var nz = 1f / Mathf.Sqrt(nx * nx + ny * ny + 1f);
                    data[row + x] = new Color32(
                        (byte)(Mathf.Clamp01(nx * nz * 0.5f + 0.5f) * 255f),
                        (byte)(Mathf.Clamp01(ny * nz * 0.5f + 0.5f) * 255f),
                        (byte)(Mathf.Clamp01(nz * 0.5f + 0.5f) * 255f),
                        255);
                }
            }

            map.SetPixels32(data);
            map.Apply(false);
            return map;
        }

        static float SmoothStep(float a, float b, float x)
        {
            if (a >= b)
                return x >= b ? 1f : 0f;
            var t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float x, float y, int octaves, int seed)
        {
            var sum = 0f;
            var amp = 0.5f;
            var norm = 0f;
            for (var i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x, y, seed + i * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                x *= 2f;
                y *= 2f;
            }
            return norm > 0f ? sum / norm : 0f;
        }

        static float ValueNoise(float x, float y, int seed)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var fx = x - x0;
            var fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            var a = Hash(x0, y0, seed);
            var b = Hash(x0 + 1, y0, seed);
            var c = Hash(x0, y0 + 1, seed);
            var d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Hash(int x, int y, int seed)
        {
            var h = x * 374761393 + y * 668265263 + seed * 1013904223;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / 2147483647f;
        }
    }
}
