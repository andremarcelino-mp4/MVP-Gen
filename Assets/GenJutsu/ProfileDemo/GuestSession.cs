using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Anonymous headset session: pairing code + resolved profile_id. Local only (PlayerPrefs).
    /// </summary>
    public static class GuestSession
    {
        const string CodeKey = "genjutsu.guest.code";
        const string ProfileKey = "genjutsu.guest.profile_id";

        public static string Code { get; private set; } = "";
        public static ProfileId? Profile { get; private set; }

        public static bool HasProfile => Profile.HasValue;

        public static void Load()
        {
            Code = PlayerPrefs.GetString(CodeKey, "");
            var raw = PlayerPrefs.GetString(ProfileKey, "");
            Profile = Profiles.TryParse(raw, allowEditorFallback: false, out var id) ? id : (ProfileId?)null;
        }

        public static void Save(string code, ProfileId profile)
        {
            Code = code ?? "";
            Profile = profile;
            PlayerPrefs.SetString(CodeKey, Code);
            PlayerPrefs.SetString(ProfileKey, Profiles.Get(profile).Key);
            PlayerPrefs.Save();
        }

        public static void ClearProfileKeepCode()
        {
            Profile = null;
            PlayerPrefs.DeleteKey(ProfileKey);
            PlayerPrefs.Save();
        }
    }
}
