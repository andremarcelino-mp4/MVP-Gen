using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GenJutsu.GenCore.PlayModeTests
{
    public class GenToolRoundtripPlayModeTests
    {
        [UnityTest]
        public IEnumerator MockedTransport_ToolCall_RoundTrips()
        {
            var go = new GameObject("gen-tool-roundtrip");
            var host = go.AddComponent<RoundtripHost>();
            yield return null;

            const string message =
                "{\"toolCall\":{\"functionCalls\":[{\"id\":\"call-7\",\"name\":\"switch_dojo\",\"args\":{\"dojo\":\"Acolhedor\"}}]}}";
            host.Count = GenToolRoundtrip.Forward(message, host, (tool, args) =>
            {
                host.Tool = tool;
                host.Args = args;
                return "{\"ok\":true,\"applied\":\"switch_dojo\"}";
            });
            yield return null;

            Assert.AreEqual(1, host.Count);
            Assert.AreEqual("switch_dojo", host.Tool);
            StringAssert.Contains("Acolhedor", host.Args);
            StringAssert.Contains("\"toolResponse\"", host.Last);
            StringAssert.Contains("call-7", host.Last);
            StringAssert.Contains("\"functionResponses\"", host.Last);
            Object.Destroy(go);
        }

        sealed class RoundtripHost : MonoBehaviour, IVoiceTransport
        {
            public string Last;
            public string Tool;
            public string Args;
            public int Count;
            public bool IsLive => true;
            public void SendRaw(string json) { Last = json; }
            public void Close() { }
        }
    }
}
