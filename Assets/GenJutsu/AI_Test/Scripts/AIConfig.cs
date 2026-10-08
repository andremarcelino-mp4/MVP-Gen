using System;
using System.IO;
using UnityEngine;

namespace GenJutsu.AITest
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "GenJutsu/AI Config")]
    public class AIConfig : ScriptableObject
    {
        public const string DefaultSystemPrompt =
            "Você é o Corvo, agente de inteligência artificial do projeto GenJutsu AI.\n\n" +
            "Você é o assistente inteligente de uma casa futurista.\n\n" +
            "Seu papel é conversar naturalmente com o usuário, entender suas preferências e futuramente controlar elementos da casa virtual.\n\n" +
            "Nesta primeira fase você está em um ambiente de testes.\n\n" +
            "Responda em português do Brasil.\n\n" +
            "Seja natural, amigável, engraçado, sarcastico e objetivo.\n\n" +
            "Não invente ações que ainda não foram implementadas.\n\n" +
            "Quando o usuário perguntar sobre funcionalidades futuras, explique que elas serão integradas posteriormente.\n\n" +
            "Você pode conversar sobre tecnologia, casa inteligente, personalização e o conceito do GenJutsu.";

        [TextArea(2, 4)]
        [Tooltip("Deixe vazio. A chave vem da variável GROQ_API_KEY ou de groq.local.json, que o Git ignora.")]
        public string apiKey;

        public string model = "openai/gpt-oss-120b";
        [TextArea(8, 16)] public string systemPrompt = DefaultSystemPrompt;
        public float temperature = 0.7f;
        public int maxTokens = 512;
        public string whisperModel = "whisper-large-v3-turbo";
        public string whisperLanguage = "pt";
        public string groqTtsModel = "canopylabs/orpheus-v1-english";
        public string selectedVoice = "faber";
        public string mqttHost = "127.0.0.1";
        public int mqttPort = 1883;
        public string mqttUserTopic = "genjutsu/ai/user";
        public string mqttAssistantTopic = "genjutsu/ai/assistant";
        public int historyLimit = 20;

        public static AIConfig LoadOrCreate()
        {
            var loaded = Resources.Load<AIConfig>("AIConfig");
            if (loaded != null)
                return loaded;
            var created = CreateInstance<AIConfig>();
            created.systemPrompt = DefaultSystemPrompt;
            return created;
        }

        public string ResolveApiKey()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment.Trim();

            var localPath = Path.Combine(Application.dataPath, "GenJutsu/AI_Test/groq.local.json");
            if (File.Exists(localPath))
            {
                try
                {
                    var local = JsonUtility.FromJson<LocalGroqFile>(File.ReadAllText(localPath));
                    if (local != null && !string.IsNullOrWhiteSpace(local.apiKey))
                        return local.apiKey.Trim();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[GenJutsu] Etapa configuração: groq.local.json inválido. " + exception.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogWarning("[GenJutsu] A chave veio do campo apiKey do AIConfig. Não commite esse asset com a chave preenchida.");
                return apiKey.Trim();
            }

            return null;
        }

        [Serializable]
        class LocalGroqFile
        {
            public string apiKey;
        }
    }
}
