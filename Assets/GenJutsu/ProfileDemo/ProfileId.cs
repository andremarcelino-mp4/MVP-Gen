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
                "Um dojo silencioso. Respire, equilibre as pedras e deixe a areia guiar a mente.",
                new Color(0.55f, 0.78f, 0.62f), new Color(0.62f, 0.66f, 0.6f), new Color(0.78f, 0.82f, 0.76f), 0.035f,
                new Color(1f, 0.96f, 0.88f), 0.9f),
            new ProfileInfo(ProfileId.Cyber, "C", "Cyber", "Estratégico",
                "Uma central de dados. Memorize padrões, organize a informação e acerte cada alvo.",
                new Color(0.1f, 0.85f, 1f), new Color(0.14f, 0.2f, 0.32f), new Color(0.02f, 0.05f, 0.1f), 0.06f,
                new Color(0.55f, 0.75f, 1f), 0.45f),
            new ProfileInfo(ProfileId.Excentrico, "E", "Excêntrico", "Criativo",
                "Um ateliê sem regras. Pinte, monte esculturas estranhas e invente melodias.",
                new Color(1f, 0.3f, 0.75f), new Color(0.6f, 0.45f, 0.58f), new Color(0.95f, 0.72f, 0.88f), 0.03f,
                new Color(1f, 0.85f, 0.9f), 1f),
            new ProfileInfo(ProfileId.Acolhedor, "A", "Acolhedor", "Empático",
                "Uma casa quentinha. Cuide das plantas, sirva o gato e mantenha a lareira acesa.",
                new Color(1f, 0.62f, 0.3f), new Color(0.5f, 0.38f, 0.28f), new Color(0.32f, 0.2f, 0.12f), 0.04f,
                new Color(1f, 0.78f, 0.55f), 0.7f)
        };

        public static ProfileInfo Get(ProfileId id)
        {
            return All[(int)id];
        }
    }
}
