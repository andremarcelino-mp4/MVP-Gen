using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GenJutsu.AITest
{
    public class ChatUI : MonoBehaviour
    {
        public GenJutsuAI brain;
        public MicrophoneCapture microphone;
        public TMP_Text titleText;
        public TMP_Text historyText;
        public TMP_Text statusText;
        public TMP_InputField inputField;
        public TMP_Dropdown voiceDropdown;
        public Button voiceButton;
        public TMP_Text voiceButtonLabel;
        public Button talkButton;
        public Button testButton;
        public Button clearButton;
        public ScrollRect scrollRect;

        readonly List<string> lines = new List<string>();

        void Awake()
        {
            if (brain == null)
                brain = GetComponent<GenJutsuAI>();
            if (microphone == null)
                microphone = GetComponent<MicrophoneCapture>();
        }

        void OnEnable()
        {
            if (brain == null)
                return;
            brain.MessageAdded += OnMessage;
            brain.HistoryCleared += OnCleared;
            brain.StatusTextChanged += OnStatus;
        }

        void OnDisable()
        {
            if (brain == null)
                return;
            brain.MessageAdded -= OnMessage;
            brain.HistoryCleared -= OnCleared;
            brain.StatusTextChanged -= OnStatus;
        }

        void Start()
        {
            if (titleText != null)
                titleText.text = "GENJUTSU AI";
            if (testButton != null)
                testButton.onClick.AddListener(() => brain.SendTest());
            if (clearButton != null)
                clearButton.onClick.AddListener(() => brain.ClearConversation());
            if (inputField != null)
            {
                inputField.onSubmit.AddListener(text => brain.SubmitText(text));
                inputField.ActivateInputField();
            }
            if (voiceDropdown != null)
            {
                voiceDropdown.ClearOptions();
                var options = new List<string> { "Faber (português)" };
                foreach (var voice in GroqSpeechSpeaker.EnglishVoices)
                    options.Add(voice + " (inglês)");
                voiceDropdown.AddOptions(options);
                voiceDropdown.onValueChanged.AddListener(index =>
                {
                    var id = index == 0 ? PiperVoiceSpeaker.VoiceId : GroqSpeechSpeaker.EnglishVoices[index - 1];
                    brain.SetVoice(id);
                });
            }
            if (voiceButton != null)
            {
                voiceButton.onClick.AddListener(CycleVoice);
                RefreshVoiceLabel();
            }
            if (talkButton != null)
            {
                var trigger = talkButton.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = talkButton.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerDown, _ =>
                {
                    if (!microphone.Begin())
                        OnStatus("Status: Erro | nenhum microfone");
                });
                AddTrigger(trigger, EventTriggerType.PointerUp, _ =>
                {
                    var wav = microphone.EndWav();
                    if (wav != null)
                        brain.SubmitAudio(wav);
                });
            }
            OnCleared();
        }

        void OnMessage(string speaker, string text)
        {
            lines.Add(speaker + ": " + text);
            RefreshHistory();
            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }
            if (scrollRect != null)
                Canvas.ForceUpdateCanvases();
        }

        void OnCleared()
        {
            lines.Clear();
            lines.Add("CORVO: Olá. Sou o agente do GenJutsu.");
            RefreshHistory();
        }

        void RefreshHistory()
        {
            if (historyText == null)
                return;
            historyText.text = string.Join("\n\n", lines.ToArray());
            var rect = historyText.rectTransform;
            var height = Mathf.Max(80f, historyText.preferredHeight + 24f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, -height);
            rect.offsetMax = new Vector2(-16f, 0f);
            if (scrollRect != null && scrollRect.content != null)
            {
                var content = scrollRect.content;
                content.sizeDelta = new Vector2(0f, height);
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        void OnStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }

        void CycleVoice()
        {
            var order = new List<string> { PiperVoiceSpeaker.VoiceId };
            order.AddRange(GroqSpeechSpeaker.EnglishVoices);
            var current = order.IndexOf(brain.SelectedVoice);
            var next = order[(current + 1) % order.Count];
            brain.SetVoice(next);
            RefreshVoiceLabel();
        }

        void RefreshVoiceLabel()
        {
            if (voiceButtonLabel == null)
                return;
            var id = brain.SelectedVoice;
            voiceButtonLabel.text = id == PiperVoiceSpeaker.VoiceId ? "VOZ: FABER" : "VOZ: " + id.ToUpperInvariant();
        }

        static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }
    }
}
