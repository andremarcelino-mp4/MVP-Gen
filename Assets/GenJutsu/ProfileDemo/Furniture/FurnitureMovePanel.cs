using GenJutsu.GenCore;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Poke buttons for the same demo phrases, so the expo works without a mic.</summary>
    public sealed class FurnitureMovePanel : MonoBehaviour
    {
        FurnitureMoveHost host;
        CanvasGroup group;
        Transform column;
        string boundKey;
        System.Action<string> onReply;

        public static FurnitureMovePanel Ensure(FurnitureMoveHost moveHost, Transform parent, System.Action<string> speak)
        {
            if (moveHost == null)
                return null;
            var existing = moveHost.GetComponentInChildren<FurnitureMovePanel>(true);
            if (existing != null)
            {
                existing.host = moveHost;
                existing.onReply = speak;
                return existing;
            }

            var go = new GameObject("FurnitureMovePanel");
            go.transform.SetParent(parent != null ? parent : moveHost.transform, false);
            var panel = go.AddComponent<FurnitureMovePanel>();
            panel.host = moveHost;
            panel.onReply = speak;
            panel.Build();
            return panel;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(520f, 360f);
            transform.localScale = Vector3.one * 0.001f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var bg = new GameObject("Bg", typeof(RectTransform));
            bg.transform.SetParent(transform, false);
            var bgRect = (RectTransform)bg.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var image = bg.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.05f, 0.05f, 0.06f, 0.82f);
            image.raycastTarget = false;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(transform, false);
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = new Vector2(0f, 0.82f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-12f, -8f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "Pedidos da casa";
            title.fontSize = 28f;
            title.color = Color.white;
            title.raycastTarget = false;

            var col = new GameObject("Cmds", typeof(RectTransform));
            col.transform.SetParent(transform, false);
            column = col.transform;
            var colRect = (RectTransform)col.transform;
            colRect.anchorMin = new Vector2(0.04f, 0.04f);
            colRect.anchorMax = new Vector2(0.96f, 0.8f);
            colRect.offsetMin = Vector2.zero;
            colRect.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            var inHouse = host != null && host.director != null &&
                          host.director.Current == ProfileDemoDirector.State.InHouse;
            group.alpha = Mathf.MoveTowards(group.alpha, inHouse ? 1f : 0f, Time.deltaTime * 3f);
            group.interactable = inHouse;
            group.blocksRaycasts = inHouse;
            if (!inHouse)
                return;

            var cam = Camera.main;
            if (cam != null)
            {
                var pos = cam.transform.position + cam.transform.forward * 0.85f + cam.transform.right * 0.38f +
                          Vector3.up * -0.18f;
                transform.position = Vector3.Lerp(transform.position, pos, 0.18f);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up), 0.18f);
            }

            var catalog = host.ActiveCatalog();
            var key = catalog != null ? catalog.HouseKey : "";
            if (key == boundKey)
                return;
            boundKey = key;
            RebuildButtons(catalog);
        }

        void RebuildButtons(FurnitureHouseCatalog catalog)
        {
            for (var i = column.childCount - 1; i >= 0; i--)
                Destroy(column.GetChild(i).gameObject);
            if (catalog == null)
                return;
            var cmds = catalog.Commands;
            for (var i = 0; i < cmds.Length; i++)
            {
                var cmd = cmds[i];
                var row = new GameObject("Cmd" + i, typeof(RectTransform));
                row.transform.SetParent(column, false);
                var rect = (RectTransform)row.transform;
                var y0 = 1f - (i + 1) * 0.24f;
                rect.anchorMin = new Vector2(0f, y0);
                rect.anchorMax = new Vector2(1f, y0 + 0.22f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var img = row.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.75f, 0.12f, 0.12f, 0.9f);
                var box = row.AddComponent<BoxCollider>();
                box.size = new Vector3(480f, 70f, 8f);
                var label = new GameObject("Label", typeof(RectTransform));
                label.transform.SetParent(row.transform, false);
                var lr = (RectTransform)label.transform;
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = new Vector2(8f, 4f);
                lr.offsetMax = new Vector2(-8f, -4f);
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = ShortLabel(cmd.Phrase);
                tmp.fontSize = 20f;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.Normal;

                var poke = row.AddComponent<XRSimpleInteractable>();
                var filter = row.AddComponent<XRPokeFilter>();
                filter.pokeConfiguration = new PokeThresholdDatumProperty(new PokeThresholdData
                {
                    pokeDirection = PokeAxis.None,
                    enablePokeAngleThreshold = false,
                    interactionDepthOffset = 0f
                });
                var phrase = cmd.Phrase;
                poke.selectEntered.AddListener(_ => Fire(phrase));
            }
        }

        static string ShortLabel(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
                return "";
            return phrase.StartsWith("Gen, ") ? phrase.Substring(5) : phrase;
        }

        void Fire(string phrase)
        {
            if (host == null)
                return;
            if (!host.TryHandle(phrase, out var reply))
                reply = "Não entendi esse pedido.";
            if (onReply != null && !string.IsNullOrEmpty(reply))
                onReply(reply);
        }
    }
}
