using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>POST /api/gen/voice-session. No API key leaves the server; the body is an ephemeral token.</summary>
    public static class GenVoiceSessionClient
    {
        static readonly HttpClient Http = NewClient();

        public static void Request(string baseUrl, string sessionId, Action<VoiceSessionTicket> onOk, Action<string> onFailed)
        {
            var root = string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/');
            var url = root + "/api/gen/voice-session";
            var payload = "{\"session_id\":\"" + GenJson.Escape(sessionId ?? "") + "\"}";
            Task.Run(() =>
            {
                try
                {
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var response = Http.PostAsync(url, content).GetAwaiter().GetResult();
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode || !GeminiLiveMessages.TryParseTicket(body, out var ticket) || string.IsNullOrEmpty(ticket.Token))
                    {
                        Fail(onFailed);
                        return;
                    }

                    ticket.BaseUrl = root;
                    ticket.SessionId = sessionId ?? "";
                    if (!string.IsNullOrEmpty(ticket.Provider) && !string.Equals(ticket.Provider, "gemini", StringComparison.OrdinalIgnoreCase))
                    {
                        Fail(onFailed);
                        return;
                    }

                    MainThread.Post(() =>
                    {
                        if (onOk != null)
                            onOk(ticket);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Gen voice-session: " + e.GetType().Name);
                    Fail(onFailed);
                }
            });
        }

        static void Fail(Action<string> onFailed)
        {
            MainThread.Post(() =>
            {
                if (onFailed != null)
                    onFailed("Sessão de voz indisponível.");
            });
        }

        static HttpClient NewClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(12);
            return client;
        }
    }
}
