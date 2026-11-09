using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GenJutsu.GenCore.PlayModeTests
{
    public class HouseSpawnPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpawnProbe_MissesFurnitureCollidersAndRenderers()
        {
            var root = new GameObject("HouseSpawnProbe");
            for (var i = 0; i < HouseSpawnLogic.Furniture.Length; i++)
            {
                var b = HouseSpawnLogic.Furniture[i];
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = b.Name;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = b.Center;
                go.transform.localScale = b.Half * 2f;
            }

            yield return null;
            var probe = HouseSpawnLogic.LocalFeet + Vector3.up * 0.9f;
            Assert.IsFalse(HouseSpawnLogic.Overlaps(probe, HouseSpawnLogic.ClearRadius, out var hit), "catalog hit " + hit);

            var cols = root.GetComponentsInChildren<Collider>();
            for (var i = 0; i < cols.Length; i++)
            {
                var world = root.transform.TransformPoint(probe);
                Assert.IsFalse(cols[i].bounds.Contains(world), cols[i].name);
            }

            Object.Destroy(root);
        }
    }
}
