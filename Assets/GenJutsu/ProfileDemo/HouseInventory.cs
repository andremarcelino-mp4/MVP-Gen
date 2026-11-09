using System.Collections.Generic;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public static class HouseInventory
    {
        public static bool HasAll(ProfileLook look, out string missing)
        {
            missing = null;
            if (look == null)
            {
                missing = "look";
                return false;
            }

            var names = new HashSet<string>();
            var tr = look.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < tr.Length; i++)
                names.Add(tr[i].name);
            for (var i = 0; i < HouseSpawnLogic.RequiredProps.Length; i++)
            {
                var n = HouseSpawnLogic.RequiredProps[i];
                if (!names.Contains(n))
                {
                    missing = n;
                    return false;
                }
            }

            return look.GetComponentInChildren<TvSet>(true) != null
                   && look.GetComponentInChildren<AirConUnit>(true) != null;
        }
    }
}
