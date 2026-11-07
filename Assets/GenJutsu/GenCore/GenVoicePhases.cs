namespace GenJutsu.GenCore
{
    public enum GenVoicePhase
    {
        Idle = 0,
        Listening = 1,
        Thinking = 2,
        Speaking = 3,
        Offline = 4
    }

    /// <summary>
    /// Push-to-talk phases. Pressing talk during speaking or thinking is the interruption.
    /// </summary>
    public static class GenVoicePhases
    {
        public static bool IsInterruption(GenVoicePhase phase)
        {
            return phase == GenVoicePhase.Speaking || phase == GenVoicePhase.Thinking;
        }

        public static GenVoicePhase AfterPttPressed(GenVoicePhase phase)
        {
            return GenVoicePhase.Listening;
        }

        public static GenVoicePhase AfterPttReleased(GenVoicePhase phase)
        {
            if (phase == GenVoicePhase.Listening)
                return GenVoicePhase.Thinking;
            return phase;
        }

        public static GenVoicePhase AfterModelAudio(GenVoicePhase phase)
        {
            if (phase == GenVoicePhase.Offline)
                return phase;
            return GenVoicePhase.Speaking;
        }

        public static GenVoicePhase AfterTurnComplete(GenVoicePhase phase, bool pttHeld)
        {
            if (pttHeld)
                return GenVoicePhase.Listening;
            if (phase == GenVoicePhase.Speaking || phase == GenVoicePhase.Thinking)
                return GenVoicePhase.Idle;
            return phase;
        }

        public static GenVoicePhase AfterServerInterrupted(GenVoicePhase phase, bool pttHeld)
        {
            return pttHeld ? GenVoicePhase.Listening : GenVoicePhase.Idle;
        }

        public static GenVoicePhase AfterFailure()
        {
            return GenVoicePhase.Offline;
        }

        public static string Label(GenVoicePhase phase)
        {
            switch (phase)
            {
                case GenVoicePhase.Listening: return "Gen · a ouvir";
                case GenVoicePhase.Thinking: return "Gen · a pensar";
                case GenVoicePhase.Speaking: return "Gen · a falar";
                case GenVoicePhase.Offline: return "Gen · offline";
                default: return "Gen";
            }
        }
    }
}
