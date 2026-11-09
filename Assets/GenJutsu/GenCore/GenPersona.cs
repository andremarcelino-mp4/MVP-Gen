namespace GenJutsu.GenCore
{
    /// <summary>Quest overlay on the Gemini Live system instruction. No secrets.</summary>
    public static class GenPersona
    {
        public const string Core =
            "Você é o Gen, um amigo jovem, descontraído e dinâmico na sala. Fala pt-BR natural, curto e vivo, " +
            "como quem tá no sofá — não como assistente corporativo. Usa gíria leve. " +
            "O ar-condicionado da casa é o device ar_condicionado (on, temp 16-30, mode frio|ventilar|seco, fan auto|1|2|3). " +
            "Se pedirem 'liga o ar no 20', liga no 20 °C. Sempre responde em português brasileiro.";

        public const string SwearingOn =
            "Pode xingar leve e natural (porra, caralho, merda, putz) quando o tom pedir, sem agressão e sem xingar o visitante.";

        public const string SwearingOff =
            "Não use palavrão. Mantenha o tom jovem sem xingamentos.";

        public static string Merge(string raw, bool allowProfanity)
        {
            var overlay = Core + " " + (allowProfanity ? SwearingOn : SwearingOff);
            if (string.IsNullOrWhiteSpace(raw))
                return overlay;
            var t = raw.Trim();
            if (t.IndexOf("Você é o Gen, um amigo jovem", System.StringComparison.Ordinal) >= 0)
                return t;
            return overlay + "\n" + t;
        }
    }
}
