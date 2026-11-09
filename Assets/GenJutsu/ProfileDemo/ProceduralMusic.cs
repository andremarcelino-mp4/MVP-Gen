using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Soft generative soundtrack per profile. Clips are streamed from a synth, so there are no audio files to ship.
    /// </summary>
    public class ProceduralMusic : MonoBehaviour
    {
        public const int SampleRate = 22050;

        public float musicVolume = 0.55f;
        public float fadeSeconds = 2.5f;

        AudioSource[] sources;
        Coroutine[] fades;
        int current = -1;
        readonly AudioClip[] clips = new AudioClip[Profiles.Count];

        public bool IsPlaying => current >= 0;

        public void SetLevel(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (sources == null || current < 0)
                return;
            FadeTo(current, musicVolume, false);
        }

        void Awake()
        {
            sources = new AudioSource[2];
            fades = new Coroutine[2];
            for (var i = 0; i < 2; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.priority = 0;
                source.volume = 0f;
                source.dopplerLevel = 0f;
                sources[i] = source;
            }
        }

        public void Play(ProfileId id)
        {
            var next = current == 0 ? 1 : 0;
            if (current >= 0)
                FadeTo(current, 0f, true);

            var source = sources[next];
            source.spatialBlend = 0f;
            source.loop = true;
            source.volume = 0f;
            StartCoroutine(PlayLicensed(id, source, next));
            current = next;
        }

        IEnumerator PlayLicensed(ProfileId id, AudioSource source, int slot)
        {
            var url = AmbientCatalog.Url(id);
            using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                req.timeout = 20;
                req.SetRequestHeader("User-Agent", "GenJutsuQuest/2.8");
                var handler = req.downloadHandler as UnityEngine.Networking.DownloadHandlerAudioClip;
                if (handler != null)
                    handler.streamAudio = true;
                yield return req.SendWebRequest();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    source.clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
                    source.loop = true;
                    source.Play();
                    FadeTo(slot, musicVolume, false);
                    Debug.Log("[Music] playing " + AmbientCatalog.Credit(id) + " bytes=" + (req.downloadedBytes));
                    yield break;
                }

                Debug.LogWarning("[Music] stream failed " + url + " " + req.result + " " + req.error);
            }

            source.clip = ClipFor(id);
            source.Play();
            FadeTo(slot, musicVolume, false);
            Debug.Log("[Music] synth fallback " + id);
        }

        public void Stop()
        {
            for (var i = 0; i < sources.Length; i++)
                FadeTo(i, 0f, true);
            current = -1;
        }

        void FadeTo(int index, float target, bool stopAtEnd)
        {
            if (fades[index] != null)
                StopCoroutine(fades[index]);
            fades[index] = StartCoroutine(FadeRoutine(index, target, stopAtEnd));
        }

        IEnumerator FadeRoutine(int index, float target, bool stopAtEnd)
        {
            var source = sources[index];
            var start = source.volume;
            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }

            source.volume = target;
            if (stopAtEnd && target <= 0f)
                source.Stop();
            fades[index] = null;
        }

        AudioClip ClipFor(ProfileId id)
        {
            var i = (int)id;
            if (clips[i] != null)
                return clips[i];

            Song song;
            switch (id)
            {
                case ProfileId.Zen: song = new ZenSong(); break;
                case ProfileId.Cyber: song = new CyberSong(); break;
                case ProfileId.Excentrico: song = new EccentricSong(); break;
                default: song = new CozySong(); break;
            }

            var stream = new SongStream(song);
            clips[i] = AudioClip.Create("Music_" + id, SampleRate * 600, 1, SampleRate, true, stream.Read, stream.SetPosition);
            return clips[i];
        }

        sealed class SongStream
        {
            readonly Song song;
            long position;

            public SongStream(Song song)
            {
                this.song = song;
            }

            public void Read(float[] data)
            {
                for (var i = 0; i < data.Length; i++)
                    data[i] = song.Sample(position++);
            }

            public void SetPosition(int newPosition)
            {
                position = newPosition;
            }
        }

        abstract class Song
        {
            protected const double Tau = System.Math.PI * 2.0;

            public float Sample(long n)
            {
                var t = n / (double)SampleRate;
                return SoftClip(Render(t));
            }

            protected abstract float Render(double t);

            protected static float Freq(int midi)
            {
                return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
            }

            protected static float Sin(double freq, double t)
            {
                return (float)System.Math.Sin(Tau * freq * t);
            }

            protected static float Attack(double t, double seconds)
            {
                return t >= seconds ? 1f : (float)(t / seconds);
            }

            protected static float Noise(long n)
            {
                unchecked
                {
                    var x = (uint)(n * 747796405L + 2891336453L);
                    x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u;
                    x = (x >> 22) ^ x;
                    return x / (float)uint.MaxValue * 2f - 1f;
                }
            }

            protected static uint Hash(int a, int b)
            {
                unchecked
                {
                    var h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
                    h ^= h >> 13;
                    h *= 0x5bd1e995;
                    h ^= h >> 15;
                    return h;
                }
            }

            static float SoftClip(float x)
            {
                x = Mathf.Clamp(x, -3f, 3f);
                return x * (27f + x * x) / (27f + 9f * x * x);
            }
        }

        sealed class ZenSong : Song
        {
            static readonly int[] Bells = { 74, -1, 69, -1, 72, -1, -1, 67, 74, -1, 77, -1, 69, -1, -1, -1 };
            const double Step = 2.14;

            protected override float Render(double t)
            {
                var drone = 0.17f * Sin(73.42, t) * (0.6f + 0.4f * Sin(0.07, t))
                            + 0.11f * Sin(110.0, t) * (0.6f + 0.4f * Sin(0.05, t + 3.0))
                            + 0.05f * Sin(146.83, t) * (0.5f + 0.5f * Sin(0.031, t));

                var bells = 0f;
                var index = (long)(t / Step);
                for (var k = 0; k < 4; k++)
                {
                    var s = index - k;
                    if (s < 0)
                        break;
                    var note = Bells[(int)(s % Bells.Length)];
                    if (note < 0)
                        continue;
                    var dt = t - s * Step;
                    var f = Freq(note);
                    var env = (float)System.Math.Exp(-dt * 0.65) * Attack(dt, 0.005);
                    bells += env * (Sin(f, dt) + 0.35f * Sin(f * 2.76, dt) * (float)System.Math.Exp(-dt * 1.5)
                                    + 0.12f * Sin(f * 5.4, dt) * (float)System.Math.Exp(-dt * 3.0));
                }

                return drone + bells * 0.14f;
            }
        }

        sealed class CyberSong : Song
        {
            static readonly int[][] Chords = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 55, 59, 62 } };
            static readonly int[] ArpOrder = { 0, 1, 2, 1 };
            static readonly bool[] BassHits = { true, false, false, true, false, false, true, false, true, false, false, true, false, false, true, false };
            const double Step = 0.15625;

            protected override float Render(double t)
            {
                var index = (long)(t / Step);
                var output = 0f;

                for (var k = 0; k < 3; k++)
                {
                    var s = index - k;
                    if (s < 0)
                        break;
                    var dt = t - s * Step;
                    var bar = (int)(s / 16 % 4);
                    var stepInBar = (int)(s % 16);
                    var chord = Chords[bar];

                    if (BassHits[stepInBar])
                    {
                        var f = Freq(chord[0] - 24);
                        var env = (float)System.Math.Exp(-dt * 5.0) * Attack(dt, 0.004);
                        output += 0.2f * env * (Sin(f, dt) + 0.5f * Sin(f * 2, dt) + 0.22f * Sin(f * 3, dt));
                    }

                    var lift = (s / 32) % 2 == 1 && stepInBar >= 8 ? 12 : 0;
                    var arp = Freq(chord[ArpOrder[stepInBar % 4]] + 12 + lift);
                    var aenv = (float)System.Math.Exp(-dt * 9.0) * Attack(dt, 0.003);
                    output += 0.1f * aenv * (Sin(arp, dt) + 0.28f * Sin(arp * 3, dt));

                    if (k == 0 && stepInBar % 2 == 1)
                        output += 0.03f * Noise((long)(t * SampleRate)) * (float)System.Math.Exp(-dt * 60.0);
                }

                var barLength = Step * 16;
                var barIndex = (long)(t / barLength);
                var tb = t - barIndex * barLength;
                var padChord = Chords[(int)(barIndex % 4)];
                var padEnv = Mathf.Min(1f, (float)(tb / 0.4)) * Mathf.Min(1f, (float)((barLength - tb) / 0.4));
                var pad = 0f;
                foreach (var note in padChord)
                    pad += Sin(Freq(note), t) + 0.5f * Sin(Freq(note) * 1.005, t);
                output += pad * 0.022f * padEnv * (0.75f + 0.25f * Sin(0.5, t));

                return output;
            }
        }

        sealed class EccentricSong : Song
        {
            static readonly int[] Scale = { 60, 62, 64, 66, 67, 69, 71, 72, 74, 76 };
            static readonly int[] BassRoots = { 36, 41, 43, 45 };
            const double Eighth = 0.268;

            protected override float Render(double t)
            {
                var pair = (long)(t / (Eighth * 2));
                var output = 0f;

                for (var k = 0; k < 6; k++)
                {
                    var s = pair * 2 + 1 - k;
                    if (s < 0)
                        continue;
                    var start = (s / 2) * Eighth * 2 + (s % 2 == 1 ? Eighth * 1.33 : 0.0);
                    var dt = t - start;
                    if (dt < 0)
                        continue;
                    var phrase = (int)(s / 32 % 4);
                    var h = Hash(phrase, (int)(s % 16));
                    if (h % 5 != 0)
                    {
                        var note = Scale[h % Scale.Length];
                        if (h % 11 == 0)
                            note += (h & 1) == 0 ? 12 : -12;
                        var f = Freq(note);
                        var env = (float)System.Math.Exp(-dt * 6.0) * Attack(dt, 0.002);
                        output += 0.15f * env * (Sin(f, dt) + 0.25f * Sin(f * 4, dt) * (float)System.Math.Exp(-dt * 14.0));
                    }

                    var stepInBar = (int)(s % 8);
                    if (stepInBar == 0 || stepInBar == 4)
                    {
                        var root = Freq(BassRoots[(int)(s / 8 % 4)]);
                        var benv = (float)System.Math.Exp(-dt * 4.0) * Attack(dt, 0.004);
                        output += 0.2f * benv * (Sin(root, dt) + 0.3f * Sin(root * 2, dt));
                    }

                    if (stepInBar == 6)
                        output += 0.045f * Sin(1800.0, dt) * (float)System.Math.Exp(-dt * 40.0);
                }

                return output;
            }
        }

        sealed class CozySong : Song
        {
            static readonly int[][] Chords = { new[] { 53, 57, 60 }, new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 48, 52, 55 } };
            static readonly int[] Arp = { 0, 1, 2, 1, 0, 2, 1, 2 };
            const double Beat = 0.882;
            const double Bar = Beat * 4;
            const double Eighth = Beat / 2;

            protected override float Render(double t)
            {
                var barIndex = (long)(t / Bar);
                var tb = t - barIndex * Bar;
                var output = Pad(Chords[(int)(barIndex % 4)], t, Mathf.Min(1f, (float)(tb / 0.8)));
                if (barIndex > 0 && tb < 1.2)
                    output += Pad(Chords[(int)((barIndex - 1) % 4)], t, Mathf.Exp((float)(-tb / 0.35)));

                var index = (long)(t / Eighth);
                for (var k = 0; k < 5; k++)
                {
                    var s = index - k;
                    if (s < 0)
                        break;
                    var stepInBar = (int)(s % 8);
                    if (stepInBar == 7 && (s / 8) % 2 == 1)
                        continue;
                    var chord = Chords[(int)(s / 8 % 4)];
                    var f = Freq(chord[Arp[stepInBar]] + 12);
                    var dt = t - s * Eighth;
                    var env = (float)System.Math.Exp(-dt * 2.2) * Attack(dt, 0.003);
                    output += 0.12f * env * (Sin(f, dt) + 0.4f * Sin(f * 2, dt) * (float)System.Math.Exp(-dt * 2.0)
                                             + 0.15f * Sin(f * 3, dt) * (float)System.Math.Exp(-dt * 4.0));
                }

                return output;
            }

            static float Pad(int[] chord, double t, float env)
            {
                var sum = 0f;
                foreach (var note in chord)
                {
                    var f = Freq(note);
                    sum += Sin(f, t) + Sin(f * 1.004, t);
                }

                sum += 1.2f * Sin(Freq(chord[0] - 12), t);
                return sum * 0.035f * env;
            }
        }
    }
}
