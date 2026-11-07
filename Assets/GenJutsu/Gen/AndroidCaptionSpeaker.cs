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
            ready = status == 0;
            if (ready && !string.IsNullOrEmpty(pending))
            {
                var text = pending;
                pending = null;
                Speak(text);
            }
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
