using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Networking;

namespace GenJutsu.AITest
{
    public class PiperVoiceSpeaker : MonoBehaviour
    {
        public const string VoiceId = "faber";
        public ModelAsset modelAsset;
        public AudioSource audioSource;
        public string configFileName = "pt_BR-faber-medium.onnx.json";

        Worker worker;
        Dictionary<string, int[]> phonemeIds;
        string espeakVoice = "pt-br";
        float noiseScale = 0.667f;
        float lengthScale = 1f;
        float noiseW = 0.8f;
        int sampleRate = 22050;
        bool configReady;

        public bool ModelReady => modelAsset != null && configReady;

        public IEnumerator Prepare()
        {
            LoadConfig();
            if (!EspeakPhonemizer.IsReady)
                yield return EspeakPhonemizer.Initialize();
            if (modelAsset == null)
                Debug.LogWarning("[GenJutsu] Etapa voz: modelo Piper não atribuído. A resposta continua em texto.");
        }

        public IEnumerator Speak(string text, Action<string> onError)
        {
            if (!ModelReady || !EspeakPhonemizer.IsReady)
            {
                const string message = "Voz em português não instalada.";
                Debug.LogWarning("[GenJutsu] Etapa voz: " + message);
                onError?.Invoke(message);
                yield break;
            }

            List<int> ids;
            try
            {
                ids = BuildIds(text);
            }
            catch (Exception exception)
            {
                Debug.LogError("[GenJutsu] Etapa voz: fonemas. " + exception.Message);
                onError?.Invoke("Voz em português não instalada.");
                yield break;
            }

            if (ids.Count < 3)
            {
                onError?.Invoke("Voz em português não instalada.");
                yield break;
            }

            float[] audio;
            try
            {
                audio = Synthesize(ids);
            }
            catch (Exception exception)
            {
                Debug.LogError("[GenJutsu] Etapa voz: inferência. " + exception.Message);
                onError?.Invoke("Voz em português não instalada.");
                yield break;
            }

            if (audio == null || audio.Length == 0)
            {
                onError?.Invoke("Voz em português não instalada.");
                yield break;
            }

            var clip = AudioClip.Create("Corvo", audio.Length, 1, sampleRate, false);
            clip.SetData(audio, 0);
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
            while (audioSource.isPlaying)
                yield return null;
        }

        float[] Synthesize(List<int> ids)
        {
            if (worker == null)
                worker = new Worker(ModelLoader.Load(modelAsset), BackendType.CPU);

            var idArray = ids.ToArray();
            using (var input = new Tensor<int>(new TensorShape(1, idArray.Length), idArray))
            using (var lengths = new Tensor<int>(new TensorShape(1), new[] { idArray.Length }))
            using (var scales = new Tensor<float>(new TensorShape(3), new[] { noiseScale, lengthScale, noiseW }))
            {
                worker.SetInput("input", input);
                worker.SetInput("input_lengths", lengths);
                worker.SetInput("scales", scales);
                worker.Schedule();
                var output = worker.PeekOutput() as Tensor<float>;
                return output != null ? output.DownloadToArray() : null;
            }
        }

        List<int> BuildIds(string text)
        {
            var phonemes = EspeakPhonemizer.ToPhonemes(text, espeakVoice);
            var normalized = phonemes.Normalize(NormalizationForm.FormD);
            var ids = new List<int> { phonemeIds["^"][0] };
            var skipLanguage = false;
            for (var index = 0; index < normalized.Length; index++)
            {
                string phoneme;
                if (char.IsHighSurrogate(normalized[index]) && index + 1 < normalized.Length)
                {
                    phoneme = normalized.Substring(index, 2);
                    index++;
                }
                else
                {
                    phoneme = normalized[index].ToString();
                }
                if (phoneme == "(")
                {
                    skipLanguage = true;
                    continue;
                }
                if (phoneme == ")")
                {
                    skipLanguage = false;
                    continue;
                }
                if (skipLanguage)
                    continue;
                if (phonemeIds.TryGetValue("c", out _) && phoneme == "c" && phonemeIds.ContainsKey("k"))
                    phoneme = "k";
                if (!phonemeIds.TryGetValue(phoneme, out var mapped))
                    continue;
                ids.AddRange(mapped);
                ids.Add(phonemeIds["_"][0]);
            }
            ids.Add(phonemeIds["$"][0]);
            return ids;
        }

        void LoadConfig()
        {
            var path = Path.Combine(Application.dataPath, "GenJutsu/AI_Test/VoiceModel", configFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[GenJutsu] Etapa voz: config do Piper ausente em " + path);
                return;
            }

            var json = File.ReadAllText(path);
            var voice = Regex.Match(json, "\"voice\"\\s*:\\s*\"([^\"]+)\"");
            if (voice.Success)
                espeakVoice = voice.Groups[1].Value;
            sampleRate = ReadNumber(json, "sample_rate", sampleRate);
            noiseScale = ReadNumber(json, "noise_scale", noiseScale);
            lengthScale = ReadNumber(json, "length_scale", lengthScale);
            noiseW = ReadNumber(json, "noise_w", noiseW);
            phonemeIds = new Dictionary<string, int[]>();
            foreach (Match match in Regex.Matches(json, "\"((?:\\\\.|[^\"\\\\])*)\"\\s*:\\s*\\[\\s*([0-9,\\s]+)\\]"))
            {
                var key = Regex.Unescape(match.Groups[1].Value);
                var parts = match.Groups[2].Value.Split(',');
                var values = new List<int>();
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), out var number))
                        values.Add(number);
                }
                if (values.Count > 0 && !phonemeIds.ContainsKey(key))
                    phonemeIds.Add(key, values.ToArray());
            }
            configReady = phonemeIds.ContainsKey("^") && phonemeIds.ContainsKey("$") && phonemeIds.ContainsKey("_");
        }

        static int ReadNumber(string json, string name, int fallback)
        {
            return (int)ReadNumber(json, name, (float)fallback);
        }

        static float ReadNumber(string json, string name, float fallback)
        {
            var match = Regex.Match(json, "\"" + name + "\"\\s*:\\s*([0-9.]+)");
            if (match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
                return value;
            return fallback;
        }

        void OnDestroy()
        {
            if (worker != null)
                worker.Dispose();
        }
    }

    static class EspeakPhonemizer
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        const string Library = "ttsespeak";
#else
        const string Library = "libespeak-ng";
#endif

        public static bool IsReady { get; private set; }
        static bool initializing;

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        static extern int espeak_Initialize(int output, int bufferLength, string path, int options);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        static extern int espeak_SetVoiceByName(string name);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr espeak_TextToPhonemes(ref IntPtr text, int textMode, int phonemeMode);

        public static IEnumerator Initialize()
        {
            if (IsReady || initializing)
                yield break;
            initializing = true;
            string dataRoot;
#if UNITY_ANDROID && !UNITY_EDITOR
            dataRoot = Path.Combine(Application.persistentDataPath, "GenJutsu");
            var marker = Path.Combine(dataRoot, "espeak-ng-data", "phontab");
            if (!File.Exists(marker))
            {
                var zipPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data.zip");
                using (var request = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, "GenJutsu/espeak-ng-data.zip")))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("[GenJutsu] Etapa voz: não foi possível ler o espeak-ng no APK. " + request.error);
                        initializing = false;
                        yield break;
                    }
                    Directory.CreateDirectory(dataRoot);
                    File.WriteAllBytes(zipPath, request.downloadHandler.data);
                    if (Directory.Exists(Path.Combine(dataRoot, "espeak-ng-data")))
                        Directory.Delete(Path.Combine(dataRoot, "espeak-ng-data"), true);
                    ZipFile.ExtractToDirectory(zipPath, dataRoot);
                }
            }
#else
            dataRoot = Path.Combine(Application.dataPath, "GenJutsu/AI_Test");
            yield return null;
#endif
            try
            {
                var status = espeak_Initialize(2, 0, dataRoot, 0);
                IsReady = status > 0;
                if (!IsReady)
                    Debug.LogError("[GenJutsu] Etapa voz: espeak_Initialize retornou " + status);
            }
            catch (Exception exception)
            {
                Debug.LogError("[GenJutsu] Etapa voz: biblioteca espeak-ng ausente. " + exception.Message);
                IsReady = false;
            }
            initializing = false;
        }

        public static string ToPhonemes(string text, string voice)
        {
            if (!IsReady)
                throw new InvalidOperationException("espeak-ng não inicializado.");
            if (espeak_SetVoiceByName(voice) != 0)
                throw new InvalidOperationException("Voz espeak não encontrada: " + voice);

            var bytes = Encoding.UTF8.GetBytes(text + "\0");
            var memory = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, memory, bytes.Length);
                var pointer = memory;
                var builder = new StringBuilder();
                var guard = 0;
                while (pointer != IntPtr.Zero && guard++ < 32)
                {
                    var phonemes = espeak_TextToPhonemes(ref pointer, 1, 0x02);
                    if (phonemes == IntPtr.Zero)
                        break;
                    builder.Append(ReadUtf8(phonemes));
                }
                return builder.ToString();
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        static string ReadUtf8(IntPtr pointer)
        {
            var length = 0;
            while (Marshal.ReadByte(pointer, length) != 0)
                length++;
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
