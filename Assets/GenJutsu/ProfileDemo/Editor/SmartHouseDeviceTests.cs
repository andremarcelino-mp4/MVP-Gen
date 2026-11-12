using System.Collections;
using GenJutsu.GenCore;
using GenJutsu.HouseState;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public class SmartHouseDeviceTests
    {
        static readonly ProfileId[] Houses =
        {
            ProfileId.Zen, ProfileId.Cyber, ProfileId.Excentrico, ProfileId.Acolhedor
        };

        [UnityTest]
        public IEnumerator EachHouse_RegistersLampLedAcTv_AndToggleChangesState()
        {
            for (var i = 0; i < Houses.Length; i++)
            {
                var id = Houses[i];
                var go = new GameObject("SmartLook_" + id);
                var look = go.AddComponent<ProfileLook>();
                look.profile = id;
                DojoHomeKit.Ensure(look);

                var rig = go.GetComponent<HouseDeviceRig>();
                Assert.IsNotNull(rig, id + " rig");
                Assert.IsNotNull(rig.sala, id + " sala_luz");
                Assert.IsNotNull(rig.quarto, id + " quarto_luz");
                Assert.IsNotNull(rig.abajur, id + " abajur");
                Assert.IsNotNull(rig.fita, id + " fita_led");
                Assert.IsNotNull(rig.tv, id + " tv light");
                Assert.IsTrue(rig.fitaGlow != null && rig.fitaGlow.Length > 0, id + " LED mesh");
                Assert.IsNotNull(go.GetComponentInChildren<TvSet>(true), id + " TvSet");
                Assert.IsNotNull(go.GetComponentInChildren<AirConUnit>(true), id + " AirCon");

                LogAssert.ignoreFailingMessages = true;
                rig.Drive(Dev("sala_luz", false, Color.white, 0f), 1f);
                rig.Drive(Dev("fita_led", false, Color.white, 0f), 1f);
                Assert.IsFalse(rig.sala.enabled, id + " sala off");
                Assert.IsFalse(rig.fita.enabled, id + " led off");

                rig.Drive(Dev("sala_luz", true, Color.magenta, 1f), 1f);
                rig.Drive(Dev("fita_led", true, Color.cyan, 1f), 1f);
                Assert.IsTrue(rig.sala.enabled, id + " sala on");
                Assert.IsTrue(rig.fita.enabled, id + " led on");
                Assert.Greater(rig.sala.color.r, 0.5f, id + " sala color");
                Assert.Greater(rig.fita.color.g, 0.5f, id + " led color");

                var tv = go.GetComponentInChildren<TvSet>(true);
                tv.SetPower(false, false);
                Assert.IsFalse(tv.Power, id + " tv off");
                tv.SetPower(true, false);
                Assert.IsTrue(tv.Power, id + " tv on");

                var ac = go.GetComponentInChildren<AirConUnit>(true);
                ac.Apply(new HouseDeviceState { Id = "ar_condicionado", On = false, Temp = 24, Mode = "frio", Fan = "auto" });
                Assert.IsFalse(ac.Power, id + " ac off");
                ac.Apply(new HouseDeviceState { Id = "ar_condicionado", On = true, Temp = 22, Mode = "frio", Fan = "auto" });
                Assert.IsTrue(ac.Power, id + " ac on");
                LogAssert.ignoreFailingMessages = false;

                Object.DestroyImmediate(go);
                yield return null;
            }
        }

        [Test]
        public void HouseStateJson_ExposesSmartDeviceIds()
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
            Assert.IsTrue(HouseStateLogic.TryDevice(snap.Devices, "ar_condicionado", out var ac) && ac.On);
        }

        static HouseDeviceState Dev(string id, bool on, Color color, float intensity)
        {
            return new HouseDeviceState { Id = id, On = on, Color = color, Intensity = intensity };
        }
    }
}
