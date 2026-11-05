using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Headset mirror of the admin-selected guest. Local cache only (PlayerPrefs).
    /// </summary>
    public static class GuestSession
    {
        const string CodeKey = "genjutsu.guest.code";
        const string ProfileKey = "genjutsu.guest.profile_id";
        const string NameKey = "genjutsu.guest.display_name";

        public static string Code { get; private set; } = "";
        public static string DisplayName { get; private set; } = "";
        public static ProfileId? Profile { get; private set; }

        public static bool HasProfile => Profile.HasValue;

        public static void Load()
        {
            Code = PlayerPrefs.GetString(CodeKey, "");
            DisplayName = PlayerPrefs.GetString(NameKey, "");
            var raw = PlayerPrefs.GetString(ProfileKey, "");
            Profile = Profiles.TryParse(raw, allowEditorFallback: false, out var id) ? id : (ProfileId?)null;
        }

        public static void Save(string code, string displayName, ProfileId? profile)
        {
            Code = code ?? "";
            DisplayName = displayName ?? "";
            Profile = profile;
            PlayerPrefs.SetString(CodeKey, Code);
            PlayerPrefs.SetString(NameKey, DisplayName);
            if (profile.HasValue)
                PlayerPrefs.SetString(ProfileKey, Profiles.Get(profile.Value).Key);
            else
                PlayerPrefs.DeleteKey(ProfileKey);
            PlayerPrefs.Save();
        }
    }
}
