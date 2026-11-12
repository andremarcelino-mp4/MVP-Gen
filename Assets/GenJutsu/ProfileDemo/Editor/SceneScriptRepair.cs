using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// Rebinds components whose script lives only inside the scene (nested or misnamed classes)
    /// to their real script assets, rebuilds the ZCEA homes and saves SampleScene.
    /// A scene-only script makes the player read the scene with the wrong layout and crash on load.
    /// </summary>
    public static class SceneScriptRepair
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var before = Report(scene, "before");
            var fixedCount = Rebind(scene);
            var removed = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneRepair] " + ProfileDemoBuilder.Build());
            scene = EditorSceneManager.OpenScene(ScenePath);
            fixedCount += Rebind(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var after = Report(scene, "after");
            var line = "rebound=" + fixedCount + " missingRemoved=" + removed + " | " + before + " | " + after;
            Directory.CreateDirectory("artifacts/v3.5");
            File.WriteAllText("artifacts/v3.5/scene-repair.txt", line);
            Debug.Log("[SceneRepair] " + line);
            if (after.Contains("sceneOnly=0 ") == false || after.Contains("missing=0") == false)
                throw new System.Exception("[SceneRepair] scene still has bad script refs: " + after);
        }

        static int Rebind(UnityEngine.SceneManagement.Scene scene)
        {
            var n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null)
                        continue;
                    var ms = MonoScript.FromMonoBehaviour(mb);
                    if (ms != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(ms)))
                        continue;
                    var type = mb.GetType();
                    var go = mb.gameObject;
                    AudioSource src = null;
                    if (mb is BallThud thud)
                        src = thud.source;
                    var enabled = mb.enabled;
                    Object.DestroyImmediate(mb, true);
                    var fresh = (MonoBehaviour)go.AddComponent(type);
                    fresh.enabled = enabled;
                    if (fresh is BallThud nt)
                        nt.source = src != null ? src : go.GetComponent<AudioSource>();
                    if (fresh is BoundsReset br)
                        br.Capture();
                    n++;
                }
            }
            return n;
        }

        static string Report(UnityEngine.SceneManagement.Scene scene, string tag)
        {
            int sceneOnly = 0, missing = 0, thuds = 0, thudNoSource = 0;
            var names = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null) { missing++; continue; }
                        if (!(c is MonoBehaviour mb)) continue;
                        var ms = MonoScript.FromMonoBehaviour(mb);
                        if (ms == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(ms)))
                        {
                            sceneOnly++;
                            names.Add(mb.GetType().Name);
                        }
                        if (mb is BallThud b) { thuds++; if (b.source == null) thudNoSource++; }
                    }
                }
            }
            return tag + ": sceneOnly=" + sceneOnly + " [" + string.Join(",", names) + "] missing=" + missing + " ballThud=" + thuds + " thudNoSource=" + thudNoSource;
        }
    }
}