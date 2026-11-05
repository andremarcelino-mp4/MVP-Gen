using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Head-forward yaw/pitch for landing gaze. Broadcast is stubbed this PR — see docs/GAZE-PUBLISHER.md.
    /// </summary>
    public interface IGazePublisher
    {
        void StartSession(string code, ProfileId profile);
        void StopSession();
    }

    public class GazePublisher : MonoBehaviour, IGazePublisher
    {
        public bool broadcasting;
        public string channel;
        public float yaw;
        public float pitch;
        public float sendHz = 12f;

        float nextSend;
        string code;
        ProfileId profile;
        bool helloSent;

        public void StartSession(string sessionCode, ProfileId id)
        {
            code = sessionCode;
            profile = id;
            channel = "gaze:" + sessionCode;
            broadcasting = false;
            helloSent = false;
            Debug.Log("[GazePublisher] stub ready for " + channel + " profile=" + id + " (no Realtime client in this PR)");
        }

        public void StopSession()
        {
            broadcasting = false;
            channel = null;
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            var f = cam.transform.forward;
            yaw = Mathf.Atan2(f.x, f.z);
            pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f));
            if (!broadcasting)
                return;
            if (Time.unscaledTime < nextSend)
                return;
            nextSend = Time.unscaledTime + 1f / Mathf.Max(10f, sendHz);
            // Fase 4: POST Broadcast pose. Sem SDK aqui de propósito.
            if (!helloSent)
                helloSent = true;
        }
    }
}
