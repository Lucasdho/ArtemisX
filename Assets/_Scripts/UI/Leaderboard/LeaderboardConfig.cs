using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Supabase REST settings for the run histogram. Publishable (anon) key only, never a secret key.</summary>
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Artemis X/UI/Leaderboard Config")]
    public class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("e.g. https://xyzcompany.supabase.co (empty = offline mode)")]
        public string projectUrl = "";
        [Tooltip("Publishable / anon key. Never paste a secret or service_role key here.")]
        public string publishableKey = "";
        public string runsTable = "runs";
        public string histogramView = "run_histogram";
        [Min(1)] public int timeoutSeconds = 8;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(projectUrl) && !string.IsNullOrWhiteSpace(publishableKey);
        public string RestUrl(string resource) => projectUrl.Trim().TrimEnd('/') + "/rest/v1/" + resource;
    }
}
