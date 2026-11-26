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

        [Test]
        public void HouseGrabPhysics_GivesGravityToProps_AndSolidFloorColliders()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Deck";
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            Object.DestroyImmediate(table.GetComponent<Collider>());
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = "Ball";
            var grab = prop.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.retainTransformParent = true;
            grab.throwOnDetach = false;
            grab.forceGravityOnDetach = false;
            var body = prop.GetComponent<Rigidbody>();
            if (body == null)
                body = prop.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var token = GameObject.CreatePrimitive(PrimitiveType.Cube);
            token.name = "Token";
            token.AddComponent<ProfileToken>();
            token.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var tokenBody = token.GetComponent<Rigidbody>();
            if (tokenBody == null)
                tokenBody = token.AddComponent<Rigidbody>();
            tokenBody.isKinematic = true;
            tokenBody.useGravity = false;

            try
            {
                HouseGrabPhysics.Apply();
                Assert.IsFalse(body.isKinematic);
                Assert.IsTrue(body.useGravity);
                Assert.IsTrue(grab.throwOnDetach);
                Assert.IsTrue(grab.forceGravityOnDetach);
                Assert.IsFalse(grab.retainTransformParent);
                Assert.IsTrue(HouseGrabPhysics.HasSolidCollider(floor));
                Assert.IsTrue(HouseGrabPhysics.HasSolidCollider(table));
                Assert.IsTrue(tokenBody.isKinematic);
                Assert.IsFalse(tokenBody.useGravity);
            }
            finally
            {
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(table);
                Object.DestroyImmediate(prop);
                Object.DestroyImmediate(token);
                var hub = GameObject.Find("HubFloor");
                if (hub != null)
                    Object.DestroyImmediate(hub);
            }
        }
    }
}
