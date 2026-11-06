using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// TLS GET /api/session — current guest chosen by /admin. No secrets.
    /// Does not invent a look when the API is down or profile_id is missing.
    /// A missing active session (404) and an explicit ended/encerrado body both mean the visit is over.
    /// See docs/CONTRATO-SESSAO-QUEST.md.
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
            Down,
            Ended
        }

        public struct Result
        {
            public Kind kind;
            public ProfileId profile;
            public bool hasProfile;
            public string rawProfile;
            public string displayName;
            public string sessionCode;
            public string sessionKey;
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
            done?.Invoke(Parse(body, req.responseCode, req.result == UnityWebRequest.Result.Success));
        }

        /// <summary>
        /// Pure parse of GET /api/session. httpSuccess is the transport result, not "a visit exists".
        /// genjustuai PR #9: 200 {session:{id, status:'active', ...}} or 200 {session:null}.
        /// Flat JSON and HTTP 404 still count so an older deploy returns the visitor to white.
        /// </summary>
        public static Result Parse(string body, long httpStatus, bool httpSuccess)
        {
            if (httpStatus == 404 || httpStatus == 204)
                return NoActive(body, httpStatus);

            if (!httpSuccess)
            {
                return new Result
                {
                    kind = Kind.Down,
                    httpStatus = httpStatus,
                    error = "Servidor indisponível. Permaneça no espaço branco."
                };
            }

            if (TryGetSessionPayload(body, out var payload, out var sessionNull))
            {
                if (sessionNull || string.IsNullOrEmpty(payload))
                    return NoActive(body, httpStatus);
                return ParseVisit(payload, httpStatus);
            }

            return ParseVisit(body ?? "", httpStatus);
        }

        static Result NoActive(string body, long httpStatus)
        {
            TryJsonString(body, "error", out var err);
            return new Result
            {
                kind = Kind.WaitingForAdmin,
                httpStatus = httpStatus,
                error = err == "no_current_session"
                    ? "À espera que o anfitrião escolha a sessão no admin."
                    : "Nenhuma sessão activa."
            };
        }

        static Result ParseVisit(string payload, long httpStatus)
        {
            if (IsClosedSession(payload))
            {
                TryJsonString(payload, "display_name", out var endedName);
                if (string.IsNullOrEmpty(endedName))
                    TryJsonString(payload, "name", out endedName);
                return new Result
                {
                    kind = Kind.Ended,
                    displayName = endedName,
                    sessionCode = ReadCode(payload),
                    sessionKey = SessionKeyFrom(payload),
                    httpStatus = httpStatus,
                    error = "Sessão encerrada pelo anfitrião."
                };
            }

            TryJsonString(payload, "display_name", out var name);
            if (string.IsNullOrEmpty(name))
                TryJsonString(payload, "name", out name);
            var code = ReadCode(payload);
            var key = SessionKeyFrom(payload);

            var hasRaw = TryExtractProfileId(payload, out var raw);
            ProfileId id = default;
            var parsed = hasRaw && Profiles.TryParse(raw, allowEditorFallback: false, out id);
            return new Result
            {
                kind = parsed ? Kind.Ready : Kind.Incomplete,
                hasProfile = parsed,
                profile = parsed ? id : default,
                rawProfile = raw,
                displayName = name,
                sessionCode = code,
                sessionKey = string.IsNullOrEmpty(key) ? code : key,
                httpStatus = httpStatus,
                error = parsed
                    ? null
                    : "Sessão sem profile_id ZCEA. Permaneça no branco."
            };
        }

        /// <summary>
        /// Reads the PR #9 wrapper. Returns false when the body is a legacy flat summary.
        /// </summary>
        public static bool TryGetSessionPayload(string json, out string payload, out bool sessionNull)
        {
            payload = null;
            sessionNull = false;
            if (string.IsNullOrEmpty(json))
                return false;
            const string needle = "\"session\"";
            var found = 0;
            while (found < json.Length)
            {
                var i = json.IndexOf(needle, found, StringComparison.Ordinal);
                if (i < 0)
                    return false;
                if (i > 0 && IsKeyChar(json[i - 1]))
                {
                    found = i + needle.Length;
                    continue;
                }

                var colon = json.IndexOf(':', i + needle.Length);
                if (colon < 0)
                    return false;
                var c = colon + 1;
                while (c < json.Length && char.IsWhiteSpace(json[c]))
                    c++;
                if (c >= json.Length)
                    return false;
                if (c + 4 <= json.Length && string.CompareOrdinal(json, c, "null", 0, 4) == 0)
                {
                    sessionNull = true;
                    return true;
                }

                if (json[c] != '{')
                    return false;
                var depth = 0;
                var start = c;
                for (; c < json.Length; c++)
                {
                    if (json[c] == '{')
                        depth++;
                    else if (json[c] == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            payload = json.Substring(start, c - start + 1);
                            return true;
                        }
                    }
                }

                return false;
            }

            return false;
        }

        public static string ReadCode(string json)
        {
            if (TryJsonString(json, "session_code", out var code) && !string.IsNullOrEmpty(code))
                return code;
            TryJsonString(json, "code", out code);
            return code;
        }

        /// <summary>
        /// Identity of one visit. genjustuai PR #9: <c>session.id</c> changes even when the profile repeats.
        /// Falls back to <c>session_code</c> only when id is absent (legacy flat body).
        /// </summary>
        public static string SessionKeyFrom(string json)
        {
            var payload = json;
            if (TryGetSessionPayload(json, out var inner, out var sessionNull))
            {
                if (sessionNull || string.IsNullOrEmpty(inner))
                    return "";
                payload = inner;
            }

            if (TryJsonString(payload, "id", out var sid) && !string.IsNullOrEmpty(sid))
                return sid;
            if (TryJsonString(payload, "session_id", out sid) && !string.IsNullOrEmpty(sid))
                return sid;
            return ReadCode(payload) ?? "";
        }

        public static bool IsClosedSession(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            string status = null;
            if (TryJsonString(json, "status", out status) ||
                TryJsonString(json, "state", out status) ||
                TryJsonString(json, "session_status", out status) ||
                TryJsonString(json, "sessionStatus", out status) ||
                TryJsonString(json, "lifecycle", out status))
            {
                if (IsEndedWord(status))
                    return true;
            }

            if (TryJsonBool(json, "ended", out var ended) && ended)
                return true;
            if (TryJsonBool(json, "encerrado", out var encerrado) && encerrado)
                return true;
            if (TryJsonBool(json, "active", out var active) && !active)
                return true;
            if (TryJsonBool(json, "is_active", out var isActive) && !isActive)
                return true;
            if (TryJsonString(json, "ended_at", out var endedAt) && !string.IsNullOrEmpty(endedAt))
                return true;
            return false;
        }

        public static bool IsEndedWord(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "ended":
                case "end":
                case "encerrado":
                case "encerrada":
                case "closed":
                case "finished":
                case "complete":
                case "completed":
                case "cancelled":
                case "canceled":
                case "inactive":
                case "finalizado":
                case "finalizada":
                case "terminado":
                case "terminada":
                    return true;
                default:
                    return false;
            }
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
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return false;
            var needle = "\"" + key + "\"";
            var i = 0;
            while (i < json.Length)
            {
                var found = json.IndexOf(needle, i, StringComparison.Ordinal);
                if (found < 0)
                    return false;
                if (found > 0 && IsKeyChar(json[found - 1]))
                {
                    i = found + needle.Length;
                    continue;
                }

                var colon = json.IndexOf(':', found + needle.Length);
                if (colon < 0)
                    return false;
                var c = colon + 1;
                while (c < json.Length && char.IsWhiteSpace(json[c]))
                    c++;
                if (c >= json.Length)
                    return false;
                if (json[c] == 'n' && json.IndexOf("null", c, StringComparison.Ordinal) == c)
                    return false;
                if (json[c] != '"')
                    return false;
                c++;
                var end = json.IndexOf('"', c);
                if (end < 0)
                    return false;
                value = json.Substring(c, end - c);
                return true;
            }

            return false;
        }

        public static bool TryJsonBool(string json, string key, out bool value)
        {
            value = false;
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return false;
            var needle = "\"" + key + "\"";
            var i = 0;
            while (i < json.Length)
            {
                var found = json.IndexOf(needle, i, StringComparison.Ordinal);
                if (found < 0)
                    return false;
                if (found > 0 && IsKeyChar(json[found - 1]))
                {
                    i = found + needle.Length;
                    continue;
                }

                var colon = json.IndexOf(':', found + needle.Length);
                if (colon < 0)
                    return false;
                var c = colon + 1;
                while (c < json.Length && char.IsWhiteSpace(json[c]))
                    c++;
                if (c + 4 <= json.Length && string.CompareOrdinal(json, c, "true", 0, 4) == 0)
                {
                    value = true;
                    return true;
                }

                if (c + 5 <= json.Length && string.CompareOrdinal(json, c, "false", 0, 5) == 0)
                {
                    value = false;
                    return true;
                }

                return false;
            }

            return false;
        }

        static bool IsKeyChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
