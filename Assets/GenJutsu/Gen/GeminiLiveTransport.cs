using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Direct Gemini Live socket. The ephemeral token comes from the backend; this class never sees an API key.
    /// Send and receive run off the main thread. Events are posted back through <see cref="MainThread"/>.
    /// </summary>
    public sealed class GeminiLiveTransport : IVoiceTransport
    {
        readonly ConcurrentQueue<string> outgoing = new ConcurrentQueue<string>();
        ClientWebSocket socket;
        CancellationTokenSource cts;
        public bool IsLive { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsConnecting { get; private set; }

        public void Connect(VoiceSessionTicket ticket, Action<string> onMessage, Action<string> onFailed)
        {
            Close();
            IsConnecting = true;
            cts = new CancellationTokenSource();
            var token = cts.Token;
            Task.Run(() => Run(ticket, onMessage, onFailed, token));
        }

        public void SendAudio(byte[] pcm16, int count)
        {
            if ((cts == null && !IsLive) || pcm16 == null || count <= 0)
                return;
            var copy = new byte[count];
            Buffer.BlockCopy(pcm16, 0, copy, 0, count);
            outgoing.Enqueue(GeminiLiveMessages.AudioChunk(copy, 16000));
        }

        public void EndUtterance()
        {
            if (IsLive)
                outgoing.Enqueue(GeminiLiveMessages.AudioStreamEnd());
        }

        public void SendRaw(string json)
        {
            if (IsLive && !string.IsNullOrEmpty(json))
                outgoing.Enqueue(json);
        }

        public void Close()
        {
            IsLive = false;
            IsReady = false;
            IsConnecting = false;
            var local = cts;
            cts = null;
            if (local != null)
            {
                try { local.Cancel(); }
                catch (Exception) { /* already closed */ }
                local.Dispose();
            }

            var ws = socket;
            socket = null;
            if (ws != null)
            {
                try
                {
                    if (ws.State == WebSocketState.Open)
                        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
                catch (Exception) { /* socket already dead */ }
                ws.Dispose();
            }

            while (outgoing.TryDequeue(out _)) { }
        }

        async Task Run(VoiceSessionTicket ticket, Action<string> onMessage, Action<string> onFailed, CancellationToken token)
        {
            ClientWebSocket ws = null;
            try
            {
                ws = new ClientWebSocket();
                socket = ws;
                var url = GeminiLiveMessages.ResolveWs(ticket != null ? ticket.WsUrl : null, ticket != null ? ticket.Token : null);
                GenVoiceLog.Info("socket connect " + GeminiLiveMessages.Scrub(url) + " model=" + (ticket != null ? ticket.Model : ""));
                await ws.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
                IsLive = true;
                GenVoiceLog.Info("socket open");
                var setup = GeminiLiveMessages.BuildSetup(
                    ticket != null ? ticket.Model : null,
                    ticket != null ? ticket.SystemInstructionRaw : null,
                    ticket != null ? ticket.ToolsRaw : null);
                GenVoiceLog.Info("setup bytes=" + setup.Length + " voiceInSetup=" + (setup.IndexOf("voiceName", StringComparison.Ordinal) >= 0));
                await SendOne(ws, setup, token).ConfigureAwait(false);
                var pump = PumpSend(ws, token);
                await PumpRecv(ws, onMessage, token).ConfigureAwait(false);
                try { await pump.ConfigureAwait(false); }
                catch (OperationCanceledException) { /* shutdown */ }
            }
            catch (OperationCanceledException)
            {
                /* Close() */
            }
            catch (Exception e)
            {
                var why = Describe(e);
                GenVoiceLog.Warn("socket failed: " + why);
                IsConnecting = false;
                MainThread.Post(() =>
                {
                    if (onFailed != null)
                        onFailed(why);
                });
            }
            finally
            {
                IsLive = false;
                IsReady = false;
                IsConnecting = false;
                if (ws != null)
                    ws.Dispose();
            }
        }

        static string Head(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var t = text.Length > 160 ? text.Substring(0, 160) : text;
            return GeminiLiveMessages.Scrub(t).Replace("\n", " ").Replace("\r", " ");
        }

        static string Describe(Exception e)
        {
            var why = e.GetType().Name + ": " + e.Message;
            var inner = e.InnerException;
            if (inner != null && !string.IsNullOrEmpty(inner.Message))
                why += " | " + inner.GetType().Name + ": " + inner.Message;
            why = GeminiLiveMessages.Scrub(why);
            return string.IsNullOrWhiteSpace(why) ? e.GetType().Name : why;
        }

        async Task PumpSend(ClientWebSocket ws, CancellationToken token)
        {
            while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                if (!outgoing.TryDequeue(out var json))
                {
                    await Task.Delay(15, token).ConfigureAwait(false);
                    continue;
                }

                await SendOne(ws, json, token).ConfigureAwait(false);
            }
        }

        static async Task SendOne(ClientWebSocket ws, string json, CancellationToken token)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
        }

        async Task PumpRecv(ClientWebSocket ws, Action<string> onMessage, CancellationToken token)
        {
            var buffer = new byte[16384];
            var msg = new MemoryStream();
            while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var why = ws.CloseStatusDescription;
                    if (string.IsNullOrWhiteSpace(why))
                        why = result.CloseStatus.ToString();
                    GenVoiceLog.Warn("socket close: " + GeminiLiveMessages.Scrub(why));
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(why) ? "Gemini fechou o socket." : why);
                }

                // Text and Binary are both UTF-8 JSON. setupComplete often arrives as Binary.
                if (result.MessageType != WebSocketMessageType.Text && result.MessageType != WebSocketMessageType.Binary)
                {
                    GenVoiceLog.Warn("frame ignored type=" + result.MessageType);
                    continue;
                }

                msg.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                    continue;
                var raw = msg.GetBuffer();
                var count = (int)msg.Length;
                var text = GeminiLiveMessages.DecodeServerFrame(raw, count);
                var kind = result.MessageType.ToString();
                msg.SetLength(0);
                var setup = GeminiLiveMessages.TryReadServer(text, out var ev) && ev.SetupComplete;
                if (!setup && text.IndexOf("\"setupComplete\"", StringComparison.Ordinal) >= 0)
                    setup = true;
                if (count <= 400 || setup)
                    GenVoiceLog.Info("frame " + kind + " bytes=" + count + " " + Head(text));
                if (setup)
                {
                    IsReady = true;
                    IsConnecting = false;
                    GenVoiceLog.Info("setupComplete");
                }
                var delivered = text;
                MainThread.Post(() =>
                {
                    if (onMessage != null)
                        onMessage(delivered);
                });
            }
        }
    }
}
