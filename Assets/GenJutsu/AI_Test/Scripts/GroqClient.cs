using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.AITest
{
    public class GroqClient : MonoBehaviour
    {
        public const string ChatUrl = "https://api.groq.com/openai/v1/chat/completions";
        public const string TranscriptionUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
        public const string SpeechUrl = "https://api.groq.com/openai/v1/audio/speech";
        public const string ModelsUrl = "https://api.groq.com/openai/v1/models";

        public IEnumerator CheckConnection(string apiKey, Action<GroqCallResult> done)
        {
            yield return Send(ModelsUrl, "GET", null, apiKey, 20, "conexão", request =>
            {
                var result = ReadStatus(request, "conexão");
                result.Success = result.StatusCode >= 200 && result.StatusCode < 300;
                if (result.Success)
                    result.Text = "ok";
                done?.Invoke(result);
            });
        }

        public IEnumerator CompleteChat(string jsonBody, string apiKey, Action<GroqCallResult> done)
        {
            Debug.Log("[GenJutsu] Requisição enviada para Groq");
            yield return Send(ChatUrl, "POST", jsonBody, apiKey, 45, "chat", request =>
            {
                var result = ReadStatus(request, "chat");
                if (!result.Success)
                {
                    done?.Invoke(result);
                    return;
                }

                GroqChatResponse parsed;
                try
                {
                    parsed = JsonUtility.FromJson<GroqChatResponse>(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[GenJutsu] Etapa chat: JSON inválido. " + exception.Message);
                    done?.Invoke(Fail("Corvo não conseguiu responder.", "chat"));
                    return;
                }

                var text = parsed != null && parsed.choices != null && parsed.choices.Length > 0 && parsed.choices[0].message != null
                    ? parsed.choices[0].message.content
                    : null;
                if (string.IsNullOrWhiteSpace(text))
                {
                    Debug.LogError("[GenJutsu] Etapa chat: resposta vazia.");
                    done?.Invoke(Fail("Corvo não conseguiu responder.", "chat"));
                    return;
                }

                result.Success = true;
                result.Text = text.Trim();
                result.TotalTokens = parsed.usage != null ? parsed.usage.total_tokens : 0;
                Debug.Log("[GenJutsu] Resposta recebida");
                Debug.Log("[GenJutsu] Tokens/resposta processados: " + result.TotalTokens);
                done?.Invoke(result);
            });
        }

        public IEnumerator Transcribe(byte[] wav, string model, string language, string apiKey, Action<GroqCallResult> done)
        {
            Debug.Log("[GenJutsu] Requisição enviada para Groq");
            var form = new WWWForm();
            form.AddField("model", model);
            form.AddField("language", language);
            form.AddField("response_format", "json");
            form.AddField("temperature", "0");
            form.AddBinaryData("file", wav, "fala.wav", "audio/wav");

            using (var request = UnityWebRequest.Post(TranscriptionUrl, form))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                request.timeout = 60;
                yield return request.SendWebRequest();
                var result = ReadStatus(request, "Whisper");
                if (!result.Success)
                {
                    done?.Invoke(result);
                    yield break;
                }

                GroqTranscriptionResponse parsed;
                try
                {
                    parsed = JsonUtility.FromJson<GroqTranscriptionResponse>(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[GenJutsu] Etapa Whisper: JSON inválido. " + exception.Message);
                    done?.Invoke(Fail("Erro: não foi possível transcrever a fala.", "Whisper"));
                    yield break;
                }

                result.Success = true;
                result.Text = parsed != null && parsed.text != null ? parsed.text.Trim() : "";
                Debug.Log("[GenJutsu] Resposta recebida");
                done?.Invoke(result);
            }
        }

        public IEnumerator Synthesize(string text, string model, string voice, string apiKey, Action<GroqCallResult> done)
        {
            var body = "{\"model\":\"" + Escape(model) + "\",\"voice\":\"" + Escape(voice) + "\",\"input\":\"" + Escape(text) + "\",\"response_format\":\"wav\"}";
            Debug.Log("[GenJutsu] Requisição enviada para Groq");
            using (var request = new UnityWebRequest(SpeechUrl, "POST"))
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                request.timeout = 60;
                yield return request.SendWebRequest();
                var result = ReadStatus(request, "voz Groq");
                if (!result.Success)
                {
                    done?.Invoke(result);
                    yield break;
                }

                result.Success = true;
                result.AudioWav = request.downloadHandler.data;
                Debug.Log("[GenJutsu] Resposta recebida");
                done?.Invoke(result);
            }
        }

        public static string BuildChatBody(AIConfig config, System.Collections.Generic.List<ChatTurn> history)
        {
            var builder = new StringBuilder();
            builder.Append("{\"model\":\"").Append(Escape(config.model)).Append("\",\"temperature\":");
            builder.Append(config.temperature.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(",\"max_tokens\":").Append(config.maxTokens).Append(",\"messages\":[");
            builder.Append("{\"role\":\"system\",\"content\":\"").Append(Escape(config.systemPrompt)).Append("\"}");
            for (var i = 0; i < history.Count; i++)
            {
                builder.Append(",{\"role\":\"").Append(history[i].Role).Append("\",\"content\":\"")
                    .Append(Escape(history[i].Content)).Append("\"}");
            }
            builder.Append("]}");
            return builder.ToString();
        }

        static IEnumerator Send(string url, string method, string json, string apiKey, int timeout, string stage, Action<UnityWebRequest> done)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                request.timeout = timeout;
                yield return request.SendWebRequest();
                done?.Invoke(request);
            }
        }

        static GroqCallResult ReadStatus(UnityWebRequest request, string stage)
        {
            var result = new GroqCallResult { Stage = stage, StatusCode = request.responseCode };
            if (request.result == UnityWebRequest.Result.Success)
            {
                result.Success = true;
                return result;
            }

            if (request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogError("[GenJutsu] Etapa " + stage + ": tempo esgotado.");
                result.ErrorMessage = "Erro: tempo esgotado.";
                return result;
            }

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("[GenJutsu] Etapa " + stage + ": sem conexão. " + request.error);
                result.ErrorMessage = "Erro: não foi possível conectar à Groq.";
                return result;
            }

            var body = request.downloadHandler != null ? request.downloadHandler.text : "";
            Debug.LogError("[GenJutsu] Etapa " + stage + ": HTTP " + request.responseCode + " " + body);
            result.ErrorMessage = MessageForStatus(request.responseCode);
            return result;
        }

        public static string MessageForStatus(long status)
        {
            if (status == 400) return "Erro: a Groq recusou o pedido.";
            if (status == 401) return "Erro: API Key inválida.";
            if (status == 403) return "Erro: acesso negado à Groq.";
            if (status == 429) return "Erro: limite de requisições atingido.";
            if (status >= 500) return "Erro: a Groq falhou.";
            return "Erro: não foi possível conectar à Groq.";
        }

        static GroqCallResult Fail(string message, string stage)
        {
            return new GroqCallResult { Success = false, ErrorMessage = message, Stage = stage };
        }

        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }
    }

    public class ChatTurn
    {
        public string Role;
        public string Content;
    }
}
