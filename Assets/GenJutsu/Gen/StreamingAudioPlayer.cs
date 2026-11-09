using System;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Plays Gemini 24 kHz PCM on Quest. Streaming AudioClip.OnPCMReaderCallback is silent on
    /// Android/OpenXR; OnAudioFilterRead on a looping pad clip is what the DSP thread actually pulls.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class StreamingAudioPlayer : MonoBehaviour
    {
        public const int Rate = 24000;
        const int Capacity = 48000 * 8;

        readonly float[] ring = new float[Capacity];
        readonly object gate = new object();
        int write;
        int count;
        AudioSource source;
        float[] resample = new float[8192];
        int dspRate = Rate;
        int loggedReads;
        int loggedWrites;

        public bool IsAudible
        {
            get
            {
                lock (gate)
                    return count > Mathf.Max(240, dspRate / 20);
            }
        }

        public bool SourcePlaying => source != null && source.isPlaying;
        public float SourceVolume => source != null ? source.volume : 0f;

        void Awake()
        {
            dspRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 0;
            source.volume = 1f;
            source.mute = false;
            source.bypassListenerEffects = true;
            source.bypassEffects = true;
            source.ignoreListenerPause = true;
            source.outputAudioMixerGroup = null;
            var pad = AudioClip.Create("GenVoicePad", dspRate, 1, dspRate, false);
            pad.SetData(new float[dspRate], 0);
            source.clip = pad;
            source.Play();
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            RequestFocus();
            GenVoiceLog.Info("speaker dsp=" + dspRate + " playing=" + source.isPlaying + " vol=" + source.volume
                + " mute=" + source.mute + " spatial=" + source.spatialBlend);
        }

        void LateUpdate()
        {
            if (source == null)
                return;
            if (!source.isPlaying)
                source.Play();
            if (source.mute)
                source.mute = false;
            if (source.volume < 0.99f)
                source.volume = 1f;
        }

        public void EnqueuePcm16(byte[] pcm, int byteCount, int sampleRate)
        {
            if (pcm == null || byteCount < 2)
                return;
            var samples = byteCount / 2;
            var floats = new float[samples];
            var n = PcmAudio.Pcm16ToFloat(pcm, byteCount, floats);
            if (sampleRate <= 0)
                sampleRate = Rate;
            var target = dspRate > 0 ? dspRate : Rate;
            if (sampleRate != target)
            {
                var need = (int)((long)n * target / sampleRate) + 2;
                if (resample.Length < need)
                    resample = new float[need];
                n = PcmAudio.Resample(floats, n, sampleRate, resample, target);
                Write(resample, n);
            }
            else
                Write(floats, n);

            loggedWrites++;
            if (loggedWrites <= 8 || loggedWrites % 20 == 0)
                GenVoiceLog.Info("pcm in bytes=" + byteCount + " rate=" + sampleRate + " queued=" + PeekCount()
                    + " playing=" + (source != null && source.isPlaying));
        }

        public void EnqueueBase64(string base64, int sampleRate)
        {
            if (string.IsNullOrEmpty(base64))
                return;
            byte[] pcm;
            try
            {
                pcm = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return;
            }

            EnqueuePcm16(pcm, pcm.Length, sampleRate);
        }

        public void Clear()
        {
            lock (gate)
            {
                write = 0;
                count = 0;
            }
        }

        int PeekCount()
        {
            lock (gate)
                return count;
        }

        void Write(float[] src, int n)
        {
            lock (gate)
            {
                for (var i = 0; i < n; i++)
                {
                    ring[write] = src[i];
                    write++;
                    if (write >= ring.Length)
                        write = 0;
                    if (count < ring.Length)
                        count++;
                }
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            lock (gate)
            {
                var read = write - count;
                if (read < 0)
                    read += ring.Length;
                var frames = data.Length / Mathf.Max(1, channels);
                for (var f = 0; f < frames; f++)
                {
                    float sample = 0f;
                    if (count > 0)
                    {
                        sample = ring[read];
                        read++;
                        if (read >= ring.Length)
                            read = 0;
                        count--;
                    }

                    var o = f * channels;
                    for (var c = 0; c < channels; c++)
                        data[o + c] = sample;
                }
            }

            loggedReads++;
        }

        static void RequestFocus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var am = activity.Call<AndroidJavaObject>("getSystemService", "audio"))
                    am.Call<int>("requestAudioFocus", null, 3, 1);
            }
            catch (Exception e)
            {
                GenVoiceLog.Warn("audio focus: " + e.Message);
            }
#endif
        }
    }
}
