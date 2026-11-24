using System.Globalization;

namespace GenJutsu.GenCore
{
    /// <summary>
    /// Token counts from a live Gemini frame, POST /api/gen/chat, or GET /api/session.
    /// </summary>
    public struct LlmTokenUsage
    {
        public int In;
        public int Out;
        public int Total;
        public int Calls;

        public bool HasCounts => Total > 0 || In > 0 || Out > 0;

        public static LlmTokenUsage PreferLocal(LlmTokenUsage local, LlmTokenUsage api)
        {
            return local.HasCounts ? local : api;
        }

        public void Add(LlmTokenUsage other)
        {
            if (!other.HasCounts && other.Calls <= 0)
                return;
            In += other.In;
            Out += other.Out;
            Total += other.Total > 0 ? other.Total : other.In + other.Out;
            Calls += other.Calls > 0 ? other.Calls : 1;
        }

        /// <summary>
        /// Gemini Live often repeats a running total. Replace when the new total is not smaller;
        /// otherwise treat it as a per-turn delta.
        /// </summary>
        public void ObserveLive(LlmTokenUsage snapshot)
        {
            if (!snapshot.HasCounts)
                return;
            if (snapshot.Total >= Total && snapshot.In >= In)
            {
                var calls = Calls;
                this = snapshot;
                Calls = calls + 1;
                if (Total <= 0)
                    Total = In + Out;
                return;
            }

            Add(snapshot);
        }

        public string FormatPtBr()
        {
            var total = Total > 0 ? Total : In + Out;
            return "Esta sessão usou " + Pt(total) + " tokens (entrada " + Pt(In) + " · saída " + Pt(Out) + ")";
        }

        public static string Pt(int n)
        {
            return n.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));
        }

        public static bool TryRead(JObj obj, out LlmTokenUsage usage)
        {
            usage = default;
            if (obj == null)
                return false;
            Fill(obj, ref usage);
            var nested = obj.Obj("usage") ?? obj.Obj("usageMetadata") ?? obj.Obj("usage_metadata");
            if (nested != null)
                Fill(nested, ref usage);
            if (usage.Total <= 0 && (usage.In > 0 || usage.Out > 0))
                usage.Total = usage.In + usage.Out;
            return usage.HasCounts;
        }

        public static bool TryRead(string json, out LlmTokenUsage usage)
        {
            usage = default;
            return GenJson.TryParse(json, out var root) && root is JObj obj && TryRead(obj, out usage);
        }

        static void Fill(JObj obj, ref LlmTokenUsage usage)
        {
            ReadInt(obj, ref usage.In, "tokens_in", "prompt_tokens", "promptTokenCount");
            ReadInt(obj, ref usage.Out, "tokens_out", "completion_tokens", "candidatesTokenCount", "responseTokenCount");
            ReadInt(obj, ref usage.Total, "tokens_total", "total_tokens", "totalTokenCount");
            ReadInt(obj, ref usage.Calls, "calls");
        }

        static void ReadInt(JObj obj, ref int field, params string[] keys)
        {
            for (var i = 0; i < keys.Length; i++)
            {
                if (obj.Num(keys[i], out var n))
                {
                    field = (int)n;
                    return;
                }

                var s = obj.Str(keys[i]);
                if (!string.IsNullOrEmpty(s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    field = parsed;
                    return;
                }
            }
        }
    }
}
