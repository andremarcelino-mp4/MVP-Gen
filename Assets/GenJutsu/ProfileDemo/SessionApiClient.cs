using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// TLS GET /api/session — current guest chosen by /admin. No secrets.
    /// Does not invent a look when the API is down or profile_id is missing.
    /// </summary>
    public class SessionApiClient : MonoBehaviour
    {
        public string baseUrl = "https://genjutsuai.vercel.app";
        public float timeoutSeconds = 8f;

        public enum Kind
        {
            WaitingForAdmin,
            Ready,
            Incomplete,
            Down
        }

        public struct Result
        {
            public Kind kind;
            public ProfileId profile;
            public bool hasProfile;
            public string rawProfile;
            public string displayName;
            public string sessionCode;
            public string error;
            public long httpStatus;
        }

        /// <summary>Active session selected in /admin. No code typed on the headset.</summary>
        public IEnumerator FetchActive(Action<Result> done)
        {
            var url = baseUrl.TrimEnd('/') + "/api/session";
            using var req = UnityWebRequest.Get(url);
            req.timeout = Mathf.CeilToInt(timeoutSeconds);
            var op = req.SendWebRequest();
            while (!op.isDone)
                yield return null;

            var body = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (req.responseCode == 404)
            {
                TryJsonString(body, "error", out var err);
                done?.Invoke(new Result
                {
                    kind = Kind.WaitingForAdmin,
                    httpStatus = 404,
                    error = err == "no_current_session"
                        ? "À espera que o anfitrião escolha a sessão no admin."
                        : "Nenhuma sessão activa."
                });
                yield break;
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                done?.Invoke(new Result
                {
                    kind = Kind.Down,
                    httpStatus = req.responseCode,
                    error = "Servidor indisponível. Permaneça no espaço branco."
                });
                yield break;
            }

            TryJsonString(body, "display_name", out var name);
            if (string.IsNullOrEmpty(name))
                TryJsonString(body, "name", out name);
            TryJsonString(body, "session_code", out var code);
            if (string.IsNullOrEmpty(code))
                TryJsonString(body, "code", out code);

            var hasRaw = TryExtractProfileId(body, out var raw);
            ProfileId id = default;
            var parsed = hasRaw && Profiles.TryParse(raw, allowEditorFallback: false, out id);
            done?.Invoke(new Result
            {
                kind = parsed ? Kind.Ready : Kind.Incomplete,
                hasProfile = parsed,
                profile = parsed ? id : default,
                rawProfile = raw,
                displayName = name,
                sessionCode = code,
                httpStatus = req.responseCode,
                error = parsed
                    ? null
                    : "Sessão sem profile_id ZCEA. Permaneça no branco."
            });
        }

        public static bool TryExtractProfileId(string json, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(json))
                return false;
            if (!TryJsonString(json, "profile_id", out value) && !TryJsonString(json, "profileId", out value))
                return false;
            return !string.IsNullOrEmpty(value) && value != "null";
        }

        public static bool TryJsonString(string json, string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(json))
                return false;
            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0)
                return false;
            i = json.IndexOf(':', i + needle.Length);
            if (i < 0)
                return false;
            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
            if (i >= json.Length)
                return false;
            if (json[i] == 'n' && json.IndexOf("null", i, StringComparison.Ordinal) == i)
                return false;
            if (json[i] != '"')
                return false;
            i++;
            var end = json.IndexOf('"', i);
            if (end < 0)
                return false;
            value = json.Substring(i, end - i);
            return true;
        }
    }
}
