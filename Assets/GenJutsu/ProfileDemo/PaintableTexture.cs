using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// CPU-side texture that activities paint into (sand grooves, paint splats). Tracks how much of it was touched.
    /// </summary>
    public sealed class PaintableTexture
    {
        public Texture2D Texture { get; }
        public int Width { get; }
        public int Height { get; }

        readonly Color32[] pixels;
        readonly Color32[] background;
        readonly bool[] touched;
        int touchedCount;
        bool dirty;

        public float Coverage => touchedCount / (float)(Width * Height);

        public PaintableTexture(int width, int height, string name, System.Func<int, int, Color> backgroundColor)
        {
            Width = width;
            Height = height;
            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            pixels = new Color32[width * height];
            background = new Color32[width * height];
            touched = new bool[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    background[y * width + x] = backgroundColor(x, y);
            }

            Clear();
        }

        public void Clear()
        {
            System.Array.Copy(background, pixels, pixels.Length);
            System.Array.Clear(touched, 0, touched.Length);
            touchedCount = 0;
            dirty = true;
            ApplyIfDirty();
        }

        public void Stamp(Vector2 uv, float radius, Color color, float strength)
        {
            var cx = uv.x * Width;
            var cy = uv.y * Height;
            var x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            var x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + radius));
            var y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            var y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + radius));
            var r2 = radius * radius;
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var dx = x + 0.5f - cx;
                    var dy = y + 0.5f - cy;
                    var d2 = dx * dx + dy * dy;
                    if (d2 > r2)
                        continue;
                    var edge = 1f - Mathf.SmoothStep(0.6f, 1f, Mathf.Sqrt(d2) / radius);
                    var i = y * Width + x;
                    pixels[i] = Color32.Lerp(pixels[i], color, strength * edge);
                    if (!touched[i] && edge > 0.3f)
                    {
                        touched[i] = true;
                        touchedCount++;
                    }
                }
            }

            dirty = true;
        }

        public void Line(Vector2 from, Vector2 to, float radius, Color color, float strength)
        {
            var pixelDistance = Vector2.Distance(new Vector2(from.x * Width, from.y * Height), new Vector2(to.x * Width, to.y * Height));
            var steps = Mathf.Clamp(Mathf.CeilToInt(pixelDistance / Mathf.Max(1f, radius * 0.5f)), 1, 64);
            for (var s = 0; s <= steps; s++)
                Stamp(Vector2.Lerp(from, to, s / (float)steps), radius, color, strength);
        }

        public void ApplyIfDirty()
        {
            if (!dirty)
                return;
            Texture.SetPixels32(pixels);
            Texture.Apply(false);
            dirty = false;
        }
    }
}
