using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Welcome card. Polls the admin-selected session; the visitor never types a code.
    /// </summary>
    public class SessionGate : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text status;
        public SessionApiClient client;
        public CanvasGroup group;
        public float pollSeconds = 2f;

        [HideInInspector] public TMP_InputField codeField;
        [HideInInspector] public Button submitButton;

        public event Action<ProfileId> ProfileResolved;

        bool applied;
        ProfileId appliedId;
        Coroutine poll;

        void Awake()
        {
            GuestSession.Load();
            StripCodeControls();
            ShowWaiting();
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

        public void HideAfterMorph()
        {
            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        public void ShowOnWhite()
        {
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            applied = false;
            ShowWaiting();
            if (isActiveAndEnabled && poll == null)
                poll = StartCoroutine(PollRoutine());
        }

        IEnumerator PollRoutine()
        {
#if UNITY_EDITOR
            if (useEditorMock)
            {
                var mockName = string.IsNullOrWhiteSpace(editorMockName) ? "Visitante" : editorMockName.Trim();
                GuestSession.Save("EDITOR", mockName, editorMockProfile);
                ShowWelcome(mockName, Profiles.Get(editorMockProfile).Title);
                Notify(editorMockProfile);
                yield break;
            }
#endif
            while (enabled)
            {
                if (client == null)
                {
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
                    if (!applied)
                        ShowWaiting();
                    return;
                case SessionApiClient.Kind.Down:
                    if (!applied)
                        SetStatus(result.error, new Color(0.65f, 0.22f, 0.22f));
                    return;
                case SessionApiClient.Kind.Incomplete:
                    GuestSession.Save(result.sessionCode, result.displayName, null);
                    ShowWelcome(result.displayName, null);
                    SetStatus("A casa ainda não tem perfil ZCEA. Permaneça no branco.", new Color(0.45f, 0.38f, 0.22f));
                    return;
                case SessionApiClient.Kind.Ready:
                    GuestSession.Save(result.sessionCode, result.displayName, result.profile);
                    ShowWelcome(result.displayName, Profiles.Get(result.profile).Title);
                    Notify(result.profile);
                    return;
            }
        }

        void Notify(ProfileId id)
        {
            if (applied && appliedId == id)
                return;
            applied = true;
            appliedId = id;
            ProfileResolved?.Invoke(id);
        }

        void ShowWaiting()
        {
            SetTitle("Bem-vindo");
            SetStatus("À espera que o anfitrião escolha a sessão no admin. Você não escolhe a casa.",
                new Color(0.28f, 0.32f, 0.38f));
        }

        void ShowWelcome(string rawName, string lookTitle)
        {
            var name = string.IsNullOrWhiteSpace(rawName) ? "visitante" : rawName.Trim();
            SetTitle("Bem-vindo, " + name);
            if (!string.IsNullOrEmpty(lookTitle))
                SetStatus("Olá, " + name + ". A casa assume o look " + lookTitle + ".", new Color(0.28f, 0.32f, 0.38f));
            else
                SetStatus("Olá, " + name + ".", new Color(0.28f, 0.32f, 0.38f));
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
