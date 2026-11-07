using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Posts {tool,args} to /api/gen/commands and returns the JSON object Gemini should hear.
    /// HTTP runs off the main thread.
    /// </summary>
    public sealed class GenToolBridge
    {
        static readonly HttpClient Http = NewClient();
        readonly string baseUrl;
        readonly IVoiceTransport transport;
        readonly Action<string> onHouseJson;

        public GenToolBridge(string baseUrl, IVoiceTransport transport, Action<string> onHouseJson)
        {
            this.baseUrl = string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/');
            this.transport = transport;
            this.onHouseJson = onHouseJson;
        }

        public void Handle(string serverMessage)
        {
            if (string.IsNullOrEmpty(serverMessage) || transport == null)
                return;
            var url = baseUrl + "/api/gen/commands";
            Task.Run(() =>
            {
                string house = null;
                GenToolRoundtrip.Forward(serverMessage, transport, (tool, args) =>
                {
                    var body = GeminiLiveMessages.CommandBody(tool, args);
                    try
                    {
                        var content = new StringContent(body, Encoding.UTF8, "application/json");
                        var response = Http.PostAsync(url, content).GetAwaiter().GetResult();
                        var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(text))
                            return "{\"ok\":false,\"error\":\"command_failed\"}";
                        if (GenJson.TryParse(text, out var root) && root is JObj obj && obj.TryGet("state", out var state) && !(state is JNull))
                            house = text;
                        var notedTool = tool;
                        var notedArgs = args;
                        MainThread.Post(() => GenHistoryClient.Note(baseUrl, "quest", notedTool + " " + notedArgs, "tool"));
                        return text;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Gen command: " + e.GetType().Name);
                        return "{\"ok\":false,\"error\":\"command_failed\"}";
                    }
                });
                if (house != null && onHouseJson != null)
                {
                    var copy = house;
                    MainThread.Post(() => onHouseJson(copy));
                }
            });
        }

        static HttpClient NewClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            return client;
        }
    }
}
