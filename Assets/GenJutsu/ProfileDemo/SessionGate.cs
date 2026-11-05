using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// World-space code entry. Valid profile_id starts the morph; invalid/API down stays white.
    /// </summary>
    public class SessionGate : MonoBehaviour
    {
        public TMP_InputField codeField;
        public Button submitButton;
        public TMP_Text status;
        public SessionApiClient client;
        public CanvasGroup group;

        public event Action<ProfileId> ProfileResolved;

        bool busy;

        void Awake()
        {
            GuestSession.Load();
            if (submitButton != null)
                submitButton.onClick.AddListener(Submit);
            if (codeField != null && !string.IsNullOrEmpty(GuestSession.Code))
                codeField.text = GuestSession.Code;
            SetStatus("Digite o código do site para entrar no dojo. O ambiente escolhe o look — você não escolhe a casa.", new Color(0.28f, 0.32f, 0.38f));
        }

        void OnDestroy()
        {
            if (submitButton != null)
                submitButton.onClick.RemoveListener(Submit);
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.enterKey.wasPressedThisFrame)
                Submit();
        }

        public void Submit()
        {
            if (busy)
                return;
            StartCoroutine(SubmitRoutine());
        }

        IEnumerator SubmitRoutine()
        {
            busy = true;
            SetInteractable(false);
            var code = codeField != null ? codeField.text : "";
            SetStatus("A consultar a sessão…", new Color(0.35f, 0.4f, 0.48f));

#if UNITY_EDITOR
            if (useEditorMock)
            {
                GuestSession.Save(code.Trim(), editorMockProfile);
                SetStatus("Mock de Editor: look " + editorMockProfile + ".", Profiles.Get(editorMockProfile).Accent);
                ProfileResolved?.Invoke(editorMockProfile);
                busy = false;
                yield break;
            }
#endif

            if (client == null)
            {
                SetStatus("Cliente de sessão em falta.", new Color(0.7f, 0.2f, 0.2f));
                busy = false;
                SetInteractable(true);
                yield break;
            }

            SessionApiClient.Result result = default;
            yield return client.Fetch(code, r => result = r);
            if (!result.ok)
            {
                DemoSfx.Play(DemoSfx.Kind.Error, transform.position, 0.35f, 0.9f);
                SetStatus(result.error, new Color(0.65f, 0.22f, 0.22f));
                busy = false;
                SetInteractable(true);
                yield break;
            }

            GuestSession.Save(code.Trim(), result.profile);
            SetStatus("Sessão " + Profiles.Get(result.profile).Title + ". O espaço muda consigo.", Profiles.Get(result.profile).Accent);
            ProfileResolved?.Invoke(result.profile);
            busy = false;
        }

        public void HideAfterMorph()
        {
            if (group != null)
                group.interactable = false;
            gameObject.SetActive(false);
        }

        public void ShowOnWhite()
        {
            gameObject.SetActive(true);
            if (group != null)
                group.alpha = 1f;
            SetInteractable(true);
            busy = false;
        }

        void SetInteractable(bool on)
        {
            if (group != null)
            {
                group.interactable = on;
                group.blocksRaycasts = on;
            }
            if (submitButton != null)
                submitButton.interactable = on;
            if (codeField != null)
                codeField.interactable = on;
        }

        void SetStatus(string text, Color color)
        {
            if (status == null)
                return;
            status.text = text;
            status.color = color;
        }

#if UNITY_EDITOR
        [Header("QA Editor only — never invent a look in a device build")]
        public bool useEditorMock;
        public ProfileId editorMockProfile = ProfileId.Zen;
#endif
    }
}
