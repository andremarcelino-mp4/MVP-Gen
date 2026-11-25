using GenJutsu.GenCore;
using NUnit.Framework;
using UnityEngine;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public class FurnitureLayoutTests
    {
        [Test]
        public void EachHouse_GetsFurnitureAndSafeAnchors()
        {
            for (var h = 0; h < FurnitureMoveCatalog.Houses.Length; h++)
            {
                var catalog = FurnitureMoveCatalog.Houses[h];
                var root = new GameObject("House_" + catalog.HouseKey);
                try
                {
                    Seed(root.transform, catalog);
                    var mover = FurnitureLayout.Ensure(root.transform, catalog.HouseKey);
                    Assert.IsNotNull(mover, catalog.HouseKey);
                    Assert.AreEqual(catalog.Furniture.Length, FurnitureLayout.CountFurniture(root.transform), catalog.HouseKey);
                    Assert.AreEqual(catalog.Anchors.Length, FurnitureLayout.CountAnchors(root.transform), catalog.HouseKey);

                    var anchors = root.GetComponentsInChildren<FurnitureAnchor>(true);
                    Assert.AreEqual(4, anchors.Length, catalog.HouseKey);
                    var seen = new System.Collections.Generic.HashSet<string>();
                    for (var i = 0; i < anchors.Length; i++)
                    {
                        Assert.IsTrue(seen.Add(anchors[i].AnchorId), catalog.HouseKey + " " + anchors[i].AnchorId);
                        var p = anchors[i].transform.localPosition;
                        Assert.Less(Mathf.Abs(p.x), 2.2f, catalog.HouseKey + " " + anchors[i].AnchorId);
                        Assert.Less(Mathf.Abs(p.z), 2.2f, catalog.HouseKey + " " + anchors[i].AnchorId);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        static void Seed(Transform root, FurnitureHouseCatalog catalog)
        {
            for (var i = 0; i < catalog.Furniture.Length; i++)
            {
                var names = catalog.Furniture[i].HierarchyNames;
                if (names == null || names.Length == 0)
                    continue;
                var go = new GameObject(names[0]);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(i * 0.2f, 0.1f, 0f);
            }
        }
    }
}
