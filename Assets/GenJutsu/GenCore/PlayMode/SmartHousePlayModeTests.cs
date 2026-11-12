using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GenJutsu.GenCore.PlayModeTests
{
    public class SmartHousePlayModeTests
    {
        [UnityTest]
        public IEnumerator HouseState_RegistersLampLedAcTv()
        {
            const string json =
                "{\"state\":{\"session_id\":\"T\",\"version\":1,\"devices\":{" +
                "\"sala_luz\":{\"on\":true,\"color\":\"#FF00FF\",\"intensity\":1}," +
                "\"fita_led\":{\"on\":true,\"color\":\"#00FFFF\",\"intensity\":1}," +
                "\"tv\":{\"on\":true,\"color\":\"#334455\",\"intensity\":0.8}," +
                "\"ar_condicionado\":{\"on\":true,\"temp\":22,\"mode\":\"frio\",\"fan\":\"auto\"}" +
                "}}}";
            Assert.IsTrue(HouseStateLogic.TryParse(json, out var snap, out var stateNull));
            Assert.IsFalse(stateNull);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "sala_luz", out var sala) && sala.On);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "fita_led", out var led) && led.On);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "tv", out var tv) && tv.On);
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "ar_condicionado", out var ac) && ac.On && ac.Temp == 22);
            CollectionAssert.Contains(HouseStateLogic.DeviceIds, "sala_luz");
            CollectionAssert.Contains(HouseStateLogic.DeviceIds, "fita_led");
            CollectionAssert.Contains(HouseStateLogic.DeviceIds, "tv");
            CollectionAssert.Contains(HouseStateLogic.DeviceIds, "ar_condicionado");
            yield return null;
        }
    }
}
