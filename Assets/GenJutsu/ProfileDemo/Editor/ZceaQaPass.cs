using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public static class ZceaQaPass
    {
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Debug.Log("[ZceaQa] " + ProfileDemoBuilder.Build());
            CaptureSet();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("artifacts/pr10-v2/qa-pass.txt", "ok " + System.DateTime.Now.ToString("o"));
        }

        static void CaptureSet()
        {
            Directory.CreateDirectory("artifacts/pr10-v2");
            var looks = Object.FindObjectsByType<ProfileLook>(FindObjectsInactive.Include);
            for (var i = 0; i < looks.Length; i++)
                looks[i].gameObject.SetActive(false);

            var plaque = Object.FindFirstObjectByType<StartPlaque>(FindObjectsInactive.Include);
            var table = GameObject.Find("JapaneseTableSet");
            if (plaque == null)
            {
                var dojo = Object.FindFirstObjectByType<DojoDirector>(FindObjectsInactive.Include);
                WhiteRoomSetup.Ensure(dojo);
                plaque = Object.FindFirstObjectByType<StartPlaque>(FindObjectsInactive.Include);
            }

            if (plaque != null)
            {
                var look = plaque.transform.position + Vector3.up * 0.02f;
                var from = look + plaque.transform.forward * 0.72f + Vector3.up * 0.12f;
                Cap(from, look, 42f, "artifacts/pr10-v2/hub-plaque.png");
            }

            if (table != null)
            {
                var p = table.transform.position;
                Cap(p + new Vector3(2.6f, 1.9f, -3.4f), p + Vector3.up * 0.5f, 50f, "artifacts/pr10-v2/hub-overview.png");
                table.SetActive(false);
            }

            var gate = Object.FindFirstObjectByType<SessionGate>(FindObjectsInactive.Include);
            if (gate != null)
                gate.gameObject.SetActive(false);

            for (var i = 0; i < looks.Length; i++)
            {
                var look = looks[i];
                for (var j = 0; j < looks.Length; j++)
                    looks[j].gameObject.SetActive(looks[j] == look);
                var house = look.transform;
                var feet = house.TransformPoint(GenJutsu.GenCore.HouseSpawnLogic.LocalFeet);
                var tv = house.TransformPoint(GenJutsu.GenCore.HouseSpawnLogic.LocalLook);
                var id = look.profile.ToString().ToLowerInvariant();
                Cap(feet + Vector3.up * 1.6f, tv + Vector3.up * 0.45f, 55f, "artifacts/pr10-v2/" + id + "-spawn.png");
                var focus = house.position + Vector3.up * 0.85f;
                Cap(focus + new Vector3(-1.65f, 1.55f, 1.85f), focus, 58f, "artifacts/pr10-v2/" + id + "-overview.png");
                Transform stand = null;
                var ts = house.GetComponentsInChildren<Transform>(true);
                for (var t = 0; t < ts.Length; t++)
                {
                    if (ts[t].name == "TvStand")
                        stand = ts[t];
                }

                if (stand != null)
                {
                    var p = stand.position + Vector3.up * 0.7f;
                    Cap(p - stand.forward * 1.35f + Vector3.up * 0.1f, p, 38f, "artifacts/pr10-v2/" + id + "-tv-close.png");
                }
            }

            for (var i = 0; i < looks.Length; i++)
                looks[i].gameObject.SetActive(false);
            if (table != null)
                table.SetActive(true);
            CompareRefs();
        }

        static void CompareRefs()
        {
            var houses = new[] { "zen", "cyber", "excentrico", "acolhedor" };
            var refs = new[]
            {
                "Assets/GenJutsu/ProfileDemo/Reference/zen.jpg",
                "Assets/GenJutsu/ProfileDemo/Reference/cyber.jpg",
                "Assets/GenJutsu/ProfileDemo/Reference/excentrico.jpg",
                "Assets/GenJutsu/ProfileDemo/Reference/acolhedor.jpg"
            };
            for (var i = 0; i < houses.Length; i++)
            {
                var leftBytes = File.ReadAllBytes(refs[i]);
                var rightPath = "artifacts/pr10-v2/" + houses[i] + "-overview.png";
                if (!File.Exists(rightPath))
                    continue;
                var rightBytes = File.ReadAllBytes(rightPath);
                var left = new Texture2D(2, 2, TextureFormat.RGB24, false);
                var right = new Texture2D(2, 2, TextureFormat.RGB24, false);
                left.LoadImage(leftBytes);
                right.LoadImage(rightBytes);
                const int h = 1080;
                var lw = Mathf.Max(1, (int)(left.width * (h / (float)left.height)));
                var rw = Mathf.Max(1, (int)(right.width * (h / (float)right.height)));
                var outTex = new Texture2D(lw + rw, h, TextureFormat.RGB24, false);
                for (var y = 0; y < h; y++)
                {
                    var v = y / (float)(h - 1);
                    for (var x = 0; x < lw; x++)
                        outTex.SetPixel(x, y, left.GetPixelBilinear(x / (float)(lw - 1), v));
                    for (var x = 0; x < rw; x++)
                        outTex.SetPixel(lw + x, y, right.GetPixelBilinear(x / (float)(rw - 1), v));
                }

                outTex.Apply();
                File.WriteAllBytes("artifacts/pr10-v2/compare-" + houses[i] + ".png", outTex.EncodeToPNG());
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
                Object.DestroyImmediate(outTex);
            }
        }

        static void Cap(Vector3 from, Vector3 look, float fov, string path)
        {
            var go = new GameObject("QaCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Debug.Log("[ZceaQa] " + path);
        }
    }
}
