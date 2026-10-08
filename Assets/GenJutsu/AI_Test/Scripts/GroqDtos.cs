using System;

namespace GenJutsu.AITest
{
    [Serializable]
    public class GroqChatResponse
    {
        public GroqChoice[] choices;
        public GroqUsage usage;
    }

    [Serializable]
    public class GroqChoice
    {
        public GroqResponseMessage message;
    }

    [Serializable]
    public class GroqResponseMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class GroqUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [Serializable]
    public class GroqTranscriptionResponse
    {
        public string text;
    }

    [Serializable]
    public class GroqErrorResponse
    {
        public GroqErrorDetail error;
    }

    [Serializable]
    public class GroqErrorDetail
    {
        public string message;
        public string type;
        public string code;
    }

    public class GroqCallResult
    {
        public bool Success;
        public string Text;
        public string ErrorMessage;
        public long StatusCode;
        public string Stage;
        public int TotalTokens;
        public byte[] AudioWav;
    }
}
