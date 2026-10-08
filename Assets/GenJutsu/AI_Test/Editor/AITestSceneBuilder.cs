using GenJutsu.AITest;
using TMPro;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GenJutsu.AITest.EditorTools
{
    public static class AITestSceneBuilder
    {
        const string ScenePath = "Assets/GenJutsu/AI_Test/Scenes/AI_Test.unity";
        const string ConfigPath = "Assets/GenJutsu/AI_Test/Resources/AIConfig.asset";
        const string ModelPath = "Assets/GenJutsu/AI_Test/VoiceModel/pt_BR-faber-medium.onnx";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public static void Build()
        {
            var previous = EditorSceneManager.GetActiveScene();
            if (previous.path != ScenePath && previous.isDirty)
                Debug.LogWarning("[GenJutsu] A cena aberta está suja e não será salva: " + previous.path);

            var config = AssetDatabase.LoadAssetAtPath<AIConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AIConfig>();
                config.apiKey = "";
                config.systemPrompt = AIConfig.DefaultSystemPrompt;
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.6f, -4f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();

            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var background = CreateRect("Background", canvasObject.transform);
            Stretch(background);
            background.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f);

            var panel = CreateRect("Panel", canvasObject.transform);
            panel.anchorMin = new Vector2(0.08f, 0.08f);
            panel.anchorMax = new Vector2(0.92f, 0.92f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 0.96f);

            var title = CreateLabel("Title", panel, font, 42, FontStyles.Bold, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.9f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.text = "GENJUTSU AI";

            var scrollObject = CreateRect("History", panel);
            scrollObject.anchorMin = new Vector2(0.05f, 0.28f);
            scrollObject.anchorMax = new Vector2(0.95f, 0.88f);
            scrollObject.offsetMin = Vector2.zero;
            scrollObject.offsetMax = Vector2.zero;
            var scroll = scrollObject.gameObject.AddComponent<ScrollRect>();
            var viewport = CreateRect("Viewport", scrollObject);
            Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 1f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 800f);
            var history = CreateLabel("HistoryText", content, font, 26, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Stretch(history.rectTransform);
            history.margin = new Vector4(24f, 16f, 24f, 16f);
            history.text = "CORVO: Olá. Sou o agente do GenJutsu.";
            var fitter = history.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;

            var inputObject = CreateRect("Input", panel);
            inputObject.anchorMin = new Vector2(0.05f, 0.16f);
            inputObject.anchorMax = new Vector2(0.95f, 0.25f);
            inputObject.offsetMin = Vector2.zero;
            inputObject.offsetMax = Vector2.zero;
            inputObject.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f);
            var input = inputObject.gameObject.AddComponent<TMP_InputField>();
            var inputText = CreateLabel("Text", inputObject, font, 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Stretch(inputText.rectTransform);
            inputText.margin = new Vector4(16f, 6f, 16f, 6f);
            var placeholder = CreateLabel("Placeholder", inputObject, font, 24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform);
            placeholder.margin = new Vector4(16f, 6f, 16f, 6f);
            placeholder.text = "A fala aparece aqui";
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            input.textViewport = inputObject;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var voice = CreateButton("VOZ: FABER", panel, font, new Vector2(0.05f, 0.08f), new Vector2(0.28f, 0.15f));
            var talk = CreateButton("FALAR", panel, font, new Vector2(0.30f, 0.08f), new Vector2(0.45f, 0.15f));
            var test = CreateButton("TESTAR IA", panel, font, new Vector2(0.47f, 0.08f), new Vector2(0.68f, 0.15f));
            var clear = CreateButton("LIMPAR CONVERSA", panel, font, new Vector2(0.70f, 0.08f), new Vector2(0.95f, 0.15f));

            var status = CreateLabel("Status", panel, font, 22, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            status.rectTransform.anchorMin = new Vector2(0.05f, 0.01f);
            status.rectTransform.anchorMax = new Vector2(0.95f, 0.07f);
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = Vector2.zero;
            status.text = "Status: Inicializando...";

            var root = new GameObject("GenJutsuAI");
            var audio = root.AddComponent<AudioSource>();
            var ai = root.AddComponent<GenJutsuAI>();
            var groq = root.AddComponent<GroqClient>();
            var piper = root.AddComponent<PiperVoiceSpeaker>();
            var speech = root.AddComponent<GroqSpeechSpeaker>();
            var house = root.AddComponent<HouseMqttClient>();
            var mic = root.AddComponent<MicrophoneCapture>();
            var ui = root.AddComponent<ChatUI>();
            piper.modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
            piper.audioSource = audio;
            speech.audioSource = audio;
            speech.client = groq;
            ai.config = config;
            ai.client = groq;
            ai.piper = piper;
            ai.groqVoice = speech;
            ai.mqtt = house;
            ui.brain = ai;
            ui.microphone = mic;
            ui.titleText = title;
            ui.historyText = history;
            ui.statusText = status;
            ui.inputField = input;
            ui.voiceButton = voice;
            ui.voiceButtonLabel = voice.GetComponentInChildren<TMP_Text>();
            ui.talkButton = talk;
            ui.testButton = test;
            ui.clearButton = clear;
            ui.scrollRect = scroll;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[GenJutsu] Cena salva em " + ScenePath + " modelo=" + (piper.modelAsset != null));
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name).AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static TMP_Text CreateLabel(string name, Transform parent, TMP_FontAsset font, int size, FontStyles style, TextAlignmentOptions align)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        static Button CreateButton(string label, Transform parent, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            var rect = CreateRect(label, parent);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.45f, 0.75f);
            var button = rect.gameObject.AddComponent<Button>();
            var text = CreateLabel("Label", rect, font, 20, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

    }
}
