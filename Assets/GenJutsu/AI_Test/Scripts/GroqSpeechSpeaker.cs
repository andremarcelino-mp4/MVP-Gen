using System;
using System.Collections;
using UnityEngine;

namespace GenJutsu.AITest
{
    public class GroqSpeechSpeaker : MonoBehaviour
    {
        public static readonly string[] EnglishVoices = { "autumn", "diana", "hannah", "austin", "daniel", "troy" };

        public AudioSource audioSource;
        public GroqClient client;

        public IEnumerator Speak(string text, string voice, string apiKey, AIConfig config, Action<string> onError)
        {
            if (client == null)
                client = GetComponent<GroqClient>();
            GroqCallResult result = null;
            yield return client.Synthesize(Trim(text), config.groqTtsModel, voice, apiKey, value => result = value);
            if (result == null || !result.Success || result.AudioWav == null)
            {
                onError?.Invoke(result != null ? result.ErrorMessage : "Corvo não conseguiu responder.");
                yield break;
            }

            var clip = WavAudio.ToClip(result.AudioWav, "CorvoGroq");
            if (clip == null)
            {
                Debug.LogError("[GenJutsu] Etapa voz Groq: WAV inválido.");
                onError?.Invoke("Corvo não conseguiu responder.");
                yield break;
            }

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
            while (audioSource.isPlaying)
                yield return null;
        }

        static string Trim(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 180)
                return text;
            return text.Substring(0, 180);
        }
    }
}
