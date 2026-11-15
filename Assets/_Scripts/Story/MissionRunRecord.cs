using System;
using System.Text;

namespace ArtemisX.Story
{
    /// <summary>Serializable summary handed off to the leaderboard screen after a mission ends.</summary>
    [Serializable]
    public struct MissionRunRecord
    {
        public string nickname;
        public string character;
        public int daysTaken;
        public int stars;
        public MissionEndReason endReason;
    }

    /// <summary>Sanitizes player nicknames to <= 12 characters with profanity and personal-data checks.</summary>
    public static class NicknameFilter
    {
        public const int MaxLength = 12;

        private static readonly string[] BlockedTerms =
        {
            "fuck", "shit", "bitch", "asshole", "bastard", "cunt", "dick", "pussy",
            "slut", "whore", "nigger", "faggot", "retard", "porn", "nazi", "caralho",
            "porra", "puta", "merda", "buceta", "viado", "cuzao", "fdp"
        };

        /// <summary>Trims, strips personal-data patterns and profanity, and enforces maxLength (up to 12).</summary>
        public static string Sanitize(string raw, string fallback = "Commander", int maxLength = MaxLength)
        {
            int limit = maxLength > 0 && maxLength <= MaxLength ? maxLength : MaxLength;
            string safeFallback = Truncate(string.IsNullOrWhiteSpace(fallback) ? "Commander" : fallback.Trim(), limit);
            if (string.IsNullOrWhiteSpace(raw))
                return safeFallback;

            var sb = new StringBuilder(raw.Length);
            int digitCount = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];
                if (char.IsControl(ch)) continue;
                if (ch == '@') return safeFallback;
                if (char.IsDigit(ch)) digitCount++;
                sb.Append(ch);
            }

            if (digitCount >= 7)
                return safeFallback;

            string cleaned = sb.ToString().Trim();
            if (string.IsNullOrEmpty(cleaned))
                return safeFallback;

            string lower = cleaned.ToLowerInvariant();
            for (int i = 0; i < BlockedTerms.Length; i++)
            {
                if (lower.Contains(BlockedTerms[i]))
                    return safeFallback;
            }

            return Truncate(cleaned, limit);
        }

        private static string Truncate(string value, int limit)
            => value.Length <= limit ? value : value.Substring(0, limit).TrimEnd();
    }
}
