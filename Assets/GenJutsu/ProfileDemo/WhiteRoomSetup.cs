using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// The white room keeps the brush, dragon scroll, teapot and archery target.
    /// They live under the table set so the morph dissolve takes them with it.
    /// </summary>
    public static class WhiteRoomSetup
    {
        static readonly string[] PropNames = { "Brush", "BrushRest", "Scroll", "JadeDragon", "Teapot", "FloatingTarget" };

        public static void Ensure(DojoDirector director)
        {
            var table = GameObject.Find("JapaneseTableSet");
            if (table != null)
                table.SetActive(true);
            ShowProps();
            StartPlaque.Ensure(table != null ? table.transform : null, director);
        }

        static void ShowProps()
        {
            var table = GameObject.Find("JapaneseTableSet");
            var wanted = new HashSet<string>(PropNames);
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < transforms.Length; i++)
            {
                var t = transforms[i];
                if (t == null || !wanted.Contains(t.name))
                    continue;
                if (table != null && t.parent != table.transform)
                    t.SetParent(table.transform, true);
                var p = t;
                while (p != null)
                {
                    p.gameObject.SetActive(true);
                    if (p.name == "JapaneseTableSet")
                        break;
                    p = p.parent;
                }
            }
        }
    }
}
