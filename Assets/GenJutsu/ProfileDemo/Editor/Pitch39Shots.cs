using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>Batchmode Game-view substitutes: Camera.Render to PNG (works with -nographics).</summary>
    public static class Pitch39Shots
    {
        const string OutDir = @"C:\Users\andre\Quest-test-prints\v3.9";

        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var cam = EnsureCam();
            Shot(cam, "01-pergaminho-limpo.png", FrameScroll);
            Shot(cam, "02-casa-altura-olhos.png", FrameHouse);
            Shot(cam, "03-pedidos-da-casa.png", FramePanel);
            Debug.Log("[Pitch39Shots] wrote " + OutDir);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static Camera EnsureCam()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("ShotCam");
                cam = go.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.enabled = true;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.11f, 0.1f);
            return cam;
        }

        static void Shot(Camera cam, string file, System.Action<Camera> frame)
        {
            frame(cam);
            var w = 1280;
            var h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[Pitch39Shots] " + file);
        }

        static void FrameScroll(Camera cam)
        {
            var scroll = GameObject.Find("Scroll") ?? GameObject.Find("Paper");
            if (scroll == null)
            {
                var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i].name == "Scroll" || all[i].name == "Paper")
                    {
                        scroll = all[i].gameObject;
                        break;
                    }
                }
            }

            var t = scroll != null ? scroll.transform : null;
            var look = t != null ? t.position : new Vector3(0f, 0.7f, 0f);
            cam.transform.position = look + new Vector3(0.35f, 0.42f, 0.55f);
            cam.transform.LookAt(look);
        }

        static void FrameHouse(Camera cam)
        {
            Transform house = null;
            var looks = Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include);
            for (var i = 0; i < looks.Length; i++)
            {
                if (looks[i] != null && looks[i].profile == ProfileId.Zen)
                {
                    house = looks[i].transform;
                    looks[i].gameObject.SetActive(true);
                    break;
                }
            }

            if (house == null)
            {
                var named = GameObject.Find("House_Zen");
                house = named != null ? named.transform : null;
            }

            var origin = house != null ? house.position : Vector3.zero;
            var eye = origin + new Vector3(0f, 1.6f, 0.4f);
            cam.transform.position = eye;
            cam.transform.LookAt(origin + new Vector3(0f, 1.2f, 1.4f));
        }

        static void FramePanel(Camera cam)
        {
            var existing = Object.FindFirstObjectByType<FurnitureMovePanel>(FindObjectsInactive.Include);
            Transform panel = existing != null ? existing.transform : null;
            if (panel == null)
            {
                var go = new GameObject("FurnitureMovePanelShot");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(520f, 360f);
                go.transform.localScale = Vector3.one * 0.001f;
                var bg = new GameObject("Bg", typeof(RectTransform));
                bg.transform.SetParent(go.transform, false);
                var bgRect = (RectTransform)bg.transform;
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                bg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.05f, 0.05f, 0.06f, 0.92f);
                var titleGo = new GameObject("Title", typeof(RectTransform));
                titleGo.transform.SetParent(go.transform, false);
                var titleRect = (RectTransform)titleGo.transform;
                titleRect.anchorMin = Vector2.zero;
                titleRect.anchorMax = Vector2.one;
                titleRect.offsetMin = new Vector2(16f, 16f);
                titleRect.offsetMax = new Vector2(-16f, -16f);
                var title = titleGo.AddComponent<TextMeshProUGUI>();
                title.text = "Pedidos da casa\n\ntraz a mesa pro centro\nleva a luminária pra janela\nmove o bonsai pro canto\nvolta tudo pro lugar";
                title.fontSize = 28f;
                title.color = Color.white;
                panel = go.transform;
            }

            panel.position = new Vector3(0f, 1.4f, 1.2f);
            panel.rotation = Quaternion.identity;
            cam.transform.position = panel.position + new Vector3(0f, 0f, -0.9f);
            cam.transform.LookAt(panel.position);
        }
    }
}
