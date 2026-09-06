using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Alif.EditorTools
{
    public static class AlifStandaloneBuilder
    {
        public static readonly string[] Scenes = AlifAdventureBuilder.ScenePaths;

        [MenuItem("Alif/Build/Build Standalone macOS App")]
        public static void BuildMacApp()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Build/macOS/Alif.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            PerformBuild("macOS", buildPlayerOptions);
        }

        [MenuItem("Alif/Build/Build WebGL")]
        public static void BuildWebGL()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Build/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            PerformBuild("WebGL", buildPlayerOptions);
        }

        [MenuItem("Alif/Build/Build Standalone Windows (64-bit)")]
        public static void BuildWindows()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Build/Windows/Alif.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            PerformBuild("Windows", buildPlayerOptions);
        }

        private static void PerformBuild(string platformName, BuildPlayerOptions options)
        {
            AlifAdventureBuilder.ValidateContent();
            AlifAdventureBuilder.ValidateScenes();
            Debug.Log($"[Alif] Starting {platformName} Build...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Alif] {platformName} Build succeeded! Output: {options.locationPathName} ({summary.totalSize} bytes)");
            }
            else
            {
                throw new System.InvalidOperationException($"[Alif] {platformName} Build failed with {summary.totalErrors} errors.");
            }
        }
    }
}
