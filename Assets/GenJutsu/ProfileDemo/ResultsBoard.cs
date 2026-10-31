using System.Text;
using TMPro;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Post-experience summary shown next to the table in the white void.
    /// </summary>
    public class ResultsBoard : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text body;
        public TMP_Text footer;

        public void Refresh()
        {
            var recorder = DemoSessionRecorder.Instance;
            if (recorder == null || body == null)
                return;

            var sb = new StringBuilder();
            var visited = 0;
            for (var i = 0; i < Profiles.Count; i++)
            {
                var info = Profiles.Get((ProfileId)i);
                var summary = recorder.GetSummary((ProfileId)i);
                var hex = ColorUtility.ToHtmlStringRGB(info.Accent);
                sb.Append("<color=#").Append(hex).Append("><b>").Append(info.Letter).Append("  ")
                    .Append(info.Title).Append(" / ").Append(info.Subtitle).Append("</b></color>");
                if (summary == null || summary.visits == 0)
                {
                    sb.Append("  <alpha=#88>ainda não visitada<alpha=#FF>\n\n");
                    continue;
                }

                visited++;
                sb.Append("  <size=80%>").Append(summary.visits).Append(summary.visits == 1 ? " visita, " : " visitas, ")
                    .Append(FormatSeconds(summary.totalSeconds)).Append(", ").Append(summary.captures.Count)
                    .Append(summary.captures.Count == 1 ? " captura" : " capturas").Append("</size>\n");
                if (summary.activities.Count == 0)
                    sb.Append("<size=75%><alpha=#99>   nenhuma interação registrada<alpha=#FF></size>\n");
                foreach (var a in summary.activities)
                {
                    sb.Append("<size=75%>   ").Append(a.displayName).Append(": <b>").Append(FormatValue(a.best))
                        .Append(' ').Append(a.unit).Append("</b>");
                    if (!string.IsNullOrEmpty(a.detail))
                        sb.Append("  <alpha=#99>").Append(a.detail).Append("<alpha=#FF>");
                    sb.Append("</size>\n");
                }

                sb.Append('\n');
            }

            if (title != null)
                title.text = visited == 0 ? "Sua jornada começa aqui" : "Sua jornada  (" + visited + "/4)";
            body.text = sb.ToString();
            if (footer != null)
                footer.text = "Sessão " + recorder.SessionId + " salva no dispositivo";
        }

        static string FormatValue(float v)
        {
            return Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.0");
        }

        static string FormatSeconds(float seconds)
        {
            var s = Mathf.RoundToInt(seconds);
            return s < 60 ? s + " s" : s / 60 + " min " + s % 60 + " s";
        }
    }
}
