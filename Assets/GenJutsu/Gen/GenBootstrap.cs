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

            if (!director.TryGetComponent<GenVoiceController>(out var voice))
                voice = director.gameObject.AddComponent<GenVoiceController>();
            voice.Configure(director.sessionGate, director, url);
            var meter = EnsureTokenMeter(director);
            voice.tokenMeter = meter;
            if (director.sessionGate != null)
                director.sessionGate.tokenMeter = meter;

            if (!director.TryGetComponent<HouseStateApplier>(out var applier))
                applier = director.gameObject.AddComponent<HouseStateApplier>();
            applier.director = director;
            applier.music = director.music != null
                ? director.music
                : director.GetComponent<ProceduralMusic>();

            if (!director.TryGetComponent<HouseStateListener>(out var listener))
                listener = director.gameObject.AddComponent<HouseStateListener>();
            listener.baseUrl = url;
            listener.gate = director.sessionGate;
            listener.applier = applier;
            listener.pollSeconds = 1f;

            voice.HouseJson -= listener.Ingest;
            voice.HouseJson += listener.Ingest;
        }

        static SessionTokenMeter EnsureTokenMeter(ProfileDemoDirector director)
        {
            if (director.sessionGate != null && director.sessionGate.tokenMeter != null)
                return director.sessionGate.tokenMeter;
            var existing = Object.FindFirstObjectByType<SessionTokenMeter>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;
            var parent = director.sessionGate != null
                ? director.sessionGate.transform.parent
                : (director.transform.parent != null ? director.transform.parent : director.transform);
            var go = new GameObject("SessionTokenMeter");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.18f, 0.55f);
            var meter = go.AddComponent<SessionTokenMeter>();
            meter.EnsureVisual();
            meter.Hide();
            return meter;
        }
    }
}
