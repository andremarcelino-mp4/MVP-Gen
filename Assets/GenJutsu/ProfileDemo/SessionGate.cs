using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Welcome card. Polls the admin-selected session; the visitor never types a code.
    /// A new visit is a new session id/code, even when the profile letter stays the same.
    /// When the admin ends the visit, <see cref="SessionEnded"/> fires and polling continues.
    /// </summary>
    public class SessionGate : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text status;
        public SessionApiClient client;
        public CanvasGroup group;
        public float pollSeconds = 2f;

        /// <summary>Shot rig and tests skip the network and push JSON through <see cref="Ingest"/>.</summary>
        public bool suppressNetworkPoll;

        [HideInInspector] public TMP_InputField codeField;
        [HideInInspector] public Button submitButton;

        public event Action<ProfileId> ProfileResolved;
        public event Action SessionEnded;
        public event Action<string> NamedWelcome;

        public string VisitorName { get; private set; }

        public bool HasSession { get; private set; }
        public string ActiveSessionKey { get; private set; }
        public ProfileId ActiveProfile { get; private set; }

        bool applied;
        string appliedKey;
        string shownWelcome;
        Coroutine poll;

        void Awake()
        {
            GuestSession.Load();
            StripCodeControls();
            CompactCard();
            ShowWaiting();
#if UNITY_EDITOR
            if (System.IO.File.Exists("Temp/dojo-shots.marker"))
                suppressNetworkPoll = true;
#endif
        }

        void OnEnable()
        {
            if (poll == null)
                poll = StartCoroutine(PollRoutine());
        }

        void OnDisable()
        {
            if (poll != null)
            {
                StopCoroutine(poll);
                poll = null;
            }
        }

        /// <summary>Keeps the welcome card on screen. The dojo does not replace it with a profile panel.</summary>
        public void HideAfterMorph()
        {
            EnsureVisible();
        }

        public void ShowOnWhite()
        {
            EnsureVisible();
        }

        public void EnsureVisible()
        {
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            if (!applied)
                ShowWaiting();
            if (isActiveAndEnabled && poll == null)
                poll = StartCoroutine(PollRoutine());
        }

        /// <summary>Same path the poll uses. httpSuccess false is a transport failure.</summary>
        public void Ingest(string body, long httpStatus, bool httpSuccess)
        {
            ApplyResult(SessionApiClient.Parse(body, httpStatus, httpSuccess));
        }

        IEnumerator PollRoutine()
        {
#if UNITY_EDITOR
            if (useEditorMock)
            {
                var mockName = string.IsNullOrWhiteSpace(editorMockName) ? "Visitante" : editorMockName.Trim();
                GuestSession.Save("EDITOR", mockName, editorMockProfile);
                ShowWelcome(mockName);
                Notify(editorMockProfile, "EDITOR");
                yield break;
            }
#endif
            while (enabled)
            {
                if (suppressNetworkPoll)
                {
                    yield return new WaitForSecondsRealtime(Mathf.Max(1f, pollSeconds));
                    continue;
                }

                if (client == null)
                {
                    if (!applied)
                        SetStatus("Cliente de sessão em falta.", new Color(0.7f, 0.2f, 0.2f));
                    yield return new WaitForSecondsRealtime(pollSeconds);
                    continue;
                }

                SessionApiClient.Result result = default;
                yield return client.FetchActive(r => result = r);
                ApplyResult(result);
                yield return new WaitForSecondsRealtime(Mathf.Max(1f, pollSeconds));
            }
        }

        void ApplyResult(SessionApiClient.Result result)
        {
            switch (result.kind)
            {
                case SessionApiClient.Kind.WaitingForAdmin:
                case SessionApiClient.Kind.Ended:
                    if (applied)
                        ClearSession();
                    else
                        ShowWaiting();
                    return;
                case SessionApiClient.Kind.Down:
                    if (!applied)
                        SetStatus(result.error, new Color(0.65f, 0.22f, 0.22f));
                    return;
                case SessionApiClient.Kind.Incomplete:
                    if (applied && !SameVisit(result.sessionKey))
                        ClearSession();
                    GuestSession.Save(result.sessionCode, result.displayName, null);
                    ShowWelcome(result.displayName);
                    return;
                case SessionApiClient.Kind.Ready:
                    GuestSession.Save(result.sessionCode, result.displayName, result.profile);
                    ShowWelcome(result.displayName);
                    Notify(result.profile, result.sessionKey);
                    return;
            }
        }

        bool SameVisit(string key)
        {
            return !string.IsNullOrEmpty(appliedKey) && appliedKey == key;
        }

        void Notify(ProfileId id, string key)
        {
            if (string.IsNullOrEmpty(key))
                key = id.ToString();
            if (applied && appliedKey == key)
                return;
            applied = true;
            appliedKey = key;
            HasSession = true;
            ActiveSessionKey = key;
            ActiveProfile = id;
            ProfileResolved?.Invoke(id);
        }

        void ClearSession()
        {
            applied = false;
            appliedKey = null;
            HasSession = false;
            ActiveSessionKey = null;
            ShowWaiting();
            SessionEnded?.Invoke();
        }

        public static string TitleFor(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return "Bem-vindo";
            return "Bem-vindo, " + rawName.Trim();
        }

        void ShowWaiting()
        {
            VisitorName = "";
            shownWelcome = null;
            SetTitle(TitleFor(null));
            HideStatus();
        }

        void ShowWelcome(string rawName)
        {
            var name = string.IsNullOrWhiteSpace(rawName) ? "visitante" : rawName.Trim();
            VisitorName = name;
            SetTitle(TitleFor(name));
            HideStatus();
            if (shownWelcome == name)
                return;
            shownWelcome = name;
            NamedWelcome?.Invoke(name);
        }

        void CompactCard()
        {
            var rect = transform as RectTransform;
            if (rect != null && rect.sizeDelta.y > 160f)
            {
                rect.sizeDelta = new Vector2(720f, 140f);
                transform.localPosition += Vector3.up * 0.22f;
            }
        }

        void HideStatus()
        {
            if (status == null)
                return;
            status.text = "";
            status.gameObject.SetActive(false);
        }

        void SetTitle(string text)
        {
            if (title != null)
                title.text = text;
        }

        void SetStatus(string text, Color color)
        {
            if (status == null)
                return;
            status.gameObject.SetActive(true);
            status.text = text;
            status.color = color;
        }

        void StripCodeControls()
        {
            if (codeField != null)
                codeField.gameObject.SetActive(false);
            if (submitButton != null)
                submitButton.gameObject.SetActive(false);
            HideChild("Code");
            HideChild("Submit");
        }

        void HideChild(string name)
        {
            var t = transform.Find(name);
            if (t != null)
                t.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        [Header("QA Editor only — never invent a look in a device build")]
        public bool useEditorMock;
        public string editorMockName = "André";
        public ProfileId editorMockProfile = ProfileId.Zen;
#endif
    }
}
