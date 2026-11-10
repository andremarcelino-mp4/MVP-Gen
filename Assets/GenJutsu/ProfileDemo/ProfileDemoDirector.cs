using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Runs the demo loop: white void, profile house, back to the white void.
    /// </summary>
    public class ProfileDemoDirector : MonoBehaviour
    {
        public enum State
        {
            WhiteSpace,
            Transforming,
            InHouse,
            Dissolving
        }

        public static ProfileDemoDirector Instance { get; private set; }

        public HouseTransition[] houses = new HouseTransition[Profiles.Count];
        public Volume[] volumes = new Volume[Profiles.Count];
        public DojoShell shell;
        public ProfileMorph morph;
        public HubPropsDissolve hubDissolve;
        public SessionGate sessionGate;
        public Renderer[] whiteSpaceRenderers;
        public GameObject[] whiteSpaceOnly;
        public ProceduralMusic music;
        public ProfileCaption caption;
        public ResultsBoard results;
        public DemoSessionRecorder recorder;
        public Rigidbody tableBody;
        public XRGrabInteractable tableGrab;
        public Light mainLight;
        public float transitionDuration = 2.6f;
        public Color whiteBackground = new Color(0.93f, 0.95f, 0.97f);

        public State Current { get; private set; } = State.WhiteSpace;
        public ProfileId Active { get; private set; }

        Camera cam;
        UniversalAdditionalCameraData camData;
        bool basePost;
        CameraClearFlags baseClear;
        Color baseBackground;
        AmbientMode baseAmbientMode;
        Color baseAmbient;
        bool baseFog;
        FogMode baseFogMode;
        Color baseFogColor;
        float baseFogDensity;
        Color baseLightColor;
        float baseLightIntensity;

        void Awake()
        {
            Instance = this;
            ZceaHubBootstrap.Ensure(this);
            if (shell != null)
                shell.HideAllLooksImmediate();
            else
            {
                foreach (var house in houses)
                {
                    if (house != null)
                        house.HideImmediate();
                }
            }

            foreach (var volume in volumes)
            {
                if (volume != null)
                    volume.weight = 0f;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (tableBody != null)
            {
                tableBody.isKinematic = true;
                tableBody.interpolation = RigidbodyInterpolation.None;
            }

            if (tableGrab != null)
                tableGrab.enabled = false;

            if (results != null)
                results.Refresh();
        }

        void CaptureBaseline()
        {
            cam = Camera.main;
            camData = cam != null ? cam.GetComponent<UniversalAdditionalCameraData>() : null;
            basePost = camData != null && camData.renderPostProcessing;
            if (cam != null)
            {
                baseClear = cam.clearFlags;
                baseBackground = cam.backgroundColor;
            }

            baseAmbientMode = RenderSettings.ambientMode;
            baseAmbient = RenderSettings.ambientLight;
            baseFog = RenderSettings.fog;
            baseFogMode = RenderSettings.fogMode;
            baseFogColor = RenderSettings.fogColor;
            baseFogDensity = RenderSettings.fogDensity;
            if (mainLight != null)
            {
                baseLightColor = mainLight.color;
                baseLightIntensity = mainLight.intensity;
            }
        }

        public bool EnterLookFromSession(ProfileId id)
        {
            return EnterProfile(id);
        }

        public bool EnterProfile(ProfileId id)
        {
            var look = ResolveLook(id);
            if (Current != State.WhiteSpace || look == null)
                return false;
            StartCoroutine(EnterRoutine(id));
            return true;
        }

        HouseTransition ResolveLook(ProfileId id)
        {
            if (shell != null)
            {
                var look = shell.GetLook(id);
                if (look != null && look.transition != null)
                    return look.transition;
            }

            var i = (int)id;
            if (houses != null && i >= 0 && i < houses.Length)
                return houses[i];
            return null;
        }

        public HouseTransition VisualHouse
        {
            get
            {
                if (Current == State.WhiteSpace || Current == State.Dissolving)
                    return null;
                return ResolveLook(Active);
            }
        }

        Coroutine visualSwitch;

        /// <summary>
        /// Magic morph into another dojo look. Visual only: GuestSession and the visit profile stay as they are.
        /// </summary>
        public bool SwitchVisualDojo(ProfileId id)
        {
            if (Current != State.InHouse || id == Active || visualSwitch != null)
                return false;
            visualSwitch = StartCoroutine(SwitchVisualRoutine(id));
            return true;
        }

        IEnumerator SwitchVisualRoutine(ProfileId next)
        {
            Current = State.Transforming;
            var from = ResolveLook(Active);
            var info = Profiles.Get(next);
            var to = ResolveLook(next);
            var origin = HeadPosition();
            if (from != null)
                yield return from.Dissolve(origin);
            Active = next;
            var whiteAmbient = new Color(0.85f, 0.86f, 0.88f);
            if (morph != null && to != null)
            {
                morph.duration = Mathf.Min(transitionDuration, 3f);
                yield return morph.MorphIn(info, to, (inf, u) => ApplyEnvironment(inf, u, whiteAmbient), null, origin);
            }
            else if (to != null)
            {
                yield return to.Build(origin);
                ApplyEnvironment(info, 1f, whiteAmbient);
            }

            if (sessionGate != null)
                sessionGate.EnsureVisible();
            if (to != null)
            {
                foreach (var pose in to.GetComponentsInChildren<ResettablePose>(true))
                    pose.ResetNow();
                HouseSpawn.Align(to.transform);
            }
            Current = State.InHouse;
            visualSwitch = null;
        }

        public bool ReturnToWhiteSpace()
        {
            if (Current != State.InHouse)
                return false;
            StartCoroutine(ReturnRoutine());
            return true;
        }

        IEnumerator EnterRoutine(ProfileId id)
        {
            Current = State.Transforming;
            Active = id;
            var info = Profiles.Get(id);
            CaptureBaseline();

            if (recorder != null)
                recorder.BeginProfile(id);
            if (music != null)
                music.Play(id);
            if (caption != null)
                caption.gameObject.SetActive(false);
            SetWhiteSpaceOnly(false);

            var house = ResolveLook(id);
            var origin = HeadPosition();
            SetPost(true);
            if (cam != null)
                cam.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;

            var whiteAmbient = new Color(0.85f, 0.86f, 0.88f);
            if (morph != null)
            {
                morph.duration = Mathf.Min(transitionDuration, 3f);
                yield return morph.MorphIn(info, house, (inf, u) =>
                {
                    ApplyEnvironment(inf, u, whiteAmbient);
                    if (u > 0.55f)
                        SetWhiteSpaceRenderers(false);
                }, hubDissolve, origin);
            }
            else
            {
                DemoSfx.Play(DemoSfx.Kind.Whoosh, origin, 0.35f, 1f);
                var build = StartCoroutine(house.Build(origin));
                var t = 0f;
                while (t < transitionDuration)
                {
                    t += Time.deltaTime;
                    var u = Mathf.SmoothStep(0f, 1f, t / transitionDuration);
                    ApplyEnvironment(info, u, whiteAmbient);
                    if (u > 0.55f)
                        SetWhiteSpaceRenderers(false);
                    yield return null;
                }

                yield return build;
            }

            ApplyEnvironment(info, 1f, whiteAmbient);
            SetWhiteSpaceRenderers(false);
            if (sessionGate != null)
                sessionGate.EnsureVisible();
            foreach (var activity in house.GetComponentsInChildren<ProfileActivity>())
                activity.ResetActivity();
            foreach (var pose in house.GetComponentsInChildren<ResettablePose>())
                pose.ResetNow();
            HouseSpawn.Align(house.transform);
            Current = State.InHouse;
        }

        IEnumerator ReturnRoutine()
        {
            Current = State.Dissolving;
            var id = Active;
            var info = Profiles.Get(id);
            var house = ResolveLook(id);
            if (house == null)
            {
                Current = State.WhiteSpace;
                yield break;
            }

            foreach (var activity in house.GetComponentsInChildren<ProfileActivity>())
                activity.OnHouseExit();
            foreach (var pose in ResettablePose.OwnedBy(house.transform))
                pose.ResetNow();
            if (recorder != null)
            {
                recorder.CaptureView(id, "vista_final");
                yield return null;
                recorder.EndProfile();
            }

            if (music != null)
                music.Stop();
            if (caption != null)
                caption.gameObject.SetActive(false);

            var origin = HeadPosition();
            SetWhiteSpaceRenderers(true);
            var whiteAmbient = new Color(0.85f, 0.86f, 0.88f);
            if (morph != null)
            {
                morph.duration = Mathf.Min(transitionDuration, 3f);
                yield return morph.MorphOut(info, house, (inf, u) => ApplyEnvironment(inf, u, whiteAmbient), hubDissolve, origin);
            }
            else
            {
                DemoSfx.Play(DemoSfx.Kind.Whoosh, origin, 0.8f, 0.8f);
                var dissolve = StartCoroutine(house.Dissolve(origin));
                var t = 0f;
                while (t < transitionDuration)
                {
                    t += Time.deltaTime;
                    ApplyEnvironment(info, 1f - Mathf.SmoothStep(0f, 1f, t / transitionDuration), whiteAmbient);
                    yield return null;
                }

                yield return dissolve;
                if (hubDissolve != null)
                    yield return hubDissolve.Appear();
            }

            RestoreBaseline();
            SetWhiteSpaceOnly(true);
            if (sessionGate != null)
                sessionGate.EnsureVisible();
            if (results != null)
                results.Refresh();
            Current = State.WhiteSpace;
        }

        void ApplyEnvironment(ProfileInfo info, float u, Color whiteAmbient)
        {
            if (cam != null)
                cam.backgroundColor = Color.Lerp(whiteBackground, info.Fog, u);
            RenderSettings.ambientLight = Color.Lerp(whiteAmbient, info.Ambient, u);
            RenderSettings.fogColor = Color.Lerp(whiteBackground, info.Fog, u);
            RenderSettings.fogDensity = Mathf.Lerp(0f, info.FogDensity, u);
            if (mainLight != null)
            {
                mainLight.color = Color.Lerp(baseLightColor, info.LightColor, u);
                mainLight.intensity = Mathf.Lerp(baseLightIntensity, info.LightIntensity, u);
            }

            var volume = volumes[(int)info.Id];
            if (volume != null)
                volume.weight = u;
        }

        void RestoreBaseline()
        {
            if (cam != null)
            {
                cam.clearFlags = baseClear;
                cam.backgroundColor = baseBackground;
            }

            RenderSettings.ambientMode = baseAmbientMode;
            RenderSettings.ambientLight = baseAmbient;
            RenderSettings.fog = baseFog;
            RenderSettings.fogMode = baseFogMode;
            RenderSettings.fogColor = baseFogColor;
            RenderSettings.fogDensity = baseFogDensity;
            if (mainLight != null)
            {
                mainLight.color = baseLightColor;
                mainLight.intensity = baseLightIntensity;
            }

            foreach (var volume in volumes)
            {
                if (volume != null)
                    volume.weight = 0f;
            }

            SetPost(basePost);
        }

        void SetPost(bool enabled)
        {
            if (camData != null)
                camData.renderPostProcessing = enabled;
        }

        void SetWhiteSpaceRenderers(bool visible)
        {
            if (whiteSpaceRenderers == null)
                return;
            foreach (var r in whiteSpaceRenderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }

        void SetWhiteSpaceOnly(bool visible)
        {
            if (whiteSpaceOnly == null)
                return;
            foreach (var go in whiteSpaceOnly)
            {
                if (go != null)
                    go.SetActive(visible);
            }
        }

        Vector3 HeadPosition()
        {
            var main = Camera.main;
            return main != null ? main.transform.position : transform.position + Vector3.up * 1.6f;
        }

        void Update()
        {
#if UNITY_EDITOR
            var kb = Keyboard.current;
            if (kb == null)
                return;
            // QA only — not a product house picker. Device builds compile this out.
            if (kb.digit1Key.wasPressedThisFrame)
                EnterProfile(ProfileId.Zen);
            else if (kb.digit2Key.wasPressedThisFrame)
                EnterProfile(ProfileId.Cyber);
            else if (kb.digit3Key.wasPressedThisFrame)
                EnterProfile(ProfileId.Excentrico);
            else if (kb.digit4Key.wasPressedThisFrame)
                EnterProfile(ProfileId.Acolhedor);
            else if (kb.digit0Key.wasPressedThisFrame)
                ReturnToWhiteSpace();
#endif
        }
    }
}
