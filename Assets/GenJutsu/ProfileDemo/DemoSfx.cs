using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Small synthesized sound effects played through a fixed pool of 3D sources.
    /// </summary>
    public static class DemoSfx
    {
        public enum Kind
        {
            Chime,
            Whoosh,
            Pop,
            Splat,
            Bowl,
            Note,
            Success,
            Error,
            Shutter,
            Purr,
            Crackle,
            Water,
            Thud
        }

        const int Rate = 22050;
        const int PoolSize = 10;

        static AudioSource[] pool;
        static int next;
        static readonly AudioClip[] clips = new AudioClip[System.Enum.GetValues(typeof(Kind)).Length];

        public static float Semitones(float semitones)
        {
            return Mathf.Pow(2f, semitones / 12f);
        }

        public static void Play(Kind kind, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            EnsurePool();
            var source = pool[next];
            for (var i = 0; i < PoolSize; i++)
            {
                var candidate = pool[(next + i) % PoolSize];
                if (!candidate.isPlaying)
                {
                    source = candidate;
                    next = (next + i) % PoolSize;
                    break;
                }
            }

            next = (next + 1) % PoolSize;
            source.transform.position = position;
            source.clip = Clip(kind);
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        static void EnsurePool()
        {
            if (pool != null && pool[0] != null)
                return;
            var root = new GameObject("DemoSfxPool");
            pool = new AudioSource[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Sfx" + i);
                go.transform.SetParent(root.transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 0.5f;
                source.maxDistance = 12f;
                source.dopplerLevel = 0f;
                source.priority = 96;
                pool[i] = source;
            }
        }

        static AudioClip Clip(Kind kind)
        {
            var i = (int)kind;
            if (clips[i] != null)
                return clips[i];

            float[] data;
            switch (kind)
            {
                case Kind.Chime: data = Synth(0.9f, (t, n) => (Sin(880f, t) + 0.6f * Sin(1320f, t) + 0.25f * Sin(1760f, t)) * Exp(t, 4f) * Atk(t) * 0.35f); break;
                case Kind.Whoosh: data = Whoosh(1.3f); break;
                case Kind.Pop: data = Synth(0.14f, (t, n) => Mathf.Sin(2f * Mathf.PI * (600f * t - 1500f * t * t)) * Exp(t, 25f) * 0.5f); break;
                case Kind.Splat: data = Splat(0.3f); break;
                case Kind.Bowl: data = Synth(4f, (t, n) => (Sin(220f, t) + 0.6f * Sin(220.7f, t) + 0.45f * Sin(596f, t) * Exp(t, 1.4f) + 0.2f * Sin(1122f, t) * Exp(t, 2.5f)) * Exp(t, 0.85f) * Atk(t) * 0.22f); break;
                case Kind.Note: data = Synth(1.2f, (t, n) => (Sin(523.25f, t) + 0.3f * Sin(2093f, t) * Exp(t, 12f)) * Exp(t, 4f) * Atk(t) * 0.4f); break;
                case Kind.Success: data = Synth(0.8f, (t, n) => Arp(t)); break;
                case Kind.Error: data = Synth(0.35f, (t, n) => (Square(180f, t) + Square(171f, t)) * Exp(t, 6f) * 0.12f); break;
                case Kind.Shutter: data = Synth(0.16f, (t, n) => Noise(n) * (Exp(t, 120f) + (t > 0.08f ? Exp(t - 0.08f, 120f) : 0f)) * 0.6f); break;
                case Kind.Purr: data = Purr(1.6f); break;
                case Kind.Crackle: data = Synth(0.6f, (t, n) => (Hash(n / 90) > 0.93f ? Noise(n) * Exp(t, 3f) : 0f) * 0.7f); break;
                case Kind.Water: data = Water(0.5f); break;
                default: data = Synth(0.2f, (t, n) => Mathf.Sin(2f * Mathf.PI * (140f * t - 150f * t * t)) * Exp(t, 18f) * 0.7f); break;
            }

            clips[i] = AudioClip.Create("Sfx_" + kind, data.Length, 1, Rate, false);
            clips[i].SetData(data, 0);
            return clips[i];
        }

        static float[] Synth(float seconds, System.Func<float, int, float> f)
        {
            var count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (var n = 0; n < count; n++)
                data[n] = f(n / (float)Rate, n);
            return Fade(data);
        }

        static float[] Whoosh(float seconds)
        {
            var count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            var low = 0f;
            for (var n = 0; n < count; n++)
            {
                var u = n / (float)count;
                var cutoff = Mathf.Lerp(0.02f, 0.25f, Mathf.Sin(u * Mathf.PI));
                low += (Noise(n) - low) * cutoff;
                data[n] = low * Mathf.Sin(u * Mathf.PI) * 0.9f;
            }

            return Fade(data);
        }

        static float[] Splat(float seconds)
        {
            var count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            var low = 0f;
            for (var n = 0; n < count; n++)
            {
                var t = n / (float)Rate;
                low += (Noise(n) - low) * 0.18f;
                data[n] = (low * 1.4f + Sin(120f, t) * 0.5f) * Exp(t, 14f) * 0.6f;
            }

            return Fade(data);
        }

        static float[] Purr(float seconds)
        {
            var count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            var low = 0f;
            for (var n = 0; n < count; n++)
            {
                var t = n / (float)Rate;
                var u = t / seconds;
                low += (Noise(n) - low) * 0.03f;
                var pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 24f * t);
                data[n] = (low * 3f * pulse + Sin(48f, t) * 0.2f * pulse) * Mathf.Sin(u * Mathf.PI) * 0.8f;
            }

            return Fade(data);
        }

        static float[] Water(float seconds)
        {
            var count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            var low = 0f;
            for (var n = 0; n < count; n++)
            {
                var u = n / (float)count;
                low += (Noise(n) - low) * 0.35f;
                var bubble = Hash(n / 400) > 0.8f ? Sin(900f + Hash(n / 400 + 7) * 900f, n / (float)Rate) * 0.15f : 0f;
                data[n] = (low * 0.35f + bubble) * Mathf.Sin(u * Mathf.PI);
            }

            return Fade(data);
        }

        static float Arp(float t)
        {
            var s = 0f;
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (var i = 0; i < notes.Length; i++)
            {
                var dt = t - i * 0.09f;
                if (dt < 0f)
                    continue;
                s += (Sin(notes[i], dt) + 0.3f * Sin(notes[i] * 2f, dt)) * Exp(dt, 5f) * Atk(dt);
            }

            return s * 0.3f;
        }

        static float[] Fade(float[] data)
        {
            var tail = Mathf.Min(data.Length / 4, 220);
            for (var i = 0; i < tail; i++)
                data[data.Length - 1 - i] *= i / (float)tail;
            return data;
        }

        static float Sin(float f, float t)
        {
            return Mathf.Sin(2f * Mathf.PI * f * t);
        }

        static float Square(float f, float t)
        {
            return Sin(f, t) + Sin(f * 3f, t) / 3f + Sin(f * 5f, t) / 5f;
        }

        static float Exp(float t, float k)
        {
            return Mathf.Exp(-t * k);
        }

        static float Atk(float t)
        {
            return Mathf.Min(1f, t / 0.004f);
        }

        static float Noise(int n)
        {
            return Hash(n) * 2f - 1f;
        }

        static float Hash(int n)
        {
            unchecked
            {
                var x = (uint)n * 747796405u + 2891336453u;
                x = ((x >> 16) ^ x) * 0x45d9f3bu;
                x = ((x >> 16) ^ x) * 0x45d9f3bu;
                x = (x >> 16) ^ x;
                return (x & 0xffffff) / 16777215f;
            }
        }
    }
}
