using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Batch fallback: multipart WAV to POST /api/gen/chat. The Groq key stays on the server.
    /// </summary>
    public sealed class GroqFallbackTransport : IVoiceTransport
    {
        static readonly HttpClient Http = NewClient();

        public bool IsLive => false;

        public void Submit(string baseUrl, string sessionId, byte[] wav, Action<ChatReply> onReply, Action<string> onFailed)
        {
            if (wav == null || wav.Length < 46)
            {
                MainThread.Post(() =>
                {
                    if (onFailed != null)
                        onFailed("Áudio vazio.");
                });
                return;
            }

            var url = (string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/')) + "/api/gen/chat";
            Task.Run(() =>
            {
                try
                {
                    using var form = new MultipartFormDataContent();
                    var audio = new ByteArrayContent(wav);
                    audio.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                    form.Add(audio, "audio", "turn.wav");
                    if (!string.IsNullOrEmpty(sessionId))
                        form.Add(new StringContent(sessionId, Encoding.UTF8), "session_id");
                    var response = Http.PostAsync(url, form).GetAwaiter().GetResult();
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode || !ChatReplyParser.TryParse(body, out var reply))
                    {
                        MainThread.Post(() =>
                        {
                            if (onFailed != null)
                                onFailed("O pedido de voz de reserva falhou.");
                        });
                        return;
                    }

                    MainThread.Post(() =>
                    {
                        if (onReply != null)
                            onReply(reply);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Gen chat fallback: " + e.GetType().Name);
                    MainThread.Post(() =>
                    {
                        if (onFailed != null)
                            onFailed("O pedido de voz de reserva falhou.");
                    });
                }
            });
        }

        public void SendRaw(string json)
        {
        }

        public void Close()
        {
        }

        static HttpClient NewClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(25);
            return client;
        }
    }
}
