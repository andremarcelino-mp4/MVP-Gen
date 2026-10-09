using System;
using System.IO;
using UnityEngine;

namespace GenJutsu.AITest
{
    public class MicrophoneCapture : MonoBehaviour
    {
        const int SampleRate = 16000;
        AudioClip clip;
        string device;
        float startedAt;
        bool recording;

        public bool IsRecording => recording;

        public bool Begin()
        {
            if (recording)
                return true;
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogError("[GenJutsu] Etapa microfone: nenhum microfone encontrado.");
                return false;
            }

            device = Microphone.devices[0];
            clip = Microphone.Start(device, false, 20, SampleRate);
            startedAt = Time.unscaledTime;
            recording = clip != null;
            if (recording)
                Debug.Log("[GenJutsu] Microfone aberto: " + device);
            return recording;
        }

        public byte[] EndWav()
        {
            if (!recording)
                return null;
            var duration = Time.unscaledTime - startedAt;
            var position = Microphone.GetPosition(device);
            Microphone.End(device);
            recording = false;
            if (clip == null || duration < 0.35f || position < SampleRate / 4)
            {
                Debug.Log("[GenJutsu] Gravação curta demais.");
                return null;
            }

            var samples = new float[position];
            clip.GetData(samples, 0);
            return WavAudio.FromFloats(samples, SampleRate);
        }
    }

    public static class WavAudio
    {
        public static byte[] FromFloats(float[] samples, int sampleRate)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                var dataLength = samples.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);
                for (var i = 0; i < samples.Length; i++)
                {
                    var clamped = Mathf.Clamp(samples[i], -1f, 1f);
                    writer.Write((short)(clamped * short.MaxValue));
                }
                return stream.ToArray();
            }
        }

        public static AudioClip ToClip(byte[] wav, string name)
        {
            if (wav == null || wav.Length < 44)
                return null;
            var channels = BitConverter.ToInt16(wav, 22);
            var sampleRate = BitConverter.ToInt32(wav, 24);
            var bits = BitConverter.ToInt16(wav, 34);
            var dataStart = 44;
            for (var i = 12; i < wav.Length - 8; i++)
            {
                if (wav[i] == 'd' && wav[i + 1] == 'a' && wav[i + 2] == 't' && wav[i + 3] == 'a')
                {
                    dataStart = i + 8;
                    break;
                }
            }

            var bytesPerSample = Mathf.Max(1, bits / 8);
            var sampleCount = (wav.Length - dataStart) / bytesPerSample;
            var samples = new float[sampleCount];
            if (bits == 16)
            {
                for (var i = 0; i < sampleCount; i++)
                    samples[i] = BitConverter.ToInt16(wav, dataStart + i * 2) / 32768f;
            }
            else
            {
                for (var i = 0; i < sampleCount; i++)
                    samples[i] = (wav[dataStart + i] - 128) / 128f;
            }

            var clip = AudioClip.Create(name, sampleCount / Mathf.Max(1, (int)channels), Mathf.Max(1, (int)channels), sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
