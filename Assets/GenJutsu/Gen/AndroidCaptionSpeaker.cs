using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Quest TTS for the chat fallback. The caption is always shown.
    /// On Android (the Quest player) this also speaks through android.speech.tts.TextToSpeech.
    /// In the Editor, and on any non-Android player, there is no TTS — captions only.
    /// </summary>
    public sealed class AndroidCaptionSpeaker : MonoBehaviour
    {
        public const string Mode =
            "Android TextToSpeech on the Quest player. Editor and other players show the caption only.";

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject tts;
        bool ready;
        string pending;
        InitProxy proxy;

        void Awake()
        {
            proxy = new InitProxy(this);
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, proxy);
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (!ready || tts == null)
            {
                pending = text;
                return;
            }

            tts.Call<int>("speak", text, 0, null, "gen");
        }

        void OnReady(int status)
        {
            GenVoiceLog.Info("TTS onInit status=" + status);
            ready = status == 0;
            if (ready)
                ChooseMaleVoice();
            if (ready && !string.IsNullOrEmpty(pending))
            {
                var text = pending;
                pending = null;
                Speak(text);
            }
        }

        void ChooseMaleVoice()
        {
            try
            {
                var locale = new AndroidJavaObject("java.util.Locale", "pt", "BR");
                var langResult = tts.Call<int>("setLanguage", locale);
                GenVoiceLog.Info("TTS setLanguage pt-BR result=" + langResult);
                var voices = tts.Call<AndroidJavaObject>("getVoices");
                string chosen = null;
                if (voices != null)
                {
                    var it = voices.Call<AndroidJavaObject>("iterator");
                    while (it.Call<bool>("hasNext"))
                    {
                        var voice = it.Call<AndroidJavaObject>("next");
                        if (!IsPortugueseMale(voice))
                            continue;
                        var name = voice.Call<string>("getName");
                        var vloc = voice.Call<AndroidJavaObject>("getLocale");
                        var country = vloc != null ? vloc.Call<string>("getCountry") : "";
                        tts.Call<int>("setVoice", voice);
                        chosen = name + " pt-" + country;
                        if (country == "BR")
                            break;
                    }
                }

                if (chosen == null)
                {
                    tts.Call<int>("setPitch", 0.85f);
                    GenVoiceLog.Info("TTS no pt-BR male voice, pitch 0.85");
                }
                else
                    GenVoiceLog.Info("TTS male voice " + chosen);
            }
            catch (System.Exception e)
            {
                GenVoiceLog.Warn("TTS voice select: " + e.Message);
                try { tts.Call<int>("setPitch", 0.85f); }
                catch (System.Exception) { /* pitch is optional */ }
            }
        }

        static bool IsPortugueseMale(AndroidJavaObject voice)
        {
            var locale = voice.Call<AndroidJavaObject>("getLocale");
            var lang = locale != null ? locale.Call<string>("getLanguage") : "";
            if (lang != "pt")
                return false;
            var name = voice.Call<string>("getName") ?? "";
            if (name.IndexOf("male", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            var features = voice.Call<AndroidJavaObject>("getFeatures");
            if (features == null)
                return false;
            var it = features.Call<AndroidJavaObject>("iterator");
            while (it.Call<bool>("hasNext"))
            {
                var feature = it.Call<string>("next");
                if (feature != null && feature.IndexOf("male", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        void OnDestroy()
        {
            if (tts != null)
            {
                tts.Call("shutdown");
                tts.Dispose();
                tts = null;
            }
        }

        sealed class InitProxy : AndroidJavaProxy
        {
            readonly AndroidCaptionSpeaker owner;

            public InitProxy(AndroidCaptionSpeaker owner) : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                this.owner = owner;
            }

            public void onInit(int status)
            {
                owner.OnReady(status);
            }
        }
#else
        public void Speak(string text)
        {
        }
#endif
    }
}
