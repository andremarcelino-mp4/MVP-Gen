using GenJutsu.GenCore;
using NUnit.Framework;
using UnityEngine;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public class HouseSpawnEditModeTests
    {
        [Test]
        public void Align_PutsOriginOnHouseFloor_AndResetsRigScaleAndCameraOffset()
        {
            var house = new GameObject("House_Zen");
            house.transform.SetPositionAndRotation(new Vector3(0f, 0f, -5.5f), Quaternion.identity);
            var origin = new GameObject("XR Origin");
            origin.transform.SetPositionAndRotation(new Vector3(0f, -0.57f, -6.27f), Quaternion.identity);
            origin.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(origin.transform, false);
            offset.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.transform.SetParent(offset.transform, false);

            try
            {
                HouseSpawn.Align(house.transform);
                var feet = house.transform.TransformPoint(HouseSpawnLogic.LocalFeet);
                Assert.AreEqual(feet.y, origin.transform.position.y, 0.001f);
                Assert.AreEqual(Vector3.one, origin.transform.localScale);
                Assert.AreEqual(0f, offset.transform.localPosition.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(house);
                Object.DestroyImmediate(origin);
            }
        }
    }
}
