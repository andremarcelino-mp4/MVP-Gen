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
                    GenVoiceLog.Info("POST " + url);
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var response = Http.PostAsync(url, content).GetAwaiter().GetResult();
                    GenVoiceLog.Info("voice-session HTTP " + (int)response.StatusCode);
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        Fail(onFailed, "voice-session HTTP " + (int)response.StatusCode);
                        return;
                    }

                    if (!GeminiLiveMessages.TryParseTicket(body, out var ticket) || string.IsNullOrEmpty(ticket.Token))
                    {
                        Fail(onFailed, "voice-session sem token efémero.");
                        return;
                    }

                    ticket.BaseUrl = root;
                    ticket.SessionId = sessionId ?? "";
                    if (!string.IsNullOrEmpty(ticket.Provider) && !string.Equals(ticket.Provider, "gemini", StringComparison.OrdinalIgnoreCase))
                    {
                        Fail(onFailed, "voice-session provider " + ticket.Provider);
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
                    var why = e.GetType().Name + ": " + e.Message;
                    if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                        why += " | " + e.InnerException.GetType().Name + ": " + e.InnerException.Message;
                    why = GeminiLiveMessages.Scrub(why);
                    GenVoiceLog.Warn("voice-session: " + why);
                    Fail(onFailed, why);
                }
            });
        }

        static void Fail(Action<string> onFailed, string why)
        {
            var message = string.IsNullOrWhiteSpace(why) ? "Sessão de voz indisponível." : why;
            MainThread.Post(() =>
            {
                if (onFailed != null)
                    onFailed(message);
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
