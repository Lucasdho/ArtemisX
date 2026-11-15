using System;
using System.Collections.Generic;
using ArtemisX.UI.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtemisX.UI.Tests
{
    public class LeaderboardTests
    {
        private const string LeaderboardConfigPath = "Assets/_Config/Leaderboard/LeaderboardConfig.asset";
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
            created.Clear();
            LeaderboardBuildInjector.CleanupInjectedResource();
        }

        [Test]
        public void LeaderboardConfigAsset_HasNoSecretsCommitted()
        {
            var config = AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(LeaderboardConfigPath);
            Assert.IsNotNull(config, "Missing LeaderboardConfig.asset at " + LeaderboardConfigPath);
            Assert.IsTrue(string.IsNullOrWhiteSpace(config.projectUrl), "LeaderboardConfig.asset must not contain projectUrl in Git.");
            Assert.IsTrue(string.IsNullOrWhiteSpace(config.publishableKey), "LeaderboardConfig.asset must not contain publishableKey in Git.");
            Assert.IsFalse(config.IsConfigured);
        }

        [Test]
        public void TryParseCredentialsJson_ValidatesAndTrimsInputs()
        {
            Assert.IsFalse(LeaderboardClient.TryParseCredentialsJson(null, out _, out _));
            Assert.IsFalse(LeaderboardClient.TryParseCredentialsJson("", out _, out _));
            Assert.IsFalse(LeaderboardClient.TryParseCredentialsJson("{bad json", out _, out _));
            Assert.IsFalse(LeaderboardClient.TryParseCredentialsJson("{\"projectUrl\":\"https://example.supabase.co\",\"publishableKey\":\"   \"}", out _, out _));

            bool ok = LeaderboardClient.TryParseCredentialsJson(
                "{\"projectUrl\":\"  https://example.supabase.co/  \",\"publishableKey\":\"  sb_publishable_abc  \"}",
                out string url,
                out string key);

            Assert.IsTrue(ok);
            Assert.AreEqual("https://example.supabase.co/", url);
            Assert.AreEqual("sb_publishable_abc", key);
        }

        [Test]
        public void LeaderboardClient_UsesExplicitConfigFirst_AndOfflineFallbackWhenNull()
        {
            var go = new GameObject("TestLeaderboardClient");
            created.Add(go);
            var client = go.AddComponent<LeaderboardClient>();

            bool called = false;
            bool wasOnline = true;
            LeaderboardClient.HistogramRow[] rows = new LeaderboardClient.HistogramRow[0];
            client.SubmitAndFetch(new LeaderboardClient.RunPayload(), (online, r) =>
            {
                called = true;
                wasOnline = online;
                rows = r;
            });

            Assert.IsFalse(client.IsConfigured);
            Assert.IsTrue(called);
            Assert.IsFalse(wasOnline);
            Assert.IsNull(rows);

            var cfg = ScriptableObject.CreateInstance<LeaderboardConfig>();
            created.Add(cfg);
            cfg.projectUrl = "https://explicit.supabase.co";
            cfg.publishableKey = "sb_publishable_explicit";

            var so = new SerializedObject(client);
            so.FindProperty("config").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(client.ResolveCredentials(out string resolvedUrl, out string resolvedKey));
            Assert.AreEqual("https://explicit.supabase.co", resolvedUrl);
            Assert.AreEqual("sb_publishable_explicit", resolvedKey);
            Assert.AreEqual("https://explicit.supabase.co/rest/v1/runs", LeaderboardClient.BuildRestUrl(resolvedUrl, cfg.runsTable));
        }

        [Test]
        public void BuildInjector_WritesAndCleansUpRuntimeResource()
        {
            var go = new GameObject("TestLeaderboardClient_Injector");
            created.Add(go);
            var client = go.AddComponent<LeaderboardClient>();

            var cfg = ScriptableObject.CreateInstance<LeaderboardConfig>();
            created.Add(cfg);

            var so = new SerializedObject(client);
            so.FindProperty("config").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();

            LeaderboardBuildInjector.WriteRuntimeConfig("https://injected.supabase.co", "sb_publishable_injected");
            Assert.IsNotNull(Resources.Load<TextAsset>(LeaderboardClient.RuntimeResourceName));

            Assert.IsTrue(client.ResolveCredentials(out string url, out string key));
            Assert.AreEqual("https://injected.supabase.co", url);
            Assert.AreEqual("sb_publishable_injected", key);

            LeaderboardBuildInjector.CleanupInjectedResource();
            Resources.UnloadUnusedAssets();
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/" + LeaderboardClient.RuntimeResourceName + ".json"));
        }

        [Test]
        public void EditorOverrides_RespectEnvironmentVariables()
        {
            string prevUrl = Environment.GetEnvironmentVariable(LeaderboardClient.EnvProjectUrl);
            string prevKey = Environment.GetEnvironmentVariable(LeaderboardClient.EnvPublishableKey);
            try
            {
                Environment.SetEnvironmentVariable(LeaderboardClient.EnvProjectUrl, "https://env.supabase.co");
                Environment.SetEnvironmentVariable(LeaderboardClient.EnvPublishableKey, "sb_publishable_env");

                Assert.IsTrue(LeaderboardClient.TryResolveEditorOverrides(out string url, out string key));
                Assert.AreEqual("https://env.supabase.co", url);
                Assert.AreEqual("sb_publishable_env", key);
            }
            finally
            {
                Environment.SetEnvironmentVariable(LeaderboardClient.EnvProjectUrl, prevUrl);
                Environment.SetEnvironmentVariable(LeaderboardClient.EnvPublishableKey, prevKey);
            }
        }
    }
}
