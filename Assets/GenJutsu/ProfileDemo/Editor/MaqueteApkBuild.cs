using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// Batchmode Android build that follows the AGENTS.md APK rule:
    /// bump bundleVersion minor + bundleVersionCode, save, build "Builds/Android/MVP Gen.apk",
    /// then keep a versioned copy "Builds/Android/MVP Gen v&lt;version&gt;.apk".
    /// Usage: Unity.exe -batchmode -quit -projectPath . -buildTarget Android
    ///        -executeMethod GenJutsu.ProfileDemo.EditorTools.MaqueteApkBuild.Build -logFile build.log
    /// </summary>
    public static class MaqueteApkBuild
    {
        public static void BuildCurrentVersion()
        {
            Debug.Log("[MaqueteApkBuild] version " + PlayerSettings.bundleVersion + " code " +
                      PlayerSettings.Android.bundleVersionCode);
            Directory.CreateDirectory("Builds/Android");
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled)
                    scenes.Add(s.path);

            const string apk = "Builds/Android/MVP Gen.apk";
            var report = BuildPipeline.BuildPlayer(scenes.ToArray(), apk, BuildTarget.Android, BuildOptions.None);
            var sum = report.summary;
            Debug.Log("[MaqueteApkBuild] result=" + sum.result + " size=" + sum.totalSize + " errors=" + sum.totalErrors);
            if (sum.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            File.Copy(apk, "Builds/Android/MVP Gen v" + PlayerSettings.bundleVersion + ".apk", true);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        [MenuItem("GenJutsu/Build APK (bump version)")]
        public static void Build()
        {
            var version = BumpMinor(PlayerSettings.bundleVersion);
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode += 1;
            AssetDatabase.SaveAssets();
            Debug.Log("[MaqueteApkBuild] version " + version + " code " + PlayerSettings.Android.bundleVersionCode);

            Directory.CreateDirectory("Builds/Android");
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled)
                    scenes.Add(s.path);

            const string apk = "Builds/Android/MVP Gen.apk";
            var report = BuildPipeline.BuildPlayer(scenes.ToArray(), apk, BuildTarget.Android, BuildOptions.None);
            var sum = report.summary;
            Debug.Log("[MaqueteApkBuild] result=" + sum.result + " size=" + sum.totalSize + " errors=" + sum.totalErrors + " time=" + sum.totalTime);
            if (sum.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }
            File.Copy(apk, "Builds/Android/MVP Gen v" + version + ".apk", true);
            Debug.Log("[MaqueteApkBuild] ok " + apk + " + v" + version);
        }

        static string BumpMinor(string v)
        {
            var parts = (v ?? "1.0").Split('.');
            int major, minor;
            if (parts.Length >= 2 && int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor))
                return major + "." + (minor + 1);
            return v + ".1";
        }
    }
}
