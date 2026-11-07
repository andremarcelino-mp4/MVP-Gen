using System.Collections.Generic;
using GenJutsu.GenCore;
using GenJutsu.ProfileDemo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Push-to-talk for Gen. Hold the right controller B button (secondary). That is not the old
    /// jump button (primary A/X, already unbound) and not trigger/grip, which pick up props.
    /// Editor: hold V. Networking stays off the main thread; this loop only pumps the microphone.
    /// </summary>
    public sealed class GenVoiceController : MonoBehaviour
    {
        public const string PushToTalkButton =
            "Right controller secondary button (B). Editor hold: V. Not primary A/X, not trigger, not grip.";

        public string baseUrl = "https://genjutsuai.vercel.app";
        public SessionGate gate;
        public ProfileDemoDirector director;

        public GenVoicePhase Phase { get; private set; } = GenVoicePhase.Idle;
        public event System.Action<string> HouseJson;

        GenStatusView view;
        QuestMicrophoneCapture mic;
        StreamingAudioPlayer player;
        AndroidCaptionSpeaker speaker;
        GeminiLiveTransport gemini;
        GroqFallbackTransport fallback;
        GenToolBridge tools;
        bool ptt;
        bool connecting;
        string reply = "";
        readonly byte[] chunk = new byte[8000];
        readonly List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

        public void Configure(SessionGate sessionGate, ProfileDemoDirector demo, string url)
        {
            gate = sessionGate;
            director = demo;
            if (!string.IsNullOrEmpty(url))
                baseUrl = url;
        }

        void Awake()
        {
            view = GetComponent<GenStatusView>();
            if (view == null)
            {
                var ui = new GameObject("GenStatus");
                ui.transform.SetParent(transform, false);
                view = ui.AddComponent<GenStatusView>();
            }

            view.EnsureVisual();
            mic = GetComponent<QuestMicrophoneCapture>() ?? gameObject.AddComponent<QuestMicrophoneCapture>();
            player = GetComponent<StreamingAudioPlayer>() ?? gameObject.AddComponent<StreamingAudioPlayer>();
            speaker = GetComponent<AndroidCaptionSpeaker>() ?? gameObject.AddComponent<AndroidCaptionSpeaker>();
            gemini = new GeminiLiveTransport();
            fallback = new GroqFallbackTransport();
            tools = new GenToolBridge(baseUrl, gemini, json =>
            {
                if (HouseJson != null)
                    HouseJson(json);
            });
            Show();
        }

        void OnDestroy()
        {
            if (gemini != null)
                gemini.Close();
            if (mic != null)
                mic.End();
        }

        void Update()
        {
            var down = ReadPtt();
            if (down && !ptt)
                Press();
            else if (!down && ptt)
                Release();
            ptt = down;
            if (Phase == GenVoicePhase.Listening)
                Pump();
            else if (Phase == GenVoicePhase.Speaking && player != null && !player.IsAudible && !ptt)
            {
                Phase = GenVoicePhases.AfterTurnComplete(Phase, false);
                Show();
            }
        }

        void Press()
        {
            if (GenVoicePhases.IsInterruption(Phase) && player != null)
                player.Clear();
            Phase = GenVoicePhases.AfterPttPressed(Phase);
            reply = "";
            Show();
            if (NetworkSuppressed())
                return;
            if (mic == null || !mic.Begin())
            {
                Phase = GenVoicePhases.AfterFailure();
                reply = "Microfone indisponível.";
                Show();
                return;
            }

            if (gemini == null || !gemini.IsReady)
                Connect();
        }

        void Release()
        {
            if (mic != null && mic.IsCapturing)
                mic.PullPcm16(chunk);
            var wav = mic != null ? mic.TakeWav() : null;
            if (mic != null)
                mic.End();
            Phase = GenVoicePhases.AfterPttReleased(Phase);
            Show();
            if (NetworkSuppressed())
            {
                Phase = GenVoicePhase.Idle;
                Show();
                return;
            }

            if (gemini != null && gemini.IsReady)
            {
                gemini.EndUtterance();
                return;
            }

            fallback.Submit(baseUrl, SessionId(), wav, OnChat, OnFailed);
        }

        void Pump()
        {
            if (mic == null || !mic.IsCapturing || gemini == null)
                return;
            var n = mic.PullPcm16(chunk);
            if (n > 0)
                gemini.SendAudio(chunk, n);
        }

        void Connect()
        {
            if (connecting || NetworkSuppressed())
                return;
            connecting = true;
            GenVoiceSessionClient.Request(baseUrl, SessionId(), OnTicket, OnFailed);
        }

        void OnTicket(VoiceSessionTicket ticket)
        {
            connecting = false;
            if (gemini == null)
                return;
            gemini.Connect(ticket, HandleSocketMessage, OnFailed);
        }

        void OnGemini(GeminiServerEvent ev)
        {
            if (ev == null)
                return;
            if (ev.Interrupted && player != null)
            {
                player.Clear();
                Phase = GenVoicePhases.AfterServerInterrupted(Phase, ptt);
            }

            if (ev.Audio != null && player != null)
            {
                for (var i = 0; i < ev.Audio.Count; i++)
                {
                    player.EnqueueBase64(ev.Audio[i].Base64, ev.Audio[i].SampleRate);
                    Phase = GenVoicePhases.AfterModelAudio(Phase);
                }
            }

            if (!string.IsNullOrEmpty(ev.Text))
                reply = ev.Text;
            if (ev.TurnComplete && (player == null || !player.IsAudible))
                Phase = GenVoicePhases.AfterTurnComplete(Phase, ptt);
            if (ev.GoAway && gemini != null)
                gemini.Close();
            Show();
        }

        public void HandleSocketMessage(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;
            if (GeminiLiveMessages.TryReadServer(json, out var ev))
            {
                if (ev.Tools.Count > 0 && tools != null)
                    tools.Handle(json);
                OnGemini(ev);
                return;
            }

            if (ChatReplyParser.TryParse(json, out var chat))
                OnChat(chat);
        }

        void OnChat(ChatReply chat)
        {
            connecting = false;
            if (chat == null)
                return;
            reply = string.IsNullOrEmpty(chat.ReplyText) ? chat.Transcript : chat.ReplyText;
            if (speaker != null && !string.IsNullOrEmpty(chat.ReplyText))
                speaker.Speak(chat.ReplyText);
            if (!string.IsNullOrEmpty(chat.StateRaw))
            {
                var wrapped = "{\"state\":" + chat.StateRaw + "}";
                if (HouseJson != null)
                    HouseJson(wrapped);
            }

            if (director != null && chat.Actions != null)
            {
                for (var i = 0; i < chat.Actions.Length; i++)
                {
                    var action = chat.Actions[i];
                    if (action == null || action.Tool != "switch_dojo")
                        continue;
                    if (Profiles.TryParse(action.Dojo, false, out var id))
                        director.SwitchVisualDojo(id);
                }
            }

            Phase = GenVoicePhase.Idle;
            Show();
        }

        void OnFailed(string message)
        {
            connecting = false;
            if (Phase == GenVoicePhase.Listening && mic != null && mic.IsCapturing)
            {
                reply = "A usar o pedido de reserva ao soltar o botão.";
                Show();
                return;
            }

            Phase = GenVoicePhases.AfterFailure();
            reply = message ?? "Gen offline.";
            Show();
        }

        bool NetworkSuppressed()
        {
            return gate != null && gate.suppressNetworkPoll;
        }

        string SessionId()
        {
            return gate != null ? gate.ActiveSessionKey ?? "" : "";
        }

        void Show()
        {
            if (view != null)
                view.Show(Phase, reply);
        }

        bool ReadPtt()
        {
            devices.Clear();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i].TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out var pressed) && pressed)
                    return true;
            }

#if UNITY_EDITOR
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.isPressed)
                return true;
#endif
            return false;
        }
    }
}
