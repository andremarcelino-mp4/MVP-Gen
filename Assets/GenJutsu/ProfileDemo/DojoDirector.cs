using System.Collections;
using System.IO;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Product loop: white hub (mesa + pergaminho)  SessionGate  morph. No house picker.
    /// COMEÇAR sempre entra: perfil da sessão ativa; se ainda não chegou, espera um pouco
    /// e usa o último perfil salvo no óculos; sem nada disso, entra no Zen.
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

        /// <summary>Quanto o COMEÇAR espera o /api/session responder antes do plano B.</summary>
        public float sessionWaitSeconds = 3f;
        public ProfileId fallbackProfile = ProfileId.Zen;

        /// <summary>Origem do último COMEÇAR: sessao, cache ou padrao.</summary>
        public string LastStartSource { get; private set; } = "";

        const string QaStartFile = "qa-comecar";
        const string QaWhiteFile = "qa-branco";

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
        bool enteredWithoutSession;
        Coroutine waitingStart;
        float nextQaPoll;

        void OnDisable()
        {
            if (gate != null)
            {
                gate.ProfileResolved -= OnProfile;
                gate.SessionEnded -= OnSessionEnded;
            }

            waitingStart = null;
        }

        public void Bind()
        {
            if (gate == null)
                return;
            gate.ProfileResolved -= OnProfile;
            gate.ProfileResolved += OnProfile;
            gate.SessionEnded -= OnSessionEnded;
            gate.SessionEnded += OnSessionEnded;
            if (!readyToStart && gate.HasSession)
            {
                pendingId = gate.ActiveProfile;
                readyToStart = true;
            }
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

            PollQaTrigger();
        }

        /// <summary>The visit is known, but the morph waits for the table plaque.</summary>
        public bool ReadyToStart => readyToStart;

        public bool TryBeginVisit()
        {
            if (demo == null)
                return false;
            if (demo.Current != ProfileDemoDirector.State.WhiteSpace)
                return false;
            // Já estamos no branco: um "voltar depois de entrar" pendente não vale mais.
            leaveAfterEnter = false;

            if (readyToStart)
                return Enter(pendingId, "sessao");
            if (gate != null && gate.HasSession)
                return Enter(gate.ActiveProfile, "sessao");
            if (waitingStart != null)
                return true;
            if (!isActiveAndEnabled || sessionWaitSeconds <= 0f)
                return EnterWithoutSession();

            Gen.GenVoiceLog.Info("comecar: aguardando sessao ate " + sessionWaitSeconds.ToString("0.0") + "s");
            waitingStart = StartCoroutine(WaitForSessionThenEnter());
            return true;
        }

        IEnumerator WaitForSessionThenEnter()
        {
            var t = 0f;
            while (t < sessionWaitSeconds && !readyToStart)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            waitingStart = null;
            if (demo == null || demo.Current != ProfileDemoDirector.State.WhiteSpace)
                yield break;
            if (readyToStart)
                Enter(pendingId, "sessao");
            else
                EnterWithoutSession();
        }

        bool EnterWithoutSession()
        {
            if (GuestSession.HasProfile)
                return Enter(GuestSession.Profile.Value, "cache");
            return Enter(fallbackProfile, "padrao");
        }

        bool Enter(ProfileId id, string source)
        {
            var ok = demo.EnterLookFromSession(id);
            if (ok)
            {
                LastStartSource = source;
                enteredWithoutSession = source != "sessao";
            }

            Gen.GenVoiceLog.Info("comecar: perfil=" + id + " fonte=" + source + " ok=" + ok);
            return ok;
        }

        void OnProfile(ProfileId id)
        {
            pendingId = id;
            readyToStart = true;
            if (gaze != null && GuestSession.HasProfile)
                gaze.StartSession(GuestSession.Code, id);
            if (demo == null || demo.Current == ProfileDemoDirector.State.WhiteSpace)
                return;
            // Entrou pelo plano B e a sessão confirmou a mesma casa: fica onde está.
            if (enteredWithoutSession && demo.Active == id)
            {
                enteredWithoutSession = false;
                LastStartSource = "sessao";
                return;
            }

            RequestWhite();
        }

        void OnSessionEnded()
        {
            if (gaze != null)
                gaze.StopSession();
            readyToStart = false;
            enteredWithoutSession = false;
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

        /// <summary>
        /// QA pelo cabo: um arquivo vazio em files/qa-comecar faz o mesmo que tocar no COMEÇAR,
        /// files/qa-branco volta para o branco. O arquivo é apagado na hora.
        /// </summary>
        void PollQaTrigger()
        {
            if (Time.unscaledTime < nextQaPoll)
                return;
            nextQaPoll = Time.unscaledTime + 1f;
            try
            {
                var dir = Application.persistentDataPath;
                var start = Path.Combine(dir, QaStartFile);
                if (File.Exists(start))
                {
                    File.Delete(start);
                    Gen.GenVoiceLog.Info("comecar: gatilho QA estado=" + demo.Current + " pronto=" + readyToStart);
                    var ok = TryBeginVisit();
                    Gen.GenVoiceLog.Info("comecar: gatilho QA resultado=" + ok);
                }

                var white = Path.Combine(dir, QaWhiteFile);
                if (File.Exists(white))
                {
                    File.Delete(white);
                    Gen.GenVoiceLog.Info("comecar: gatilho QA branco");
                    demo.ReturnToWhiteSpace();
                }
            }
            catch (System.Exception e)
            {
                Gen.GenVoiceLog.Warn("comecar: gatilho QA falhou " + e.Message);
            }
        }
    }
}
