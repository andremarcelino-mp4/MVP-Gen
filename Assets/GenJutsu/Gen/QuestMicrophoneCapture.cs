using System;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Quest mic → 16 kHz PCM16. Starts the default device (or devices[0]) at 48 kHz, then 16 kHz,
    /// waits until GetPosition is past 0, and reads about 100 ms at a time. Unity's Microphone API
    /// is main-thread only.
    /// </summary>
    public sealed class QuestMicrophoneCapture : MonoBehaviour
    {
        public const int TargetRate = 16000;

        AudioClip clip;
        string device;
        int lastSample;
        int channels = 1;
        int sourceRate = TargetRate;
        bool capturing;
        bool heard;
        float nextChunk;
        float nextWaitLog;
        readonly System.Collections.Generic.List<byte> utterance = new System.Collections.Generic.List<byte>(16000);
        float[] floatScratch = new float[4096];
        float[] monoScratch = new float[4096];
        float[] resampled = new float[4096];
        byte[] pcmScratch = new byte[8192];

        public bool IsCapturing => capturing;
        public bool HasPermission { get; private set; } = true;
        public float LastRms { get; private set; }
        public int LastPosition { get; private set; }

        public void EnsurePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            HasPermission = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
            GenVoiceLog.Info("RECORD_AUDIO granted=" + HasPermission);
            if (!HasPermission)
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
#else
            HasPermission = true;
#endif
        }

        public bool Begin()
        {
            EnsurePermission();
            if (!HasPermission)
            {
                GenVoiceLog.Warn("mic denied RECORD_AUDIO");
                return false;
            }

            var listed = Microphone.devices;
            var count = listed != null ? listed.Length : 0;
            var names = "";
            for (var i = 0; i < count; i++)
                names += (i == 0 ? "" : ",") + listed[i];
            GenVoiceLog.Info("Microphone.devices count=" + count + " [" + names + "]");
            End();
            device = null;
            clip = TryStart(null, 48000);
            if (clip == null && count > 0)
            {
                device = listed[0];
                clip = TryStart(device, 48000);
            }

            if (clip == null)
                clip = TryStart(device, TargetRate);
            if (clip == null)
            {
                GenVoiceLog.Warn("Microphone.Start failed");
                return false;
            }

            channels = Mathf.Max(1, clip.channels);
            sourceRate = clip.frequency > 0 ? clip.frequency : 48000;
            lastSample = 0;
            heard = false;
            nextChunk = 0f;
            nextWaitLog = 0f;
            LastRms = 0f;
            LastPosition = 0;
            utterance.Clear();
            capturing = true;
            GenVoiceLog.Info("Microphone.Start ok device=" + (device ?? "null")
                + " freq=" + clip.frequency + " ch=" + clip.channels + " samples=" + clip.samples
                + " position=" + Microphone.GetPosition(device));
            return true;
        }

        static AudioClip TryStart(string name, int rate)
        {
            GenVoiceLog.Info("Microphone.Start name=" + (name ?? "null") + " loop=1 rate=" + rate);
            AudioClip started = null;
            try
            {
                started = Microphone.Start(name, true, 1, rate);
            }
            catch (Exception e)
            {
                GenVoiceLog.Warn("Microphone.Start error " + e.GetType().Name + ": " + e.Message);
                return null;
            }

            GenVoiceLog.Info("Microphone.Start result=" + (started != null) + " position=" + Microphone.GetPosition(name));
            return started;
        }

        public void End()
        {
            var was = capturing || clip != null;
            capturing = false;
            LastRms = 0f;
            try
            {
                if (was && Microphone.IsRecording(device))
                    Microphone.End(device);
            }
            catch (Exception e)
            {
                GenVoiceLog.Warn("Microphone.End " + e.Message);
            }

            clip = null;
        }

        /// <summary>Newly captured PCM16 at 16 kHz since the previous pull. Empty until the mic position moves, then about every 100 ms.</summary>
        public int PullPcm16(byte[] dst)
        {
            if (!capturing || clip == null || dst == null || dst.Length < 2)
                return 0;
            int pos;
            try
            {
                pos = Microphone.GetPosition(device);
            }
            catch (Exception e)
            {
                GenVoiceLog.Warn("GetPosition " + e.Message);
                return 0;
            }

            LastPosition = pos;
            if (pos <= 0)
            {
                if (Time.unscaledTime >= nextWaitLog)
                {
                    nextWaitLog = Time.unscaledTime + 0.5f;
                    GenVoiceLog.Info("mic position=" + pos + " waiting");
                }

                return 0;
            }

            if (!heard)
            {
                heard = true;
                lastSample = 0;
                GenVoiceLog.Info("mic position=" + pos + " rate=" + sourceRate);
            }

            if (Time.unscaledTime < nextChunk)
                return 0;
            var available = pos - lastSample;
            if (available < 0)
                available += clip.samples;
            if (available <= 0)
                return 0;
            nextChunk = Time.unscaledTime + 0.1f;
            var frames = Mathf.Min(available, Mathf.Max(1, sourceRate / 10));
            var interleaved = frames * channels;
            if (floatScratch.Length < interleaved)
                floatScratch = new float[interleaved];
            if (lastSample + frames <= clip.samples)
            {
                clip.GetData(floatScratch, lastSample);
            }
            else
            {
                var first = clip.samples - lastSample;
                var head = new float[first * channels];
                clip.GetData(head, lastSample);
                Array.Copy(head, floatScratch, head.Length);
                var tail = new float[(frames - first) * channels];
                clip.GetData(tail, 0);
                Array.Copy(tail, 0, floatScratch, head.Length, tail.Length);
            }

            lastSample = (lastSample + frames) % clip.samples;
            if (monoScratch.Length < frames)
                monoScratch = new float[frames];
            double acc = 0;
            for (var i = 0; i < frames; i++)
            {
                float sum = 0f;
                for (var c = 0; c < channels; c++)
                    sum += floatScratch[i * channels + c];
                var sample = sum / channels;
                monoScratch[i] = sample;
                acc += sample * sample;
            }

            LastRms = frames > 0 ? Mathf.Sqrt((float)(acc / frames)) : 0f;
            var outFrames = frames;
            var src = monoScratch;
            if (sourceRate != TargetRate)
            {
                var need = (int)((long)frames * TargetRate / sourceRate) + 2;
                if (resampled.Length < need)
                    resampled = new float[need];
                outFrames = PcmAudio.Resample(monoScratch, frames, sourceRate, resampled, TargetRate);
                src = resampled;
            }

            var bytesNeeded = outFrames * 2;
            if (pcmScratch.Length < bytesNeeded)
                pcmScratch = new byte[bytesNeeded];
            var wrote = PcmAudio.FloatToPcm16(src, outFrames, pcmScratch);
            var copy = Mathf.Min(wrote, dst.Length - (dst.Length & 1));
            if (copy <= 0)
                return 0;
            Buffer.BlockCopy(pcmScratch, 0, dst, 0, copy);
            for (var i = 0; i < copy; i++)
                utterance.Add(pcmScratch[i]);
            GenVoiceLog.Info("mic chunk bytes=" + copy + " rms=" + LastRms.ToString("0.000") + " position=" + pos);
            return copy;
        }

        public byte[] CopyPcm()
        {
            return utterance.ToArray();
        }

        public byte[] TakeWav()
        {
            var pcm = utterance.ToArray();
            utterance.Clear();
            return WavEncoder.FromPcm16(pcm, pcm.Length, TargetRate);
        }
    }
}
