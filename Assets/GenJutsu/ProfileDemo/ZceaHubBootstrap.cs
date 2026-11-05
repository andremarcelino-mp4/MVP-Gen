using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Wires SessionGate / morph / hub dissolve on the existing SampleScene without a rebuild.
    /// </summary>
    public static class ZceaHubBootstrap
    {
        public static void Ensure(ProfileDemoDirector director)
        {
            if (director == null)
                return;

            director.gameObject.SetActive(true);
            var root = director.transform.root;
            if (root.name == "ProfileDemo")
                root.gameObject.SetActive(true);

            DisableHousePicker(root);
            EnsureHubVisible();

            if (director.morph == null)
                director.morph = director.GetComponent<ProfileMorph>() ?? director.gameObject.AddComponent<ProfileMorph>();
            director.morph.duration = Mathf.Min(director.transitionDuration, 2.6f);

            if (director.shell == null)
                director.shell = EnsureShell(director);

            if (director.hubDissolve == null)
                director.hubDissolve = director.GetComponent<HubPropsDissolve>() ?? director.gameObject.AddComponent<HubPropsDissolve>();
            if (director.hubDissolve.roots == null || director.hubDissolve.roots.Length == 0)
                director.hubDissolve.roots = CollectHubRoots();
            director.hubDissolve.duration = 1.35f;

            var api = director.GetComponent<SessionApiClient>() ?? director.gameObject.AddComponent<SessionApiClient>();
            if (director.sessionGate == null)
                director.sessionGate = BuildGate(director.transform.parent != null ? director.transform.parent : director.transform, api);

            var gaze = director.GetComponent<GazePublisher>() ?? director.gameObject.AddComponent<GazePublisher>();
            var dojo = director.GetComponent<DojoDirector>() ?? director.gameObject.AddComponent<DojoDirector>();
            dojo.demo = director;
            dojo.gate = director.sessionGate;
            dojo.shell = director.shell;
            dojo.morph = director.morph;
            dojo.hubDissolve = director.hubDissolve;
            dojo.gaze = gaze;
            dojo.Bind();
        }

        static void DisableHousePicker(Transform profileRoot)
        {
            foreach (var token in Object.FindObjectsByType<ProfileToken>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                token.opensHouse = false;
                var grab = token.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.enabled = false;
            }

            if (profileRoot == null)
                return;
            var tokens = profileRoot.Find("TableTokens");
            if (tokens != null)
                tokens.gameObject.SetActive(false);
        }

        static void EnsureHubVisible()
        {
            var table = GameObject.Find("JapaneseTableSet");
            if (table != null)
                table.SetActive(true);
            var scroll = GameObject.Find("Scroll");
            if (scroll != null)
                scroll.SetActive(true);
        }

        static DojoShell EnsureShell(ProfileDemoDirector director)
        {
            Transform houses = null;
            var parent = director.transform.parent;
            if (parent != null)
                houses = parent.Find("Houses") ?? parent.Find("DojoShell");
            if (houses == null)
            {
                var go = new GameObject("DojoShell");
                go.transform.SetParent(parent != null ? parent : director.transform, false);
                houses = go.transform;
            }

            var shell = houses.GetComponent<DojoShell>() ?? houses.gameObject.AddComponent<DojoShell>();
            var looks = new ProfileLook[Profiles.Count];
            if (director.houses != null)
            {
                for (var i = 0; i < director.houses.Length && i < looks.Length; i++)
                {
                    var house = director.houses[i];
                    if (house == null)
                        continue;
                    house.comfortEase = true;
                    var look = house.GetComponent<ProfileLook>() ?? house.gameObject.AddComponent<ProfileLook>();
                    look.profile = (ProfileId)i;
                    look.transition = house;
                    if (director.volumes != null && i < director.volumes.Length)
                        look.volume = director.volumes[i];
                    looks[i] = look;
                }
            }

            shell.looks = looks;
            return shell;
        }

        static GameObject[] CollectHubRoots()
        {
            var list = new System.Collections.Generic.List<GameObject>();
            var table = GameObject.Find("JapaneseTableSet");
            if (table != null)
                list.Add(table);
            var scroll = GameObject.Find("Scroll");
            if (scroll != null && (table == null || !scroll.transform.IsChildOf(table.transform)))
                list.Add(scroll);
            var hanging = GameObject.Find("HangingScroll");
            if (hanging != null)
                list.Add(hanging);
            return list.ToArray();
        }

        static SessionGate BuildGate(Transform parent, SessionApiClient api)
        {
            var existing = parent.GetComponentInChildren<SessionGate>(true);
            if (existing != null)
            {
                existing.client = api;
                existing.gameObject.SetActive(true);
                return existing;
            }

            var canvasGo = new GameObject("SessionGate", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.15f, 0.55f);
            canvasGo.transform.localScale = Vector3.one * 0.00115f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = new Vector2(920f, 520f);

            var group = canvasGo.AddComponent<CanvasGroup>();
            FullImage(rect, "Background", new Color(0.97f, 0.97f, 0.98f, 0.94f));
            var title = Tmp(rect, "Title", "Código da sessão", 48f, new Color(0.16f, 0.18f, 0.22f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -88f), new Vector2(-36f, -20f));
            title.fontStyle = FontStyles.Bold;
            var hint = Tmp(rect, "Hint", "O questionário no site escolhe o look. Aqui só entra o código — sem escolher casa.",
                26f, new Color(0.4f, 0.43f, 0.48f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -150f), new Vector2(-36f, -92f));
            hint.textWrappingMode = TextWrappingModes.Normal;

            var inputGo = new GameObject("Code", typeof(RectTransform));
            inputGo.transform.SetParent(rect, false);
            var inputRect = (RectTransform)inputGo.transform;
            Stretch(inputRect, new Vector2(0f, 0.38f), new Vector2(1f, 0.52f), new Vector2(36f, 0f), new Vector2(-36f, 0f));
            inputGo.AddComponent<Image>().color = new Color(0.92f, 0.93f, 0.95f, 1f);
            var field = inputGo.AddComponent<TMP_InputField>();
            var inputText = Tmp(inputRect, "Text", "", 34f, new Color(0.12f, 0.14f, 0.18f),
                Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            inputText.raycastTarget = true;
            var placeholder = Tmp(inputRect, "Placeholder", "ABCD-1234", 34f, new Color(0.55f, 0.58f, 0.62f),
                Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            field.textViewport = inputRect;
            field.textComponent = inputText;
            field.placeholder = placeholder;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 24;

            var buttonGo = new GameObject("Submit", typeof(RectTransform));
            buttonGo.transform.SetParent(rect, false);
            var buttonRect = (RectTransform)buttonGo.transform;
            Stretch(buttonRect, new Vector2(0f, 0.18f), new Vector2(1f, 0.34f), new Vector2(36f, 0f), new Vector2(-36f, 0f));
            buttonGo.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f);
            var button = buttonGo.AddComponent<Button>();
            Tmp(buttonRect, "Label", "Entrar no dojo", 32f, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var status = Tmp(rect, "Status", "", 24f, new Color(0.35f, 0.38f, 0.44f),
                new Vector2(0f, 0f), new Vector2(1f, 0.18f), new Vector2(36f, 12f), new Vector2(-36f, 0f));
            status.textWrappingMode = TextWrappingModes.Normal;

            var gate = canvasGo.AddComponent<SessionGate>();
            gate.codeField = field;
            gate.submitButton = button;
            gate.status = status;
            gate.group = group;
            gate.client = api;
            return gate;
        }

        static Image FullImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            Stretch(r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Tmp(RectTransform parent, string name, string text, float size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            Stretch(r, anchorMin, anchorMax, offsetMin, offsetMax);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
        }

        static void Stretch(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;
        }
    }
}
