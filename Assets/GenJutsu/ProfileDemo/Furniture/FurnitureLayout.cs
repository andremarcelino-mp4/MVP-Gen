using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public static class FurnitureLayout
    {
        public static FurnitureMover Ensure(Transform house, string houseKey)
        {
            if (house == null)
                return null;
            var catalog = FurnitureMoveCatalog.ForHouse(houseKey);
            if (catalog == null)
                return null;

            var mover = house.GetComponent<FurnitureMover>();
            if (mover == null)
                mover = house.gameObject.AddComponent<FurnitureMover>();

            TagFurniture(house, catalog);
            PlaceAnchors(house, catalog);
            mover.Bind(house, catalog);
            return mover;
        }

        public static int CountFurniture(Transform house)
        {
            return house == null ? 0 : house.GetComponentsInChildren<MovableFurniture>(true).Length;
        }

        public static int CountAnchors(Transform house)
        {
            return house == null ? 0 : house.GetComponentsInChildren<FurnitureAnchor>(true).Length;
        }

        static void TagFurniture(Transform house, FurnitureHouseCatalog catalog)
        {
            for (var i = 0; i < catalog.Furniture.Length; i++)
            {
                var spec = catalog.Furniture[i];
                var go = FindNamed(house, spec.HierarchyNames);
                if (go == null)
                    continue;
                var mark = go.GetComponent<MovableFurniture>();
                if (mark == null)
                    mark = go.AddComponent<MovableFurniture>();
                mark.FurnitureId = spec.Id;
                mark.DisplayName = spec.DisplayName;
                mark.CaptureHome();
            }
        }

        static void PlaceAnchors(Transform house, FurnitureHouseCatalog catalog)
        {
            var root = house.Find("FurnitureMoveAnchors");
            if (root == null)
            {
                var go = new GameObject("FurnitureMoveAnchors");
                go.transform.SetParent(house, false);
                root = go.transform;
            }

            for (var i = 0; i < catalog.Anchors.Length; i++)
            {
                var spec = catalog.Anchors[i];
                Transform child = null;
                for (var c = 0; c < root.childCount; c++)
                {
                    if (root.GetChild(c).name == spec.Id)
                    {
                        child = root.GetChild(c);
                        break;
                    }
                }

                if (child == null)
                {
                    var go = new GameObject(spec.Id);
                    go.transform.SetParent(root, false);
                    child = go.transform;
                }

                child.localPosition = spec.LocalPosition;
                child.localRotation = Quaternion.Euler(0f, spec.Yaw, 0f);
                var anchor = child.GetComponent<FurnitureAnchor>();
                if (anchor == null)
                    anchor = child.gameObject.AddComponent<FurnitureAnchor>();
                anchor.AnchorId = spec.Id;
                anchor.Yaw = spec.Yaw;
            }
        }

        static GameObject FindNamed(Transform root, string[] names)
        {
            if (names == null)
                return null;
            for (var i = 0; i < names.Length; i++)
            {
                var found = FindByName(root, names[i]);
                if (found != null)
                    return found;
            }

            return null;
        }

        static GameObject FindByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;
            if (root.name == name)
                return root.gameObject;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                    return all[i].gameObject;
            }

            return null;
        }
    }
}
