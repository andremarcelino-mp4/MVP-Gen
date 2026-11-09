using UnityEngine;

namespace GenJutsu.Gen
{
    public static class GenVoiceLog
    {
        public static void Info(string message)
        {
            Debug.Log("[GenVoice] " + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning("[GenVoice] " + message);
        }
    }
}
