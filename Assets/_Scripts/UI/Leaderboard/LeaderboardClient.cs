using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtemisX.UI
{
    /// <summary>
    /// Posts a finished run to Supabase (POST /rest/v1/runs) and reads the histogram view (GET /rest/v1/run_histogram)
    /// with UnityWebRequest coroutines (WebGL-safe). Any failure reports offline; it never throws or blocks.
    /// </summary>
    public class LeaderboardClient : MonoBehaviour
    {
        public const string RuntimeResourceName = "LeaderboardRuntimeConfig";
        public const string LocalSecretsFileName = ".leaderboard.local.json";
        public const string EnvProjectUrl = "ARTEMISX_SUPABASE_URL";
        public const string EnvPublishableKey = "ARTEMISX_SUPABASE_PUBLISHABLE_KEY";

        [Serializable]
        public class RunPayload
        {
            public string nickname;
            public int character_index;
            public int days_taken;
            public int stars;
            public string end_reason;
        }

        [Serializable]
        public class HistogramRow
        {
            public int days_taken;
            public string end_reason;
            public int runs;
        }

        [Serializable]
        private class HistogramWrapper
        {
            public HistogramRow[] rows;
        }

        [Serializable]
        public class RuntimeCredentials
        {
            public string projectUrl = "";
            public string publishableKey = "";
        }

        [Serializable]
        private class JwtRolePayload
        {
            public string role;
        }

        [SerializeField] private LeaderboardConfig config;

        public bool IsConfigured => ResolveCredentials(out _, out _);

        /// <summary>Submits the run, then fetches the histogram. done(online, rows); rows is null when offline.</summary>
        public void SubmitAndFetch(RunPayload payload, Action<bool, HistogramRow[]> done)
        {
            if (payload == null)
            {
                Debug.Log("[Leaderboard] Null run payload: skipping submission.", this);
                done?.Invoke(false, null);
                return;
            }

            if (!ResolveCredentials(out string projectUrl, out string publishableKey))
            {
                Debug.Log("[Leaderboard] No Supabase URL/key configured: offline mode.", this);
                done?.Invoke(false, null);
                return;
            }
            StartCoroutine(Run(projectUrl, publishableKey, payload, done));
        }

        public bool ResolveCredentials(out string projectUrl, out string publishableKey)
        {
            if (config == null)
            {
                projectUrl = string.Empty;
                publishableKey = string.Empty;
                return false;
            }

            if (config.IsConfigured && IsSafeClientKey(config.publishableKey))
            {
                projectUrl = config.projectUrl.Trim();
                publishableKey = config.publishableKey.Trim();
                return true;
            }

            var runtimeAsset = Resources.Load<TextAsset>(RuntimeResourceName);
            if (runtimeAsset != null && TryParseCredentialsJson(runtimeAsset.text, out projectUrl, out publishableKey))
                return true;

#if UNITY_EDITOR
            if (TryResolveEditorOverrides(out projectUrl, out publishableKey))
                return true;
#endif

            projectUrl = string.Empty;
            publishableKey = string.Empty;
            return false;
        }

        public static bool IsSafeClientKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            string trimmed = key.Trim();
            if (trimmed.StartsWith("sb_secret_", StringComparison.OrdinalIgnoreCase))
                return false;

            int firstDot = trimmed.IndexOf('.');
            if (firstDot > 0)
            {
                int secondDot = trimmed.IndexOf('.', firstDot + 1);
                if (secondDot > firstDot + 1 && trimmed.IndexOf('.', secondDot + 1) < 0)
                {
                    string payloadPart = trimmed.Substring(firstDot + 1, secondDot - firstDot - 1)
                        .Replace('-', '+')
                        .Replace('_', '/');
                    int mod = payloadPart.Length % 4;
                    if (mod == 2) payloadPart += "==";
                    else if (mod == 3) payloadPart += "=";

                    try
                    {
                        string json = Encoding.UTF8.GetString(Convert.FromBase64String(payloadPart));
                        var claims = JsonUtility.FromJson<JwtRolePayload>(json);
                        if (claims != null && string.Equals(claims.role, "service_role", StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                    catch (Exception)
                    {
                        // Non-JSON or malformed segment; allow caller/server to validate if not a service_role JWT.
                    }
                }
            }

            return true;
        }

        public static bool TryParseCredentialsJson(string json, out string projectUrl, out string publishableKey)
        {
            projectUrl = string.Empty;
            publishableKey = string.Empty;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var creds = JsonUtility.FromJson<RuntimeCredentials>(json);
                if (creds == null || string.IsNullOrWhiteSpace(creds.projectUrl) || !IsSafeClientKey(creds.publishableKey))
                    return false;

                projectUrl = creds.projectUrl.Trim();
                publishableKey = creds.publishableKey.Trim();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string BuildRestUrl(string baseUrl, string resource)
        {
            return (baseUrl ?? string.Empty).Trim().TrimEnd('/') + "/rest/v1/" + resource;
        }

#if UNITY_EDITOR
        public static bool TryResolveEditorOverrides(out string projectUrl, out string publishableKey)
        {
            string envUrl = Environment.GetEnvironmentVariable(EnvProjectUrl);
            string envKey = Environment.GetEnvironmentVariable(EnvPublishableKey);
            if (!string.IsNullOrWhiteSpace(envUrl) && IsSafeClientKey(envKey))
            {
                projectUrl = envUrl.Trim();
                publishableKey = envKey.Trim();
                return true;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string localPath = Path.Combine(projectRoot, LocalSecretsFileName);
                if (File.Exists(localPath))
                    return TryParseCredentialsJson(File.ReadAllText(localPath, Encoding.UTF8), out projectUrl, out publishableKey);
            }

            projectUrl = string.Empty;
            publishableKey = string.Empty;
            return false;
        }
#endif

        private IEnumerator Run(string projectUrl, string publishableKey, RunPayload payload, Action<bool, HistogramRow[]> done)
        {
            using (var post = new UnityWebRequest(BuildRestUrl(projectUrl, config.runsTable), UnityWebRequest.kHttpVerbPOST))
            {
                post.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                post.downloadHandler = new DownloadHandlerBuffer();
                SetHeaders(post, publishableKey);
                post.SetRequestHeader("Content-Type", "application/json");
                post.SetRequestHeader("Prefer", "return=minimal");
                yield return post.SendWebRequest();
                if (post.result != UnityWebRequest.Result.Success)
                    Debug.Log($"[Leaderboard] Submit failed ({post.responseCode}): {post.error}", this);
            }

            string url = BuildRestUrl(projectUrl, config.histogramView) + "?select=days_taken,end_reason,runs";
            using (var get = UnityWebRequest.Get(url))
            {
                SetHeaders(get, publishableKey);
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Leaderboard] Histogram failed ({get.responseCode}): {get.error}", this);
                    done?.Invoke(false, null);
                    yield break;
                }

                HistogramRow[] rows = null;
                try
                {
                    rows = JsonUtility.FromJson<HistogramWrapper>("{\"rows\":" + get.downloadHandler.text + "}").rows;
                }
                catch (Exception e)
                {
                    Debug.Log("[Leaderboard] Could not read histogram: " + e.Message, this);
                }
                done?.Invoke(rows != null, rows);
            }
        }

        private void SetHeaders(UnityWebRequest request, string publishableKey)
        {
            request.timeout = config.timeoutSeconds;
            string key = publishableKey.Trim();
            request.SetRequestHeader("apikey", key);
            if (!key.StartsWith("sb_"))
                request.SetRequestHeader("Authorization", "Bearer " + key);
        }
    }
}
