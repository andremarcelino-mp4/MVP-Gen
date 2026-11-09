using System;
using System.Collections;
using System.Text;
using GenJutsu.GenCore;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.Gen
{
    /// <summary>
    /// POST /api/gen/voice-session. No API key leaves the server; the body is an ephemeral token.
    /// Uses UnityWebRequest, the same stack as the session poll that already works on the Quest.
    /// One attempt per call. HTTP 429 blocks further posts until Retry-After (or 30s) elapses.
    /// </summary>
    public static class GenVoiceSessionClient
    {
        static DateTime nextAllowedUtc = DateTime.MinValue;

        public static bool CoolingDown(out int seconds)
        {
            var left = nextAllowedUtc - DateTime.UtcNow;
            if (left.TotalSeconds <= 0)
            {
                seconds = 0;
                return false;
            }

            seconds = (int)Math.Ceiling(left.TotalSeconds);
            return true;
        }

        public static IEnumerator Post(string baseUrl, string sessionId, Action<VoiceSessionTicket> onOk, Action<string> onFailed)
        {
            if (CoolingDown(out var wait))
            {
                GenVoiceLog.Warn("voice-session skipped, cooling down " + wait + "s");
                Fail(onFailed, "voice-session HTTP 429");
                yield break;
            }

            var root = string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/');
            var url = root + "/api/gen/voice-session";
            var payload = string.IsNullOrEmpty(sessionId) ? "{}" : "{\"session_id\":\"" + GenJson.Escape(sessionId) + "\"}";
            GenVoiceLog.Info("POST " + url + " body=" + (payload == "{}" ? "{}" : "session_id"));
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 12;
                yield return req.SendWebRequest();

                var status = req.responseCode;
                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                GenVoiceLog.Info("voice-session HTTP " + status);
                if (status == 429)
                {
                    var retry = 30;
                    var header = req.GetResponseHeader("Retry-After");
                    if (int.TryParse(header, out var parsed) && parsed > 0)
                        retry = parsed > 120 ? 120 : parsed;
                    nextAllowedUtc = DateTime.UtcNow.AddSeconds(retry);
                    GenVoiceLog.Warn("voice-session HTTP 429, next try in " + retry + "s");
                    Fail(onFailed, "voice-session HTTP 429");
                    yield break;
                }

                if (req.result != UnityWebRequest.Result.Success || status < 200 || status >= 300)
                {
                    var why = "voice-session HTTP " + status;
                    if (!string.IsNullOrEmpty(req.error))
                        why = why + " " + req.error;
                    Fail(onFailed, why);
                    yield break;
                }

                if (!GeminiLiveMessages.TryParseTicket(body, out var ticket)
                    || (string.IsNullOrEmpty(ticket.Token) && string.IsNullOrEmpty(ticket.WsUrl)))
                {
                    Fail(onFailed, "voice-session sem token efémero.");
                    yield break;
                }

                ticket.BaseUrl = root;
                ticket.SessionId = sessionId ?? "";
                if (!string.IsNullOrEmpty(ticket.Provider) && !string.Equals(ticket.Provider, "gemini", StringComparison.OrdinalIgnoreCase))
                {
                    Fail(onFailed, "voice-session provider " + ticket.Provider);
                    yield break;
                }

                if (onOk != null)
                    onOk(ticket);
            }
        }

        static void Fail(Action<string> onFailed, string why)
        {
            var message = string.IsNullOrWhiteSpace(why) ? "Sessão de voz indisponível." : why;
            if (onFailed != null)
                onFailed(message);
        }
    }
}
