using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.AITest
{
    public class GenJutsuAI : MonoBehaviour
    {
        public const string TestPrompt = "Olá Corvo. Faça um teste de funcionamento do GenJutsu.";

        public AIConfig config;
        public GroqClient client;
        public PiperVoiceSpeaker piper;
        public GroqSpeechSpeaker groqVoice;
        public HouseMqttClient mqtt;

        public event Action<string, string> MessageAdded;
        public event Action HistoryCleared;
        public event Action<string> StatusTextChanged;

        readonly List<ChatTurn> history = new List<ChatTurn>();
        string apiKey;
        bool busy;
        string connectionStatus = "Inicializando...";

        public bool IsBusy => busy;
        public string SelectedVoice => config != null ? config.selectedVoice : PiperVoiceSpeaker.VoiceId;

        void Awake()
        {
            SilenceSimulator();
            if (config == null)
                config = AIConfig.LoadOrCreate();
            if (client == null)
                client = GetComponent<GroqClient>();
            if (piper == null)
                piper = GetComponent<PiperVoiceSpeaker>();
            if (groqVoice == null)
                groqVoice = GetComponent<GroqSpeechSpeaker>();
            if (mqtt == null)
                mqtt = GetComponent<HouseMqttClient>();
        }

        IEnumerator Start()
        {
            Debug.Log("[GenJutsu] Inicializando IA...");
            SetConnection("Inicializando...");
            yield return piper.Prepare();
            apiKey = config.ResolveApiKey();
            mqtt.Connect(config.mqttHost, config.mqttPort);
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[GenJutsu] Etapa configuração: API Key não configurada.");
                FailStatus("Erro: API Key não configurada.");
                yield break;
            }

            SetConnection("Conectando...");
            GroqCallResult result = null;
            yield return client.CheckConnection(apiKey, value => result = value);
            if (result != null && result.Success)
                SetConnection("Conectado");
            else
                FailStatus(result != null ? result.ErrorMessage : "Erro: não foi possível conectar à Groq.");
        }

        float nextStatusRefresh;
        bool simulatorSilenced;

        void Update()
        {
            if (!simulatorSilenced)
                simulatorSilenced = SilenceSimulator();
            if (busy || Time.unscaledTime < nextStatusRefresh)
                return;
            nextStatusRefresh = Time.unscaledTime + 0.5f;
            PublishStatus();
        }

        public void SetVoice(string voiceId)
        {
            config.selectedVoice = voiceId;
            PublishStatus();
            if (voiceId != PiperVoiceSpeaker.VoiceId)
                Debug.Log("[GenJutsu] Voz Groq selecionada. A pronúncia é em inglês.");
        }

        public void SendTest()
        {
            SubmitText(TestPrompt);
        }

        public void SubmitText(string text)
        {
            if (busy)
                return;
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (string.IsNullOrEmpty(apiKey))
            {
                FailStatus("Erro: API Key não configurada.");
                return;
            }

            var clean = text.Trim();
            Debug.Log("[GenJutsu] Mensagem enviada");
            AddTurn("user", clean);
            MessageAdded?.Invoke("VOCÊ", clean);
            mqtt.Publish(config.mqttUserTopic, clean);
            StartCoroutine(Ask(clean));
        }

        public void SubmitAudio(byte[] wav)
        {
            if (busy || wav == null)
                return;
            if (string.IsNullOrEmpty(apiKey))
            {
                FailStatus("Erro: API Key não configurada.");
                return;
            }
            StartCoroutine(TranscribeThenAsk(wav));
        }

        public void ClearConversation()
        {
            if (busy)
                return;
            history.Clear();
            HistoryCleared?.Invoke();
            Debug.Log("[GenJutsu] Conversa atualizada");
            PublishStatus();
        }

        IEnumerator TranscribeThenAsk(byte[] wav)
        {
            busy = true;
            PublishStatus("Transcrevendo...");
            GroqCallResult result = null;
            yield return client.Transcribe(wav, config.whisperModel, config.whisperLanguage, apiKey, value => result = value);
            busy = false;
            if (result == null || !result.Success)
            {
                FailStatus(result != null ? result.ErrorMessage : "Erro: não foi possível conectar à Groq.");
                yield break;
            }
            if (string.IsNullOrWhiteSpace(result.Text))
            {
                Debug.Log("[GenJutsu] Etapa Whisper: texto vazio, nada enviado.");
                PublishStatus();
                yield break;
            }
            SubmitText(result.Text);
        }

        IEnumerator Ask(string text)
        {
            busy = true;
            PublishStatus("Corvo está pensando...");
            TrimHistory();
            var body = GroqClient.BuildChatBody(config, history);
            GroqCallResult result = null;
            yield return client.CompleteChat(body, apiKey, value => result = value);
            if (result == null || !result.Success)
            {
                busy = false;
                FailStatus(result != null ? result.ErrorMessage : "Corvo não conseguiu responder.");
                yield break;
            }

            AddTurn("assistant", result.Text);
            MessageAdded?.Invoke("CORVO", result.Text);
            mqtt.Publish(config.mqttAssistantTopic, result.Text);
            Debug.Log("[GenJutsu] Conversa atualizada");
            PublishStatus("Falando...");
            string voiceError = null;
            if (config.selectedVoice == PiperVoiceSpeaker.VoiceId)
                yield return piper.Speak(result.Text, value => voiceError = value);
            else
                yield return groqVoice.Speak(result.Text, config.selectedVoice, apiKey, config, value => voiceError = value);
            busy = false;
            if (!string.IsNullOrEmpty(voiceError))
                Debug.LogWarning("[GenJutsu] Etapa voz: " + voiceError);
            SetConnection("Conectado");
        }

        void AddTurn(string role, string content)
        {
            history.Add(new ChatTurn { Role = role, Content = content });
        }

        void TrimHistory()
        {
            var limit = Mathf.Max(2, config.historyLimit);
            if (history.Count > limit)
                history.RemoveRange(0, history.Count - limit);
        }

        static bool SilenceSimulator()
        {
            var found = false;
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == "XRInteractionSimulator")
                {
                    behaviour.gameObject.SetActive(false);
                    found = true;
                }
            }
            return found;
        }

        void SetConnection(string status)
        {
            connectionStatus = status;
            PublishStatus();
        }

        void FailStatus(string message)
        {
            connectionStatus = message;
            Debug.LogError("[GenJutsu] " + message);
            StatusTextChanged?.Invoke("Status: " + message + " | " + mqtt.StatusText);
        }

        void PublishStatus(string temporary = null)
        {
            var main = temporary ?? connectionStatus;
            var voiceNote = config.selectedVoice != PiperVoiceSpeaker.VoiceId ? " | Voz em inglês" : "";
            StatusTextChanged?.Invoke("Status: " + main + voiceNote + " | " + mqtt.StatusText);
        }
    }
}
