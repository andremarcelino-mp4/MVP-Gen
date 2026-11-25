using System.Globalization;
using System.Text;

namespace GenJutsu.GenCore
{
    public readonly struct FurnitureMoveIntent
    {
        public readonly bool Matched;
        public readonly bool Reset;
        public readonly string FurnitureId;
        public readonly string TargetId;
        public readonly string FurnitureName;
        public readonly string TargetName;
        public readonly string Reply;

        public FurnitureMoveIntent(bool matched, bool reset, string furnitureId, string targetId,
            string furnitureName, string targetName, string reply)
        {
            Matched = matched;
            Reset = reset;
            FurnitureId = furnitureId ?? "";
            TargetId = targetId ?? "";
            FurnitureName = furnitureName ?? "";
            TargetName = targetName ?? "";
            Reply = reply ?? "";
        }

        public static FurnitureMoveIntent None => new FurnitureMoveIntent(false, false, "", "", "", "", "");
    }

    /// <summary>
    /// Offline keyword match: furniture synonym + target synonym (accent/case-insensitive).
    /// </summary>
    public static class FurnitureCommandParser
    {
        static readonly string[] ResetHints =
        {
            "volta tudo", "volta tudo pro lugar", "volta tudo para o lugar",
            "reset", "recoloca", "devolve tudo", "volta os moveis"
        };

        public static FurnitureMoveIntent Parse(string raw, string houseKey)
        {
            var catalog = FurnitureMoveCatalog.ForHouse(houseKey);
            if (catalog == null)
                return FurnitureMoveIntent.None;
            return Parse(raw, catalog);
        }

        public static FurnitureMoveIntent Parse(string raw, FurnitureHouseCatalog catalog)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(raw))
                return FurnitureMoveIntent.None;

            var text = Fold(raw);
            if (text.Length == 0)
                return FurnitureMoveIntent.None;

            if (ContainsAny(text, ResetHints))
            {
                return new FurnitureMoveIntent(true, true, "", "", "", "",
                    "Pronto, voltei tudo pro lugar.");
            }

            var furniture = LongestMatch(text, catalog.Furniture, out var spec);
            var target = LongestMatch(text, catalog.Anchors, out var anchor);
            if (furniture == null || target == null)
                return FurnitureMoveIntent.None;

            var name = spec.DisplayName;
            var place = PlaceWord(anchor.Id);
            var reply = "Pronto, levei " + Article(name) + " " + name + " " + place + ".";
            return new FurnitureMoveIntent(true, false, spec.Id, anchor.Id, name, anchor.Id, reply);
        }

        static string PlaceWord(string id)
        {
            switch (id)
            {
                case "janela": return "pra janela";
                case "centro": return "pro centro";
                case "parede": return "pra parede";
                case "canto": return "pro canto";
                default: return "pra lá";
            }
        }

        static string Article(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "o";
            var n = Fold(name);
            if (n.StartsWith("mesa") || n.StartsWith("luminaria") || n.StartsWith("planta") || n.StartsWith("almofada"))
                return "a";
            return "o";
        }

        static string LongestMatch(string text, FurnitureSpec[] items, out FurnitureSpec spec)
        {
            spec = default;
            var best = 0;
            string id = null;
            for (var i = 0; i < items.Length; i++)
            {
                var syn = items[i].Synonyms;
                for (var s = 0; s < syn.Length; s++)
                {
                    var needle = Fold(syn[s]);
                    if (needle.Length < 3 || needle.Length <= best)
                        continue;
                    if (!ContainsWord(text, needle))
                        continue;
                    best = needle.Length;
                    spec = items[i];
                    id = items[i].Id;
                }
            }

            return id;
        }

        static string LongestMatch(string text, FurnitureAnchorSpec[] items, out FurnitureAnchorSpec spec)
        {
            spec = default;
            var best = 0;
            string id = null;
            for (var i = 0; i < items.Length; i++)
            {
                var syn = items[i].Synonyms;
                for (var s = 0; s < syn.Length; s++)
                {
                    var needle = Fold(syn[s]);
                    if (needle.Length < 4 || needle.Length <= best)
                        continue;
                    if (!ContainsWord(text, needle))
                        continue;
                    best = needle.Length;
                    spec = items[i];
                    id = items[i].Id;
                }
            }

            return id;
        }

        public static string Fold(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            var form = raw.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(form.Length);
            for (var i = 0; i < form.Length; i++)
            {
                var c = form[i];
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        static bool ContainsAny(string text, string[] hints)
        {
            for (var i = 0; i < hints.Length; i++)
            {
                if (text.IndexOf(Fold(hints[i]), System.StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }

        static bool ContainsWord(string text, string needle)
        {
            var i = text.IndexOf(needle, System.StringComparison.Ordinal);
            while (i >= 0)
            {
                var before = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
                var afterIndex = i + needle.Length;
                var after = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
                if (before && after)
                    return true;
                i = text.IndexOf(needle, i + 1, System.StringComparison.Ordinal);
            }

            return false;
        }
    }
}
