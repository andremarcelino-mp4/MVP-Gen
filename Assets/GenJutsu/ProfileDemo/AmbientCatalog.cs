namespace GenJutsu.ProfileDemo
{
    public static class AmbientCatalog
    {
        public static string Url(ProfileId id)
        {
            switch (id)
            {
                case ProfileId.Zen:
                    return "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Enchanted%20Valley.mp3";
                case ProfileId.Cyber:
                    return "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Floating%20Cities.mp3";
                case ProfileId.Excentrico:
                    return "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Funkorama.mp3";
                default:
                    return "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Carefree.mp3";
            }
        }

        public static string Credit(ProfileId id)
        {
            switch (id)
            {
                case ProfileId.Zen: return "Kevin MacLeod — Enchanted Valley (CC BY)";
                case ProfileId.Cyber: return "Kevin MacLeod — Floating Cities (CC BY)";
                case ProfileId.Excentrico: return "Kevin MacLeod — Funkorama (CC BY)";
                default: return "Kevin MacLeod — Carefree (CC BY)";
            }
        }
    }
}
