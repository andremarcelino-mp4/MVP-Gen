using System;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Plays 24 kHz PCM from Gemini. The clip callback runs on the audio thread; enqueue is lock-protected
    /// so the socket thread can push samples without touching the AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class StreamingAudioPlayer : MonoBehaviour
    {
        public const int Rate = 24000;
        const int Capacity = Rate * 4;

        readonly float[] ring = new float[Capacity];
        readonly object gate = new object();
        int write;
        int count;
        AudioSource source;
        float[] resample = new float[4096];

        public bool IsAudible
        {
            get
            {
                lock (gate)
                    return count > Rate / 10;
            }
        }

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 32;
            source.volume = 1f;
            var clip = AudioClip.Create("GenVoice", Rate, 1, Rate, true, OnRead);
            source.clip = clip;
            source.Play();
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
            if (sampleRate != Rate)
            {
                var need = (int)((long)n * Rate / sampleRate) + 2;
                if (resample.Length < need)
                    resample = new float[need];
                n = PcmAudio.Resample(floats, n, sampleRate, resample, Rate);
                Write(resample, n);
                return;
            }

            Write(floats, n);
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

        void OnRead(float[] data)
        {
            lock (gate)
            {
                var read = write - count;
                if (read < 0)
                    read += ring.Length;
                for (var i = 0; i < data.Length; i++)
                {
                    if (count <= 0)
                    {
                        data[i] = 0f;
                        continue;
                    }

                    data[i] = ring[read];
                    read++;
                    if (read >= ring.Length)
                        read = 0;
                    count--;
                }
            }
        }
    }
}
