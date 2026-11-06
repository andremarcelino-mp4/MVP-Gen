using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Product loop: white hub (mesa + pergaminho) → SessionGate → morph. No house picker.
    /// </summary>
    [RequireComponent(typeof(ProfileDemoDirector))]
    public class DojoDirector : MonoBehaviour
    {
        public ProfileDemoDirector demo;
        public SessionGate gate;
        public DojoShell shell;
        public ProfileMorph morph;
        public HubPropsDissolve hubDissolve;
        public GazePublisher gaze;

        void Reset()
        {
            demo = GetComponent<ProfileDemoDirector>();
        }

        void Awake()
        {
            if (demo == null)
                demo = GetComponent<ProfileDemoDirector>();
        }

        void OnEnable()
        {
            Bind();
        }

        void OnDisable()
        {
            if (gate != null)
                gate.ProfileResolved -= OnProfile;
        }

        public void Bind()
        {
            if (gate == null)
                return;
            gate.ProfileResolved -= OnProfile;
            gate.ProfileResolved += OnProfile;
        }

        void OnProfile(ProfileId id)
        {
            if (demo != null && demo.Current == ProfileDemoDirector.State.WhiteSpace)
                demo.EnterLookFromSession(id);
            if (gaze != null && GuestSession.HasProfile)
                gaze.StartSession(GuestSession.Code, id);
        }
    }
}
