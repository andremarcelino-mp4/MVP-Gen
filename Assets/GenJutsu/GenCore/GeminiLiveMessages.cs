using System;
using System.Collections.Generic;
using System.Text;

namespace GenJutsu.GenCore
{
    public sealed class VoiceSessionTicket
    {
        public string Provider = "";
        public string Model = "";
        public string Token = "";
        public string ExpiresAt = "";
        public string WsUrl = "";
        public string SystemInstructionRaw = "";
        public string ToolsRaw = "";
        public string VoiceName = "";
        public string BaseUrl = "";
        public string SessionId = "";
    }

    public sealed class GeminiToolCall
    {
        public string Id = "";
        public string Name = "";
        public string ArgsJson = "{}";
        public string ResponseJson = "{\"ok\":true}";
    }

    public sealed class GeminiAudioChunk
    {
        public string Base64 = "";
        public int SampleRate = 24000;
    }

    public sealed class GeminiServerEvent
    {
        public bool SetupComplete;
        public bool Interrupted;
        public bool TurnComplete;
        public bool GoAway;
        public string Text = "";
        public readonly List<GeminiAudioChunk> Audio = new List<GeminiAudioChunk>();
        public readonly List<GeminiToolCall> Tools = new List<GeminiToolCall>();
    }

    /// <summary>
    /// Gemini Live BidiGenerateContent JSON, matching the current WebSocket reference:
    /// https://ai.google.dev/api/live
    /// One top-level field per client message: setup, realtimeInput, or toolResponse.
    /// </summary>
    public static class GeminiLiveMessages
    {
        public const string DefaultWs =
            "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent";

        public static bool TryParseTicket(string json, out VoiceSessionTicket ticket)
        {
            ticket = null;
            if (!GenJson.TryParse(json, out var root) || !(root is JObj obj))
                return false;
            ticket = new VoiceSessionTicket();
            ticket.Provider = obj.Str("provider") ?? "";
            ticket.Model = obj.Str("model") ?? "";
            ticket.Token = obj.Str("token") ?? "";
            ticket.ExpiresAt = obj.Str("expiresAt") ?? obj.Str("expires_at") ?? "";
            ticket.WsUrl = obj.Str("wsUrl") ?? obj.Str("ws_url") ?? "";
            ticket.VoiceName = obj.Str("voiceName") ?? obj.Str("voice_name") ?? "";
            if (obj.TryGet("systemInstruction", out var si) || obj.TryGet("system_instruction", out si))
                ticket.SystemInstructionRaw = si.Raw ?? "";
            if (obj.TryGet("tools", out var tools))
                ticket.ToolsRaw = tools.Raw ?? "";
            return true;
        }

        public static string ResolveWs(string wsUrl, string token)
        {
            if (!string.IsNullOrEmpty(token) && token.StartsWith("wss://", StringComparison.Ordinal))
                return token;
            if (string.IsNullOrEmpty(wsUrl))
                wsUrl = DefaultWs;
            if (string.IsNullOrEmpty(token) || wsUrl.IndexOf("access_token=", StringComparison.Ordinal) >= 0)
                return wsUrl;
            var join = wsUrl.IndexOf('?') >= 0 ? "&" : "?";
            return wsUrl + join + "access_token=" + Uri.EscapeDataString(token);
        }

        public static string BuildSetup(string model, string systemInstructionRaw, string toolsRaw)
        {
            var modelName = string.IsNullOrEmpty(model) ? "models/gemini-2.5-flash-native-audio-preview-12-2025" : model;
            if (modelName.IndexOf("/", StringComparison.Ordinal) < 0)
                modelName = "models/" + modelName;
            var sb = new StringBuilder(256);
            sb.Append("{\"setup\":{\"model\":\"").Append(GenJson.Escape(modelName)).Append("\"");
            // voiceName is locked into the ephemeral token (Charon). Sending it again is unnecessary.
            sb.Append(",\"generationConfig\":{\"responseModalities\":[\"AUDIO\"],\"speechConfig\":{\"languageCode\":\"pt-BR\"}}");
            var instruction = EmbedInstruction(systemInstructionRaw);
            if (instruction != null)
                sb.Append(",\"systemInstruction\":").Append(instruction);
            var tools = EmbedTools(toolsRaw);
            if (tools != null)
                sb.Append(",\"tools\":").Append(tools);
            sb.Append("}}");
            return sb.ToString();
        }

        public static string AudioChunk(byte[] pcm16, int sampleRate)
        {
            var rate = sampleRate > 0 ? sampleRate : 16000;
            var data = pcm16 == null || pcm16.Length == 0 ? "" : Convert.ToBase64String(pcm16);
            return "{\"realtimeInput\":{\"audio\":{\"mimeType\":\"audio/pcm;rate=" + rate + "\",\"data\":\"" + data + "\"}}}";
        }

        public static string AudioStreamEnd()
        {
            return "{\"realtimeInput\":{\"audioStreamEnd\":true}}";
        }

        public static string TextTurn(string text)
        {
            return "{\"realtimeInput\":{\"text\":\"" + GenJson.Escape(text ?? "") + "\"}}";
        }

        public static string BuildToolResponse(IList<GeminiToolCall> calls)
        {
            var sb = new StringBuilder();
            sb.Append("{\"toolResponse\":{\"functionResponses\":[");
            for (var i = 0; i < calls.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                var c = calls[i];
                sb.Append("{\"id\":\"").Append(GenJson.Escape(c.Id ?? "")).Append("\"");
                sb.Append(",\"name\":\"").Append(GenJson.Escape(c.Name ?? "")).Append("\"");
                sb.Append(",\"response\":").Append(GenJson.AsObject(c.ResponseJson));
                sb.Append('}');
            }

            sb.Append("]}}");
            return sb.ToString();
        }

        public static string CommandBody(string tool, string argsJson)
        {
            if (string.IsNullOrWhiteSpace(argsJson))
                argsJson = "{}";
            return "{\"tool\":\"" + GenJson.Escape(tool ?? "") + "\",\"args\":" + argsJson + "}";
        }

        /// <summary>
        /// Gemini sometimes delivers JSON, including <c>setupComplete</c>, as a binary WebSocket frame.
        /// Those bytes are UTF-8. A BOM or trailing NULs must not hide the object.
        /// </summary>
        public static string DecodeServerFrame(byte[] buffer, int count)
        {
            if (buffer == null || count <= 0)
                return "";
            var start = 0;
            var end = count > buffer.Length ? buffer.Length : count;
            if (end >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                start = 3;
            while (start < end && (buffer[start] == 0 || IsFrameSpace(buffer[start])))
                start++;
            while (end > start && (buffer[end - 1] == 0 || IsFrameSpace(buffer[end - 1])))
                end--;
            if (end <= start)
                return "";
            return Encoding.UTF8.GetString(buffer, start, end - start);
        }

        static bool IsFrameSpace(byte b)
        {
            return b == (byte)' ' || b == (byte)'\n' || b == (byte)'\r' || b == (byte)'\t';
        }

        public static bool TryReadServer(string json, out GeminiServerEvent ev)
        {
            ev = null;
            if (!GenJson.TryParse(json, out var root) || !(root is JObj obj))
                return false;
            ev = new GeminiServerEvent();
            if (obj.TryGet("setupComplete", out _))
                ev.SetupComplete = true;
            if (obj.TryGet("goAway", out _))
                ev.GoAway = true;
            var content = obj.Obj("serverContent");
            if (content != null)
            {
                if (content.TryGet("interrupted", out var interrupted) && interrupted is JBool ib && ib.Value)
                    ev.Interrupted = true;
                if (content.TryGet("turnComplete", out var done) && done is JBool db && db.Value)
                    ev.TurnComplete = true;
                var text = new StringBuilder();
                AppendText(content.Obj("outputTranscription"), text);
                AppendText(content.Obj("inputTranscription"), text);
                var turn = content.Obj("modelTurn");
                if (turn != null && turn.TryGet("parts", out var parts) && parts is JArr arr)
                {
                    for (var i = 0; i < arr.Items.Count; i++)
                    {
                        var part = arr.Items[i] as JObj;
                        if (part == null)
                            continue;
                        var said = part.Str("text");
                        if (!string.IsNullOrEmpty(said))
                        {
                            if (text.Length > 0)
                                text.Append(' ');
                            text.Append(said);
                        }

                        var inline = part.Obj("inlineData") ?? part.Obj("inline_data");
                        if (inline == null)
                            continue;
                        var data = inline.Str("data");
                        if (string.IsNullOrEmpty(data))
                            continue;
                        var mime = inline.Str("mimeType") ?? inline.Str("mime_type") ?? "";
                        ev.Audio.Add(new GeminiAudioChunk
                        {
                            Base64 = data,
                            SampleRate = RateFromMime(mime)
                        });
                    }
                }

                ev.Text = text.ToString();
            }

            if (obj.TryGet("toolCall", out var tool) && tool is JObj toolObj &&
                toolObj.TryGet("functionCalls", out var calls) && calls is JArr callArr)
            {
                for (var i = 0; i < callArr.Items.Count; i++)
                {
                    var call = callArr.Items[i] as JObj;
                    if (call == null)
                        continue;
                    var args = "{}";
                    if (call.TryGet("args", out var argsVal) && !string.IsNullOrEmpty(argsVal.Raw))
                        args = argsVal.Raw;
                    ev.Tools.Add(new GeminiToolCall
                    {
                        Id = call.Str("id") ?? "",
                        Name = call.Str("name") ?? "",
                        ArgsJson = args
                    });
                }
            }

            return ev.SetupComplete || ev.Interrupted || ev.TurnComplete || ev.GoAway || ev.Audio.Count > 0
                   || ev.Tools.Count > 0 || !string.IsNullOrEmpty(ev.Text);
        }

        public static int RateFromMime(string mime)
        {
            if (string.IsNullOrEmpty(mime))
                return 24000;
            var key = "rate=";
            var i = mime.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                return 24000;
            i += key.Length;
            var end = i;
            while (end < mime.Length && char.IsDigit(mime[end]))
                end++;
            if (end == i)
                return 24000;
            return int.TryParse(mime.Substring(i, end - i), out var rate) && rate > 0 ? rate : 24000;
        }

        static void AppendText(JObj node, StringBuilder text)
        {
            if (node == null)
                return;
            var said = node.Str("text");
            if (string.IsNullOrEmpty(said))
                return;
            if (text.Length > 0)
                text.Append(' ');
            text.Append(said);
        }

        static string EmbedInstruction(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var t = raw.Trim();
            if (t.StartsWith("{", StringComparison.Ordinal))
                return t;
            if (t.StartsWith("\"", StringComparison.Ordinal))
                return "{\"parts\":[{\"text\":" + t + "}]}";
            return "{\"parts\":[{\"text\":\"" + GenJson.Escape(t) + "\"}]}";
        }

        static string EmbedTools(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == "null")
                return null;
            var t = GenJson.OmitKeys(raw.Trim(), "additionalProperties");
            if (string.IsNullOrWhiteSpace(t))
                return null;
            if (t.IndexOf("\"functionDeclarations\"", StringComparison.Ordinal) >= 0)
                return t.StartsWith("[", StringComparison.Ordinal) ? t : "[" + t + "]";
            if (t.StartsWith("[", StringComparison.Ordinal))
                return "[{\"functionDeclarations\":" + t + "}]";
            if (t.StartsWith("{", StringComparison.Ordinal))
                return "[{\"functionDeclarations\":[" + t + "]}]";
            return null;
        }

        /// <summary>Removes access_token values so a socket error can be shown without leaking the ephemeral token.</summary>
        public static string Scrub(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "";
            var i = message.IndexOf("access_token=", StringComparison.Ordinal);
            if (i < 0)
                return message;
            var end = i + "access_token=".Length;
            while (end < message.Length && message[end] != '&' && message[end] != '"' && message[end] != ' ')
                end++;
            return message.Substring(0, i) + "access_token=…" + message.Substring(end);
        }
    }

    public interface IVoiceTransport
    {
        bool IsLive { get; }
        void SendRaw(string json);
        void Close();
    }

    /// <summary>
    /// Forwards a Gemini toolCall to the command client and writes one toolResponse back to the transport.
    /// </summary>
    public static class GenToolRoundtrip
    {
        public static int Forward(string serverMessage, IVoiceTransport transport, Func<string, string, string> postCommand)
        {
            if (transport == null || postCommand == null)
                return 0;
            if (!GeminiLiveMessages.TryReadServer(serverMessage, out var ev) || ev.Tools.Count == 0)
                return 0;
            var replies = new List<GeminiToolCall>(ev.Tools.Count);
            for (var i = 0; i < ev.Tools.Count; i++)
            {
                var call = ev.Tools[i];
                string response;
                try
                {
                    response = postCommand(call.Name, call.ArgsJson);
                }
                catch (Exception)
                {
                    response = "{\"ok\":false,\"error\":\"command_failed\"}";
                }

                replies.Add(new GeminiToolCall
                {
                    Id = call.Id,
                    Name = call.Name,
                    ResponseJson = response
                });
            }

            transport.SendRaw(GeminiLiveMessages.BuildToolResponse(replies));
            return replies.Count;
        }
    }
}
