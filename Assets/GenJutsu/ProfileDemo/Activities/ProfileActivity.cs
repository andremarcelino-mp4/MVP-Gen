using GenJutsu.TableProps;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// One registrable interaction inside a house. Scores and captures go to the session recorder.
    /// </summary>
    public abstract class ProfileActivity : MonoBehaviour
    {
        public ProfileId profile;
        public string activityId;
        public string displayName;

        public abstract void ResetActivity();

        public virtual void OnHouseExit()
        {
        }

        protected void Report(float value, string unit, bool higherIsBetter = true, string detail = null)
        {
            ReportAs(activityId, displayName, value, unit, higherIsBetter, detail);
        }

        protected void ReportAs(string id, string label, float value, string unit, bool higherIsBetter = true, string detail = null)
        {
            var recorder = DemoSessionRecorder.Instance;
            if (recorder != null)
                recorder.ReportScore(profile, id, label, value, unit, higherIsBetter, detail);
        }

        protected void Popup(Vector3 position, string text)
        {
            ScorePopup.Spawn(position, text, Profiles.Get(profile).Accent);
        }

        protected void SaveTexture(Texture2D texture, string label)
        {
            var recorder = DemoSessionRecorder.Instance;
            if (recorder != null)
                recorder.SaveCapture(profile, label, texture);
        }

        protected void SaveView(string label)
        {
            var recorder = DemoSessionRecorder.Instance;
            if (recorder != null)
                recorder.CaptureView(profile, label);
        }

        protected static bool IsHeld(ResettablePose pose)
        {
            return pose != null && pose.IsHeld;
        }
    }
}
