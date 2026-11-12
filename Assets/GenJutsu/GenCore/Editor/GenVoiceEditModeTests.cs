using NUnit.Framework;
using UnityEngine;

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
        public void Media_ParsesTvAudioAndPlayingFlag()
        {
            const string json = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":8,\"media\":{\"type\":\"audio\",\"url\":\"https://example.com/lofi.mp3\",\"title\":\"lo-fi para relaxar\",\"artist\":\"Gen\",\"license\":\"CC BY\",\"playing\":true,\"device\":\"tv\"}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(json, out var snap, out _));
            Assert.IsTrue(snap.HasMedia);
            Assert.AreEqual("audio", snap.Media.Type);
            Assert.AreEqual("https://example.com/lofi.mp3", snap.Media.Url);
            Assert.AreEqual("lo-fi para relaxar", snap.Media.Title);
            Assert.AreEqual("CC BY", snap.Media.License);
            Assert.IsTrue(snap.Media.Playing);
            Assert.AreEqual("tv", snap.Media.Device);
            Assert.IsTrue(HouseStateLogic.Map(snap).HasMedia);
        }

        [Test]
        public void Devices_ParseSalaRed_AndKeepLegacyLights()
        {
            const string json = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":6,\"devices\":{\"sala_luz\":{\"on\":true,\"color\":\"#FF0000\",\"intensity\":0.9},\"tv\":{\"on\":false,\"color\":\"#00FFFF\",\"intensity\":0.2},\"ambiente\":{\"on\":true,\"color\":\"#334455\",\"intensity\":0.4}},\"lights\":{\"main\":{\"color\":\"#FFFFFF\",\"intensity\":0.5}}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(json, out var snap, out _));
            Assert.AreEqual(3, snap.Devices.Length);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "sala_luz", out var sala));
            Assert.IsTrue(sala.On);
            Assert.Greater(sala.Color.r, 0.9f);
            Assert.AreEqual(0.9f, sala.Intensity, 0.001f);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "tv", out var tv));
            Assert.IsFalse(tv.On);
            var plan = HouseStateLogic.Map(snap);
            Assert.IsTrue(plan.HasMain);
            Assert.AreEqual(0.5f, plan.MainIntensity, 0.001f);
            Assert.AreEqual(3, plan.Devices.Length);
            Assert.AreEqual(7, HouseStateLogic.DeviceIds.Length);
            Assert.AreEqual("sala_luz", HouseStateLogic.DeviceIds[0]);
            Assert.AreEqual("ambiente", HouseStateLogic.DeviceIds[5]);
            Assert.AreEqual("ar_condicionado", HouseStateLogic.DeviceIds[6]);
        }

        [Test]
        public void Ac_ParsesTempModeFan_AndStaysOptional()
        {
            const string json = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":9,\"devices\":{\"ar_condicionado\":{\"on\":true,\"temp\":20,\"mode\":\"frio\",\"fan\":\"2\"}}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(json, out var snap, out _));
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "ar_condicionado", out var ac));
            Assert.IsTrue(ac.On);
            Assert.AreEqual(20, ac.Temp);
            Assert.AreEqual("frio", ac.Mode);
            Assert.AreEqual("2", ac.Fan);
            Assert.AreEqual(20, HouseStateLogic.Map(snap).Devices[0].Temp);

            const string missing = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":10,\"devices\":{\"tv\":{\"on\":true}}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(missing, out var noAc, out _));
            Assert.IsFalse(HouseStateLogic.TryDevice(noAc.Devices, "ar_condicionado", out _));

            const string aliases = "{\"state\":{\"session_id\":\"AB3K7Q\",\"version\":11,\"devices\":{\"ar_condicionado\":{\"on\":true,\"temperature\":33,\"mode\":\"cool\",\"fan\":1}}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(aliases, out var aliasSnap, out _));
            Assert.IsTrue(HouseStateLogic.TryDevice(aliasSnap.Devices, "ar_condicionado", out var alias));
            Assert.AreEqual(30, alias.Temp);
            Assert.AreEqual("frio", alias.Mode);
            Assert.AreEqual("1", alias.Fan);
        }

        [Test]
        public void Persona_WeavesYoungPtBr_AndHonorsProfanityFlag()
        {
            var withSwear = GenPersona.Merge("Fale português.", true);
            StringAssert.Contains("amigo jovem", withSwear);
            StringAssert.Contains("porra", withSwear);
            StringAssert.Contains("Fale português.", withSwear);
            var clean = GenPersona.Merge("Fale português.", false);
            StringAssert.Contains("Não use palavrão", clean);
            StringAssert.DoesNotContain("porra", clean);
            Assert.IsTrue(GeminiLiveMessages.TryParseTicket(
                "{\"provider\":\"gemini\",\"token\":\"x\",\"config\":{\"GEN_PROFANITY\":false}}",
                out var ticket));
            Assert.IsTrue(ticket.HasProfanityFlag);
            Assert.IsFalse(ticket.AllowProfanity);
            var setup = GeminiLiveMessages.BuildSetup("gemini-3.8-live", "Fale português.", null, ticket.AllowProfanity);
            StringAssert.Contains("Não use palavrão", setup);
            StringAssert.Contains("languageCode\":\"pt-BR", setup);
        }

        [Test]
        public void Devices_NestedUnderLights_StillMap()
        {
            const string json = "{\"version\":3,\"lights\":{\"sala_luz\":{\"on\":true,\"color\":\"#00FF00\",\"intensity\":0.3},\"quarto_luz\":{\"on\":true,\"color\":\"#0000FF\",\"intensity\":0.7},\"abajur\":{\"on\":true,\"color\":\"#FFAA00\",\"intensity\":0.5},\"fita_led\":{\"on\":true,\"color\":\"#00FFFF\",\"intensity\":0.8}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(json, out var snap, out _));
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "sala_luz", out var sala));
            Assert.Greater(sala.Color.g, 0.9f);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "fita_led", out var led));
            Assert.Greater(led.Color.g, 0.9f);
        }

        [Test]
        public void WarmAndCoolLights_MapColorTransitionAndVisibleDrive()
        {
            const string warm = "{\"state\":{\"session_id\":\"YP8PFF\",\"version\":4,\"lights\":{\"main\":{\"color\":\"#FFAA00\",\"intensity\":0.8,\"transition_ms\":800},\"lanterns\":{\"color\":\"#FF1A1A\",\"intensity\":0.2,\"transition_ms\":800}},\"ambience\":{\"preset\":\"bamboo\",\"volume\":0.3,\"transition_ms\":500}}}";
            const string cool = "{\"state\":{\"session_id\":\"YP8PFF\",\"version\":5,\"lights\":{\"main\":{\"color\":\"#00BFFF\",\"intensity\":0.4,\"transition_ms\":300}}}}";

            Assert.IsTrue(HouseStateLogic.TryParse(warm, out var warmSnap, out _));
            var warmPlan = HouseStateLogic.Map(warmSnap);
            Assert.IsTrue(HouseStateLogic.TryHex("#FFAA00", out var warmColor));
            Assert.AreEqual(warmColor.r, warmPlan.MainColor.r, 0.01f);
            Assert.AreEqual(warmColor.g, warmPlan.MainColor.g, 0.01f);
            Assert.AreEqual(0.8f, warmPlan.MainIntensity, 0.001f);
            Assert.AreEqual(800, warmPlan.MainTransitionMs);
            Assert.AreEqual("bamboo", warmPlan.Preset);
            Assert.AreEqual(500, warmPlan.AmbienceTransitionMs);
            Assert.IsFalse(warmPlan.ChangesSessionProfile);

            Assert.IsTrue(HouseStateLogic.TryParse(cool, out var coolSnap, out _));
            var coolPlan = HouseStateLogic.Map(coolSnap);
            Assert.IsTrue(HouseStateLogic.TryHex("#00BFFF", out var coolColor));
            Assert.AreEqual(coolColor.b, coolPlan.MainColor.b, 0.01f);
            Assert.Less(coolPlan.MainColor.r, 0.05f);
            Assert.Greater(HouseStateLogic.DriveIntensity(0.8f, false), HouseStateLogic.DriveIntensity(0.4f, false));
        }

        [Test]
        public void LiveSetup_StripsAdditionalProperties_AndUsesGemini38()
        {
            const string tools = "[{\"name\":\"set_lights\",\"parameters\":{\"type\":\"object\",\"additionalProperties\":false,\"properties\":{\"color\":{\"type\":\"string\"}}}}]";
            var setup = GeminiLiveMessages.BuildSetup("gemini-3.8-live", "Fale português.", tools);

            StringAssert.Contains("models/gemini-3.8-live", setup);
            StringAssert.Contains("\"responseModalities\":[\"AUDIO\"]", setup);
            StringAssert.Contains("\"languageCode\":\"pt-BR\"", setup);
            StringAssert.DoesNotContain("voiceName", setup);
            StringAssert.Contains("functionDeclarations", setup);
            StringAssert.Contains("set_lights", setup);
            StringAssert.DoesNotContain("additionalProperties", setup);
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
        public void BinarySetupComplete_DecodesAsUtf8Json()
        {
            var json = "{\"setupComplete\":{}}";
            var payload = System.Text.Encoding.UTF8.GetBytes(json);
            var frame = new byte[payload.Length + 4];
            frame[0] = 0xEF;
            frame[1] = 0xBB;
            frame[2] = 0xBF;
            System.Buffer.BlockCopy(payload, 0, frame, 3, payload.Length);
            var text = GeminiLiveMessages.DecodeServerFrame(frame, frame.Length);
            Assert.IsTrue(GeminiLiveMessages.TryReadServer(text, out var ev));
            Assert.IsTrue(ev.SetupComplete);

            Assert.IsTrue(GeminiLiveMessages.TryParseTicket(
                "{\"provider\":\"gemini\",\"model\":\"gemini-3.8-live\",\"token\":\"ya29.x\",\"wsUrl\":\"wss://example/ws?access_token=abc\",\"voiceName\":\"Charon\"}",
                out var ticket));
            Assert.AreEqual("Charon", ticket.VoiceName);
            Assert.AreEqual("wss://example/ws?access_token=abc", GeminiLiveMessages.ResolveWs(ticket.WsUrl, ticket.Token));
        }

        [Test]
        public void HistoryBody_CarriesSessionRoleAndKind()
        {
            var body = GeminiLiveMessages.HistoryBody("YP8PFF", "gen", "Bem-vindo, Ana.", "welcome");
            StringAssert.Contains("\"session_id\":\"YP8PFF\"", body);
            StringAssert.Contains("\"role\":\"gen\"", body);
            StringAssert.Contains("\"kind\":\"welcome\"", body);
            StringAssert.Contains("Bem-vindo, Ana.", body);
            StringAssert.Contains("\"source\":\"quest\"", body);
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
        public void SpawnPoint_IsClearOfFurnitureInEachHouse()
        {
            Assert.Greater(HouseSpawnLogic.DistanceToLook(), 1.8f);
            Assert.Less(HouseSpawnLogic.DistanceToLook(), 3.6f);
            var names = new[] { "Zen", "Cyber", "Excentrico", "Acolhedor" };
            for (var i = 0; i < names.Length; i++)
            {
                var probe = HouseSpawnLogic.LocalFeet + Vector3.up * 0.9f;
                Assert.IsFalse(HouseSpawnLogic.Overlaps(probe, HouseSpawnLogic.ClearRadius, out var hit),
                    names[i] + " spawn inside " + hit);
                Assert.IsFalse(HouseSpawnLogic.Overlaps(HouseSpawnLogic.LocalFeet, 0.2f, out hit),
                    names[i] + " feet inside " + hit);
            }

            Assert.AreEqual(14, HouseSpawnLogic.RequiredProps.Length);
        }

        [Test]
        public void AmbientCatalog_UsesReachableIncompetechUrls()
        {
            StringAssert.Contains("incompetech.com", "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Carefree.mp3");
            Assert.Greater(HouseSpawnLogic.Furniture.Length, 5);
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
