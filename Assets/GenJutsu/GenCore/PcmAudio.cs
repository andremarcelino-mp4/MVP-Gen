using System;

namespace GenJutsu.GenCore
{
    public static class PcmAudio
    {
        public static int FloatToPcm16(float[] src, int count, byte[] dst)
        {
            if (src == null || dst == null || count <= 0)
                return 0;
            var n = Math.Min(count, src.Length);
            n = Math.Min(n, dst.Length / 2);
            for (var i = 0; i < n; i++)
            {
                var sample = src[i];
                if (sample > 1f)
                    sample = 1f;
                else if (sample < -1f)
                    sample = -1f;
                var v = (short)Math.Round(sample * 32767.0);
                dst[i * 2] = (byte)(v & 0xff);
                dst[i * 2 + 1] = (byte)((v >> 8) & 0xff);
            }

            return n * 2;
        }

        public static int Pcm16ToFloat(byte[] src, int byteCount, float[] dst)
        {
            if (src == null || dst == null || byteCount < 2)
                return 0;
            var samples = Math.Min(byteCount / 2, Math.Min(src.Length / 2, dst.Length));
            for (var i = 0; i < samples; i++)
            {
                short v = (short)(src[i * 2] | (src[i * 2 + 1] << 8));
                dst[i] = v / 32768f;
            }

            return samples;
        }

        public static int Resample(float[] src, int srcCount, int srcRate, float[] dst, int dstRate)
        {
            if (src == null || dst == null || srcCount <= 0 || srcRate <= 0 || dstRate <= 0)
                return 0;
            srcCount = Math.Min(srcCount, src.Length);
            if (srcRate == dstRate)
            {
                var n = Math.Min(srcCount, dst.Length);
                Array.Copy(src, dst, n);
                return n;
            }

            var outCount = (int)((long)srcCount * dstRate / srcRate);
            if (outCount > dst.Length)
                outCount = dst.Length;
            if (outCount <= 0)
                return 0;
            var step = (double)srcRate / dstRate;
            for (var i = 0; i < outCount; i++)
            {
                var pos = i * step;
                var i0 = (int)pos;
                if (i0 >= srcCount - 1)
                {
                    dst[i] = src[srcCount - 1];
                    continue;
                }

                var f = (float)(pos - i0);
                dst[i] = src[i0] + (src[i0 + 1] - src[i0]) * f;
            }

            return outCount;
        }
    }

    public static class WavEncoder
    {
        public static byte[] FromPcm16(byte[] pcm, int count, int sampleRate)
        {
            if (pcm == null)
                pcm = Array.Empty<byte>();
            if (count < 0)
                count = 0;
            if (count > pcm.Length)
                count = pcm.Length;
            count -= count & 1;
            if (sampleRate <= 0)
                sampleRate = 16000;
            var wav = new byte[44 + count];
            Write(wav, 0, "RIFF");
            WriteInt(wav, 4, 36 + count);
            Write(wav, 8, "WAVE");
            Write(wav, 12, "fmt ");
            WriteInt(wav, 16, 16);
            WriteShort(wav, 20, 1);
            WriteShort(wav, 22, 1);
            WriteInt(wav, 24, sampleRate);
            WriteInt(wav, 28, sampleRate * 2);
            WriteShort(wav, 32, 2);
            WriteShort(wav, 34, 16);
            Write(wav, 36, "data");
            WriteInt(wav, 40, count);
            if (count > 0)
                Buffer.BlockCopy(pcm, 0, wav, 44, count);
            return wav;
        }

        static void Write(byte[] dst, int offset, string ascii)
        {
            for (var i = 0; i < ascii.Length; i++)
                dst[offset + i] = (byte)ascii[i];
        }

        static void WriteInt(byte[] dst, int offset, int value)
        {
            dst[offset] = (byte)value;
            dst[offset + 1] = (byte)(value >> 8);
            dst[offset + 2] = (byte)(value >> 16);
            dst[offset + 3] = (byte)(value >> 24);
        }

        static void WriteShort(byte[] dst, int offset, int value)
        {
            dst[offset] = (byte)value;
            dst[offset + 1] = (byte)(value >> 8);
        }
    }
}
