using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// TLS GET /api/session?code=… — no secrets, no invented profile on failure.
    /// </summary>
    public class SessionApiClient : MonoBehaviour
    {
        public string baseUrl = "https://genjutsuai.vercel.app";
        public float timeoutSeconds = 8f;

        public struct Result
        {
            public bool ok;
            public ProfileId profile;
            public string rawProfile;
            public string error;
            public long httpStatus;
        }

        public IEnumerator Fetch(string code, Action<Result> done)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                done?.Invoke(new Result { ok = false, error = "Digite o código da sessão." });
                yield break;
            }

            var trimmed = code.Trim();
            var url = baseUrl.TrimEnd('/') + "/api/session?code=" + UnityWebRequest.EscapeURL(trimmed);
            using var req = UnityWebRequest.Get(url);
            req.timeout = Mathf.CeilToInt(timeoutSeconds);
            var op = req.SendWebRequest();
            while (!op.isDone)
                yield return null;

            if (req.result != UnityWebRequest.Result.Success)
            {
                var down = req.result == UnityWebRequest.Result.ConnectionError ||
                           req.result == UnityWebRequest.Result.DataProcessingError ||
                           req.responseCode == 0;
                done?.Invoke(new Result
                {
                    ok = false,
                    httpStatus = req.responseCode,
                    error = down
                        ? "Servidor indisponível. Permaneça no espaço branco."
                        : "Código inválido ou sessão não encontrada."
                });
                yield break;
            }

            var body = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (!TryExtractProfileId(body, out var raw) ||
                !Profiles.TryParse(raw, allowEditorFallback: false, out var id))
            {
                done?.Invoke(new Result
                {
                    ok = false,
                    httpStatus = req.responseCode,
                    rawProfile = raw,
                    error = "Resposta sem profile_id válido. Permaneça no branco."
                });
                yield break;
            }

            done?.Invoke(new Result { ok = true, profile = id, rawProfile = raw, httpStatus = req.responseCode });
        }

        public static bool TryExtractProfileId(string json, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(json))
                return false;
            if (TryJsonString(json, "profile_id", out value) || TryJsonString(json, "profileId", out value))
                return !string.IsNullOrEmpty(value);
            return false;
        }

        static bool TryJsonString(string json, string key, out string value)
        {
            value = null;
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
            if (i >= json.Length || json[i] != '"')
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
