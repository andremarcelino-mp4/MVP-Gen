using System;
using System.Globalization;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// LAN packet and yaw→servo map for the physical maquete (ESP32 + MG996R).
    /// Yaw/pitch are radians, Y-up, yaw 0 ≈ camera +Z (Atan2(x,z)).
    /// </summary>
    public static class GazeLanCodec
    {
        public const float DefaultYawLimitRad = 1.04719755f;
        public const int DefaultServoMin = 30;
        public const int DefaultServoMax = 150;

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

        public static string FormatLine(float yawRad, float pitchRad, int servo)
        {
            return "Y," + yawRad.ToString("0.000", CultureInfo.InvariantCulture)
                   + ",P," + pitchRad.ToString("0.000", CultureInfo.InvariantCulture)
                   + ",S," + servo
                   + "\n";
        }
    }
}
