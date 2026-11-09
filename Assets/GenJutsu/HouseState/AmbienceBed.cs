using UnityEngine;

namespace GenJutsu.HouseState
{
    /// <summary>
    /// Ambience presets from the house API. These loops are synthesized placeholders:
    /// the project has no recorded rain, bamboo, or night clips.
    /// </summary>
    public sealed class AmbienceBed : MonoBehaviour
    {
        public const string PlaceholderNote =
            "Synthesized placeholders. No recorded rain, bamboo, quiet, or night clips are in the project.";

        const int Rate = 22050;
        AudioSource source;
        string playing = "";

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 64;
            source.volume = 0f;
        }

        public bool IsPlaying => source != null && source.isPlaying;
        public string Current => playing;

        public void Play(string preset, float volume, float fadeSeconds)
        {
            if (source == null)
                return;
            var name = (preset ?? "").Trim().ToLowerInvariant();
            if (name == "off" || name == "silence" || name == "mute" || name == "quiet")
            {
                StopBed();
                return;
            }

            if (playing != name)
            {
                source.clip = Clip(name);
                source.Play();
                playing = name;
            }

            source.volume = Mathf.Clamp(volume, 0f, 0.5f);
            if (fadeSeconds > 0f && source.volume > 0f)
                source.volume = Mathf.MoveTowards(source.volume, Mathf.Clamp(volume, 0f, 0.5f), Time.deltaTime / fadeSeconds);
        }

        public void StopBed()
        {
            playing = "";
            if (source == null)
                return;
            source.volume = 0f;
            source.Stop();
        }

        static AudioClip Clip(string preset)
        {
            var samples = new float[Rate * 4];
            Fill(preset, samples);
            var clip = AudioClip.Create("Ambience_" + preset, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static void Fill(string preset, float[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)Rate;
                if (preset == "rain")
                    data[i] = Noise(i) * 0.35f * (0.65f + 0.35f * Mathf.PerlinNoise(t * 3f, 0.2f));
                else if (preset == "bamboo")
                    data[i] = Bamboo(t);
                else if (preset == "night")
                    data[i] = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.12f + Noise(i) * 0.04f;
                else
                    data[i] = Noise(i) * 0.08f;
            }
        }

        static float Bamboo(float t)
        {
            var hit = Mathf.Repeat(t, 0.85f);
            var env = Mathf.Exp(-hit * 7f);
            var tone = Mathf.Sin(2f * Mathf.PI * (392f + 40f * Mathf.Floor(t / 0.85f % 5f)) * hit);
            return tone * env * 0.35f;
        }

        static float Noise(int i)
        {
            var n = (i * 1103515245 + 12345) & 0x7fffffff;
            return (n / (float)0x7fffffff) * 2f - 1f;
        }
    }
}
