using GenJutsu.HouseState;
using GenJutsu.ProfileDemo;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>Adds Gen voice and the house-state loop onto the dojo that is already in the scene.</summary>
    public static class GenBootstrap
    {
        public static void Ensure(ProfileDemoDirector director)
        {
            if (director == null)
                return;
            var api = director.GetComponent<SessionApiClient>();
            var url = api != null && !string.IsNullOrEmpty(api.baseUrl)
                ? api.baseUrl
                : "https://genjutsuai.vercel.app";

            var voice = director.GetComponent<GenVoiceController>() ?? director.gameObject.AddComponent<GenVoiceController>();
            voice.Configure(director.sessionGate, director, url);

            var applier = director.GetComponent<HouseStateApplier>() ?? director.gameObject.AddComponent<HouseStateApplier>();
            applier.director = director;
            applier.music = director.music != null
                ? director.music
                : director.GetComponent<ProceduralMusic>();

            var listener = director.GetComponent<HouseStateListener>() ?? director.gameObject.AddComponent<HouseStateListener>();
            listener.baseUrl = url;
            listener.gate = director.sessionGate;
            listener.applier = applier;
            listener.pollSeconds = 1f;

            voice.HouseJson -= listener.Ingest;
            voice.HouseJson += listener.Ingest;
        }
    }
}
