using System.Collections;
using System.Text;
using GenJutsu.GenCore;
using GenJutsu.ProfileDemo;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Posts one Quest line to the site history. No API key. A 404 or 429 is logged once and not retried.
    /// The site persists this in Supabase; the body is session_id, role, text, kind, source.
    /// </summary>
    public static class GenHistoryClient
    {
        static bool missingLogged;

        /// <summary>QA no Editor: não grava nada no histórico do site.</summary>
        public static bool Muted;

        public static void Note(string baseUrl, string role, string text, string kind)
        {
            if (Muted || string.IsNullOrWhiteSpace(text))
                return;
            var root = string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/');
            var body = GeminiLiveMessages.HistoryBody(GuestSession.Code, role, text, kind);
            MainThread.Run(Post(root + "/api/gen/history", body));
        }

        static IEnumerator Post(string url, string body)
        {
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 8;
                yield return req.SendWebRequest();
                var status = req.responseCode;
                if (status >= 200 && status < 300)
                {
                    GenVoiceLog.Info("history HTTP " + status);
                    yield break;
                }

                if (status == 404 || status == 405)
                {
                    if (!missingLogged)
                    {
                        missingLogged = true;
                        GenVoiceLog.Warn("history HTTP " + status + " (endpoint not on the server yet)");
                    }

                    yield break;
                }

                GenVoiceLog.Warn("history HTTP " + status);
            }
        }
    }
}
