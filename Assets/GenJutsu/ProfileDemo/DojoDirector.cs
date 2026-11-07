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

        ProfileId pendingId;
        bool readyToStart;
        bool leaveAfterEnter;

        void OnDisable()
        {
            if (gate != null)
            {
                gate.ProfileResolved -= OnProfile;
                gate.SessionEnded -= OnSessionEnded;
            }
        }

        public void Bind()
        {
            if (gate == null)
                return;
            gate.ProfileResolved -= OnProfile;
            gate.ProfileResolved += OnProfile;
            gate.SessionEnded -= OnSessionEnded;
            gate.SessionEnded += OnSessionEnded;
        }

        void Update()
        {
            if (demo == null)
                return;
            if (leaveAfterEnter && demo.Current == ProfileDemoDirector.State.InHouse)
            {
                leaveAfterEnter = false;
                demo.ReturnToWhiteSpace();
            }

        }

        /// <summary>The visit is known, but the morph waits for the table plaque.</summary>
        public bool ReadyToStart => readyToStart;

        public bool TryBeginVisit()
        {
            if (!readyToStart || demo == null || leaveAfterEnter)
                return false;
            if (demo.Current != ProfileDemoDirector.State.WhiteSpace)
                return false;
            return demo.EnterLookFromSession(pendingId);
        }

        void OnProfile(ProfileId id)
        {
            pendingId = id;
            readyToStart = true;
            if (gaze != null && GuestSession.HasProfile)
                gaze.StartSession(GuestSession.Code, id);
            if (demo != null && demo.Current != ProfileDemoDirector.State.WhiteSpace)
                RequestWhite();
        }

        void OnSessionEnded()
        {
            if (gaze != null)
                gaze.StopSession();
            readyToStart = false;
            RequestWhite();
        }

        void RequestWhite()
        {
            if (demo == null)
                return;
            if (demo.Current == ProfileDemoDirector.State.InHouse)
                demo.ReturnToWhiteSpace();
            else if (demo.Current == ProfileDemoDirector.State.Transforming)
                leaveAfterEnter = true;
        }
    }
}
