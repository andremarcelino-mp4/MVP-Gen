using NUnit.Framework;

namespace GenJutsu.ProfileDemo
{
    public class GazeLanCodecTests
    {
        [Test]
        public void CenterYawMapsToServoMid()
        {
            var s = GazeLanCodec.ServoFromYaw(0f);
            Assert.AreEqual(90, s);
        }

        [Test]
        public void LeftAndRightYawReachLimits()
        {
            var left = GazeLanCodec.ServoFromYaw(-GazeLanCodec.DefaultYawLimitRad);
            var right = GazeLanCodec.ServoFromYaw(GazeLanCodec.DefaultYawLimitRad);
            Assert.AreEqual(GazeLanCodec.DefaultServoMin, left);
            Assert.AreEqual(GazeLanCodec.DefaultServoMax, right);
        }

        [Test]
        public void InvertSwapsSides()
        {
            var a = GazeLanCodec.ServoFromYaw(0.5f, invert: false);
            var b = GazeLanCodec.ServoFromYaw(0.5f, invert: true);
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void PacketIsAsciiLine()
        {
            var line = GazeLanCodec.FormatLine(-0.31f, 0.04f, 72);
            Assert.AreEqual("Y,-0.310,P,0.040,S,72\n", line);
        }
    }
}
