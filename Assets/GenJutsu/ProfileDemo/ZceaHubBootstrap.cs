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
            var parent = director.transform.parent != null ? director.transform.parent : director.transform;
            director.sessionGate = DedupAndEnsureGate(director.sessionGate, parent, api);

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

        static SessionGate DedupAndEnsureGate(SessionGate preferred, Transform parent, SessionApiClient api)
        {
            var all = UnityEngine.Object.FindObjectsByType<SessionGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SessionGate keep = preferred;
            if (keep == null)
            {
                foreach (var g in all)
                {
                    if (g != null)
                    {
                        keep = g;
                        break;
                    }
                }
            }

            foreach (var g in all)
            {
                if (g == null || g == keep)
                    continue;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(g.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(g.gameObject);
            }

            if (keep == null)
                keep = BuildWelcomeCard(parent, api);
            else
            {
                keep.client = api;
                keep.gameObject.SetActive(true);
                if (keep.title == null)
                {
                    var t = keep.transform.Find("Title");
                    if (t != null)
                        keep.title = t.GetComponent<TMP_Text>();
                }
                if (keep.status == null)
                {
                    var s = keep.transform.Find("Status");
                    if (s != null)
                        keep.status = s.GetComponent<TMP_Text>();
                }
            }

            return keep;
        }

        static SessionGate BuildWelcomeCard(Transform parent, SessionApiClient api)
        {
            var canvasGo = new GameObject("SessionGate", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.15f, 0.55f);
            canvasGo.transform.localScale = Vector3.one * 0.00115f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = new Vector2(920f, 360f);

            var group = canvasGo.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            FullImage(rect, "Background", new Color(0.97f, 0.97f, 0.98f, 0.94f));
            var title = Tmp(rect, "Title", "Bem-vindo", 48f, new Color(0.16f, 0.18f, 0.22f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -100f), new Vector2(-36f, -24f));
            title.fontStyle = FontStyles.Bold;
            var status = Tmp(rect, "Status", "À espera que o anfitrião escolha a sessão no admin.",
                28f, new Color(0.35f, 0.38f, 0.44f),
                new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(36f, 28f), new Vector2(-36f, -8f));
            status.textWrappingMode = TextWrappingModes.Normal;

            var gate = canvasGo.AddComponent<SessionGate>();
            gate.title = title;
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
