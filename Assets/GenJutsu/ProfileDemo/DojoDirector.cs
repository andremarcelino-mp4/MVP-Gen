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
            if (gate != null)
                gate.ProfileResolved += OnProfile;
        }

        void OnDisable()
        {
            if (gate != null)
                gate.ProfileResolved -= OnProfile;
        }

        void OnProfile(ProfileId id)
        {
            if (demo != null)
                demo.EnterLookFromSession(id);
            if (gaze != null && GuestSession.HasProfile)
                gaze.StartSession(GuestSession.Code, id);
        }
    }
}
