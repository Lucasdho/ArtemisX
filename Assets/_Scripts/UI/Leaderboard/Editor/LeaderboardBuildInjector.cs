using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArtemisX.UI.EditorTools
{
    /// <summary>
    /// Injects local/CI Supabase leaderboard credentials into a gitignored Resources asset only for the duration of a build,
    /// keeping LeaderboardConfig.asset empty in version control.
    /// </summary>
    [InitializeOnLoad]
    public sealed class LeaderboardBuildInjector : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string LeaderboardConfigPath = "Assets/_Config/Leaderboard/LeaderboardConfig.asset";
        private const string RuntimeConfigAssetPath = "Assets/Resources/" + LeaderboardClient.RuntimeResourceName + ".json";

        public int callbackOrder => -100;

        static LeaderboardBuildInjector()
        {
            if (!BuildPipeline.isBuildingPlayer)
                CleanupInjectedResource();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            CleanupInjectedResource();

            var config = AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(LeaderboardConfigPath);
            if (config != null && config.IsConfigured)
                return;

            if (!LeaderboardClient.TryResolveEditorOverrides(out string projectUrl, out string publishableKey))
            {
                Debug.Log("[Leaderboard] No local/env credentials found; building in offline leaderboard mode.");
                return;
            }

            WriteRuntimeConfig(projectUrl, publishableKey);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanupInjectedResource();
        }

        public static void WriteRuntimeConfig(string projectUrl, string publishableKey)
        {
            string fullPath = Path.GetFullPath(RuntimeConfigAssetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var payload = new LeaderboardClient.RuntimeCredentials
            {
                projectUrl = projectUrl.Trim(),
                publishableKey = publishableKey.Trim()
            };
            File.WriteAllText(fullPath, JsonUtility.ToJson(payload), Encoding.UTF8);
            AssetDatabase.ImportAsset(RuntimeConfigAssetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        public static void CleanupInjectedResource()
        {
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(RuntimeConfigAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(RuntimeConfigAssetPath);
                return;
            }

            string fullPath = Path.GetFullPath(RuntimeConfigAssetPath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            string metaPath = fullPath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }
    }
}
