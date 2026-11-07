using NUnit.Framework;

namespace GenJutsu.GenCore.Tests
{
    public class GenVoiceEditModeTests
    {
        const string Warm = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":2,\"mode\":\"dojo\",\"dojo\":\"Zen\"," +
                            "\"lights\":{\"main\":{\"intensity\":1.2,\"color\":\"#FF0000\"},\"lanterns\":{\"intensity\":0.4,\"color\":\"#00FF00\"}}," +
                            "\"ambience\":{\"preset\":\"soft\",\"volume\":0.3}}}";

        const string Cool = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":1,\"mode\":\"dojo\",\"dojo\":\"Cyber\"," +
                            "\"lights\":{\"main\":{\"intensity\":0.2,\"color\":\"#0000FF\"}}," +
                            "\"ambience\":{\"preset\":\"night\",\"volume\":0.1}}}";

        [Test]
        public void NewerVersion_ReplacesPlan_OlderAndEqualDoNot()
        {
            var cursor = new HouseStateCursor();

            Assert.IsTrue(cursor.Offer(Cool, "AB3K7Q"));
            Assert.AreEqual(1, cursor.Version);
            Assert.AreEqual("Cyber", cursor.Plan.Dojo);

            Assert.IsFalse(cursor.Offer(Cool, "AB3K7Q"));
            Assert.AreEqual(1, cursor.Version);

            Assert.IsTrue(cursor.Offer(Warm, "AB3K7Q"));
            Assert.AreEqual(2, cursor.Version);
            Assert.AreEqual("Zen", cursor.Plan.Dojo);
            Assert.AreEqual(1.2f, cursor.Plan.MainIntensity, 0.001f);
            Assert.Greater(cursor.Plan.MainColor.r, 0.9f);
            Assert.IsFalse(cursor.Offer(
                "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":2,\"dojo\":\"Excentrico\",\"lights\":{\"main\":{\"intensity\":9,\"color\":\"#FFFFFF\"}}}}",
                "AB3K7Q"));
            Assert.AreEqual("Zen", cursor.Plan.Dojo);
        }

        [Test]
        public void NullState_AndOtherSession_AreIgnored()
        {
            var cursor = new HouseStateCursor();
            Assert.IsTrue(cursor.Offer(Warm, "AB3K7Q"));
            Assert.IsFalse(cursor.Offer("{\"state\":null}", "AB3K7Q"));
            Assert.AreEqual(2, cursor.Version);
            Assert.IsFalse(cursor.Offer(
                "{\"state\":{\"session_id\":\"OTHER\",\"version\":9,\"dojo\":\"Acolhedor\"}}",
                "AB3K7Q"));
            Assert.AreEqual(2, cursor.Version);
            Assert.AreEqual("Zen", cursor.Plan.Dojo);
        }

        [Test]
        public void ApplierMapping_DoesNotChangeTheSessionProfile()
        {
            Assert.IsTrue(HouseStateLogic.TryParse(Warm, out var snap, out var stateNull));
            Assert.IsFalse(stateNull);
            var plan = HouseStateLogic.Map(snap);

            Assert.IsTrue(plan.HasMain);
            Assert.IsTrue(plan.HasLanterns);
            Assert.AreEqual(0.4f, plan.LanternIntensity, 0.001f);
            Assert.Greater(plan.LanternColor.g, 0.9f);
            Assert.IsTrue(plan.SwitchDojo);
            Assert.AreEqual("Zen", plan.Dojo);
            Assert.IsTrue(plan.HasAmbience);
            Assert.AreEqual("soft", plan.Preset);
            Assert.AreEqual(0.3f, plan.Volume, 0.001f);
            Assert.IsFalse(plan.ChangesSessionProfile);
        }

        [Test]
        public void GeminiAudio_UsesPcm16Rate_AndStreamEnd()
        {
            var chunk = GeminiLiveMessages.AudioChunk(new byte[] { 0, 1, 2, 3 }, 16000);
            StringAssert.Contains("\"realtimeInput\"", chunk);
            StringAssert.Contains("audio/pcm;rate=16000", chunk);
            StringAssert.DoesNotContain("mediaChunks", chunk);
            Assert.AreEqual("{\"realtimeInput\":{\"audioStreamEnd\":true}}", GeminiLiveMessages.AudioStreamEnd());
            Assert.AreEqual(24000, GeminiLiveMessages.RateFromMime("audio/pcm;rate=24000"));
        }

        [Test]
        public void InterruptedFlag_IsReadFromServerContent()
        {
            Assert.IsTrue(GeminiLiveMessages.TryReadServer("{\"serverContent\":{\"interrupted\":true}}", out var ev));
            Assert.IsTrue(ev.Interrupted);
        }

        [Test]
        public void ToolCall_ForwardsToolAndArgs_AndBuildsToolResponse()
        {
            const string message =
                "{\"toolCall\":{\"functionCalls\":[{\"id\":\"call-1\",\"name\":\"switch_dojo\",\"args\":{\"dojo\":\"Cyber\"}}]}}";
            Assert.IsTrue(GeminiLiveMessages.TryReadServer(message, out var ev));
            Assert.AreEqual(1, ev.Tools.Count);
            Assert.AreEqual("call-1", ev.Tools[0].Id);
            Assert.AreEqual("switch_dojo", ev.Tools[0].Name);
            Assert.AreEqual("{\"tool\":\"switch_dojo\",\"args\":{\"dojo\":\"Cyber\"}}",
                GeminiLiveMessages.CommandBody(ev.Tools[0].Name, ev.Tools[0].ArgsJson));

            var mock = new MockTransport();
            var n = GenToolRoundtrip.Forward(message, mock, (tool, args) =>
            {
                Assert.AreEqual("switch_dojo", tool);
                Assert.AreEqual("{\"dojo\":\"Cyber\"}", args);
                return "{\"ok\":true}";
            });

            Assert.AreEqual(1, n);
            StringAssert.Contains("\"toolResponse\"", mock.Last);
            StringAssert.Contains("\"functionResponses\"", mock.Last);
            StringAssert.Contains("call-1", mock.Last);
            StringAssert.Contains("switch_dojo", mock.Last);
            StringAssert.Contains("\"ok\":true", mock.Last);
        }

        [Test]
        public void PushToTalk_InterruptsSpeaking()
        {
            Assert.IsTrue(GenVoicePhases.IsInterruption(GenVoicePhase.Speaking));
            Assert.IsTrue(GenVoicePhases.IsInterruption(GenVoicePhase.Thinking));
            Assert.AreEqual(GenVoicePhase.Listening, GenVoicePhases.AfterPttPressed(GenVoicePhase.Speaking));
            Assert.AreEqual(GenVoicePhase.Thinking, GenVoicePhases.AfterPttReleased(GenVoicePhase.Listening));
            Assert.AreEqual(GenVoicePhase.Speaking, GenVoicePhases.AfterModelAudio(GenVoicePhase.Thinking));
            Assert.AreEqual(GenVoicePhase.Idle, GenVoicePhases.AfterServerInterrupted(GenVoicePhase.Speaking, false));
            Assert.AreEqual(GenVoicePhase.Listening, GenVoicePhases.AfterServerInterrupted(GenVoicePhase.Speaking, true));
        }

        sealed class MockTransport : IVoiceTransport
        {
            public string Last;
            public bool IsLive => true;
            public void SendRaw(string json) { Last = json; }
            public void Close() { }
        }
    }
}
