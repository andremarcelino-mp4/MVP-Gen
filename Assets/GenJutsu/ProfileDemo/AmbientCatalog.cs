namespace GenJutsu.ProfileDemo
{
    public static class AmbientCatalog
    {
        public static string Url(ProfileId id)
        {
            switch (id)
            {
                case ProfileId.Zen:
                    return "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/no_curator/Broke_For_Free/Directionless_EP/Broke_For_Free_-_01_-_Night_Owl.mp3";
                case ProfileId.Cyber:
                    return "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/ccCommunity/Kai_Engel/Lights/Kai_Engel_-_04_-_Iridescent.mp3";
                case ProfileId.Excentrico:
                    return "https://files.freemusicarchive.org/storage-freemusicarchive-org/music/WFMU/Jahzzar/Tumbling_Dishes_Like_Old-Mans_Wheels/Jahzzar_-_05_-_Siesta.mp3";
                default:
                    return "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Carefree.mp3";
            }
        }

        public static string Credit(ProfileId id)
        {
            switch (id)
            {
                case ProfileId.Zen: return "Broke For Free — Night Owl (CC BY)";
                case ProfileId.Cyber: return "Kai Engel — Iridescent (CC BY)";
                case ProfileId.Excentrico: return "Jahzzar — Siesta (CC BY-SA)";
                default: return "Kevin MacLeod — Carefree (CC BY)";
            }
        }
    }
}
