using System;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Microphone → 16 kHz PCM16. Unity's Microphone API is main-thread only; callers copy the bytes
    /// and hand them to the socket on a worker. Asks for RECORD_AUDIO on Android.
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
        readonly System.Collections.Generic.List<byte> utterance = new System.Collections.Generic.List<byte>(16000);
        float[] floatScratch = new float[4096];
        float[] monoScratch = new float[4096];
        float[] resampled = new float[4096];
        byte[] pcmScratch = new byte[8192];

        public bool IsCapturing => capturing;
        public bool HasPermission { get; private set; } = true;

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
                return false;
            if (Microphone.devices == null || Microphone.devices.Length == 0)
                return false;
            End();
            device = Microphone.devices[0];
            GenVoiceLog.Info("mic start device=" + device + " count=" + Microphone.devices.Length);
            clip = Microphone.Start(device, true, 8, TargetRate);
            if (clip == null)
                return false;
            channels = Mathf.Max(1, clip.channels);
            sourceRate = clip.frequency > 0 ? clip.frequency : TargetRate;
            lastSample = 0;
            utterance.Clear();
            capturing = true;
            return true;
        }

        public void End()
        {
            capturing = false;
            if (!string.IsNullOrEmpty(device))
            {
                if (Microphone.IsRecording(device))
                    Microphone.End(device);
            }

            clip = null;
        }

        /// <summary>Newly captured PCM16 at 16 kHz since the previous pull. Also kept for the WAV fallback.</summary>
        public int PullPcm16(byte[] dst)
        {
            if (!capturing || clip == null || dst == null || dst.Length < 2)
                return 0;
            var pos = Microphone.GetPosition(device);
            if (pos < 0)
                return 0;
            var available = pos - lastSample;
            if (available < 0)
                available += clip.samples;
            if (available < sourceRate / 20)
                return 0;
            var frames = Mathf.Min(available, 4000);
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
            for (var i = 0; i < frames; i++)
            {
                float sum = 0f;
                for (var c = 0; c < channels; c++)
                    sum += floatScratch[i * channels + c];
                monoScratch[i] = sum / channels;
            }

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
