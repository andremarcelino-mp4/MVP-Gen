using System;
using System.Globalization;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Maquete packet (ESP32 + MG996R + MPU6050) and yaw→servo map.
    /// Yaw/pitch are radians, Y-up, yaw 0 ≈ camera +Z (Atan2(x,z)); pitch &gt; 0 = looking up.
    /// Line: Y,&lt;yaw&gt;,P,&lt;pitch&gt;,S,&lt;servo&gt;,PD,&lt;pitch deg clamped&gt;,N,&lt;seq&gt;\n
    /// </summary>
    public static class GazeLanCodec
    {
        public const float DefaultYawLimitRad = 1.04719755f;
        public const int DefaultServoMin = 30;
        public const int DefaultServoMax = 150;
        public const float DefaultPitchLimitDeg = 60f;

        public static int ServoFromYaw(
            float yawRad,
            float yawLimitRad = DefaultYawLimitRad,
            int servoMin = DefaultServoMin,
            int servoMax = DefaultServoMax,
            bool invert = false)
        {
            if (invert)
                yawRad = -yawRad;
            var limit = Math.Max(0.05f, yawLimitRad);
            var clamped = yawRad;
            if (clamped < -limit)
                clamped = -limit;
            else if (clamped > limit)
                clamped = limit;
            var t = (clamped / limit + 1f) * 0.5f;
            var lo = Math.Min(servoMin, servoMax);
            var hi = Math.Max(servoMin, servoMax);
            return (int)Math.Round(lo + t * (hi - lo));
        }

        /// <summary>Head pitch in degrees, clamped to ±limit (maquete tilt target).</summary>
        public static float PitchDegrees(float pitchRad, float limitDeg = DefaultPitchLimitDeg, bool invert = false)
        {
            var deg = pitchRad * 57.2957795f;
            if (invert)
                deg = -deg;
            var limit = Math.Max(1f, Math.Abs(limitDeg));
            if (deg < -limit)
                deg = -limit;
            else if (deg > limit)
                deg = limit;
            return deg;
        }

        public static string FormatLine(float yawRad, float pitchRad, int servo)
        {
            return "Y," + yawRad.ToString("0.000", CultureInfo.InvariantCulture)
                   + ",P," + pitchRad.ToString("0.000", CultureInfo.InvariantCulture)
                   + ",S," + servo
                   + "\n";
        }

        public static string FormatLine(float yawRad, float pitchRad, int servo, float pitchDeg, int seq)
        {
            return "Y," + yawRad.ToString("0.000", CultureInfo.InvariantCulture)
                   + ",P," + pitchRad.ToString("0.000", CultureInfo.InvariantCulture)
                   + ",S," + servo
                   + ",PD," + pitchDeg.ToString("0.0", CultureInfo.InvariantCulture)
                   + ",N," + seq.ToString(CultureInfo.InvariantCulture)
                   + "\n";
        }
    }
}
