using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public enum ProfileId
    {
        Zen = 0,
        Cyber = 1,
        Excentrico = 2,
        Acolhedor = 3
    }

    public readonly struct ProfileInfo
    {
        public readonly ProfileId Id;
        public readonly string Letter;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string Caption;
        public readonly Color Accent;
        public readonly Color Ambient;
        public readonly Color Fog;
        public readonly float FogDensity;
        public readonly Color LightColor;
        public readonly float LightIntensity;

        public ProfileInfo(ProfileId id, string letter, string title, string subtitle, string caption, Color accent,
            Color ambient, Color fog, float fogDensity, Color lightColor, float lightIntensity)
        {
            Id = id;
            Letter = letter;
            Title = title;
            Subtitle = subtitle;
            Caption = caption;
            Accent = accent;
            Ambient = ambient;
            Fog = fog;
            FogDensity = fogDensity;
            LightColor = lightColor;
            LightIntensity = lightIntensity;
        }

        public string Key => Id.ToString();
    }

    public static class Profiles
    {
        public const int Count = 4;

        static readonly ProfileInfo[] All =
        {
            new ProfileInfo(ProfileId.Zen, "Z", "Zen", "Equilibrado",
                "Dojo silencioso.",
                new Color(0.55f, 0.78f, 0.62f), new Color(0.72f, 0.7f, 0.62f), new Color(0.86f, 0.84f, 0.76f), 0.012f,
                new Color(1f, 0.95f, 0.86f), 1.05f),
            new ProfileInfo(ProfileId.Cyber, "C", "Cyber", "Estratégico",
                "Dojo com neon discreto.",
                new Color(0.1f, 0.85f, 1f), new Color(0.22f, 0.26f, 0.32f), new Color(0.1f, 0.13f, 0.18f), 0.018f,
                new Color(0.62f, 0.82f, 1f), 0.55f),
            new ProfileInfo(ProfileId.Excentrico, "E", "Excêntrico", "Criativo",
                "Dojo com cor.",
                new Color(1f, 0.35f, 0.72f), new Color(0.78f, 0.7f, 0.68f), new Color(0.93f, 0.86f, 0.88f), 0.01f,
                new Color(1f, 0.92f, 0.9f), 1.05f),
            new ProfileInfo(ProfileId.Acolhedor, "A", "Acolhedor", "Empático",
                "Dojo quente, plantas e almofadas.",
                new Color(1f, 0.62f, 0.32f), new Color(0.72f, 0.58f, 0.42f), new Color(0.9f, 0.78f, 0.62f), 0.012f,
                new Color(1f, 0.82f, 0.58f), 1.05f)
        };

        public static ProfileInfo Get(ProfileId id)
        {
            return All[(int)id];
        }

        /// <summary>
        /// API strings are case-sensitive: Zen, Cyber, Excentrico, Acolhedor.
        /// Editor fallback (Z/C/E/A, ignore-case) is opt-in and never used on device.
        /// </summary>
        public static bool TryParse(string raw, bool allowEditorFallback, out ProfileId id)
        {
            id = ProfileId.Zen;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            raw = raw.Trim();
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Key == raw)
                {
                    id = All[i].Id;
                    return true;
                }
            }

            if (!allowEditorFallback)
                return false;
            switch (raw.ToUpperInvariant())
            {
                case "Z":
                case "ZEN":
                    id = ProfileId.Zen;
                    return true;
                case "C":
                case "CYBER":
                    id = ProfileId.Cyber;
                    return true;
                case "E":
                case "EXCENTRICO":
                case "EXCÊNTRICO":
                    id = ProfileId.Excentrico;
                    return true;
                case "A":
                case "ACOLHEDOR":
                    id = ProfileId.Acolhedor;
                    return true;
                default:
                    return false;
            }
        }
    }
}
