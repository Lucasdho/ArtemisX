using System;
using System.Collections;
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

        [SerializeField] private LeaderboardConfig config;

        public bool IsConfigured => config != null && config.IsConfigured;

        /// <summary>Submits the run, then fetches the histogram. done(online, rows); rows is null when offline.</summary>
        public void SubmitAndFetch(RunPayload payload, Action<bool, HistogramRow[]> done)
        {
            if (!IsConfigured)
            {
                Debug.Log("[Leaderboard] No Supabase URL/key configured: offline mode.", this);
                done?.Invoke(false, null);
                return;
            }
            StartCoroutine(Run(payload, done));
        }

        private IEnumerator Run(RunPayload payload, Action<bool, HistogramRow[]> done)
        {
            using (var post = new UnityWebRequest(config.RestUrl(config.runsTable), UnityWebRequest.kHttpVerbPOST))
            {
                post.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                post.downloadHandler = new DownloadHandlerBuffer();
                SetHeaders(post);
                post.SetRequestHeader("Content-Type", "application/json");
                post.SetRequestHeader("Prefer", "return=minimal");
                yield return post.SendWebRequest();
                if (post.result != UnityWebRequest.Result.Success)
                    Debug.Log($"[Leaderboard] Submit failed ({post.responseCode}): {post.error}", this);
            }

            string url = config.RestUrl(config.histogramView) + "?select=days_taken,end_reason,runs";
            using (var get = UnityWebRequest.Get(url))
            {
                SetHeaders(get);
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

        private void SetHeaders(UnityWebRequest request)
        {
            request.timeout = config.timeoutSeconds;
            request.SetRequestHeader("apikey", config.publishableKey.Trim());
            request.SetRequestHeader("Authorization", "Bearer " + config.publishableKey.Trim());
        }
    }
}
