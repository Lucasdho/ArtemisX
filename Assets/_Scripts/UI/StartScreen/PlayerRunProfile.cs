using System;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI.StartScreen
{
    /// <summary>
    /// Stores the active player's chosen astronaut (0..3), sanitized nickname (<= 12 chars),
    /// and the 4 playable Junior Astronaut roster definitions.
    /// Asset lives at Assets/_Config/UI/PlayerRunProfile.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerRunProfile", menuName = "Artemis X/UI/Player Run Profile")]
    public class PlayerRunProfile : ScriptableObject
    {
        [Serializable]
        public struct AstronautEntry
        {
            public int index;
            public string characterId;
            public string callsign;
            public string roleTitle;
            public Sprite portrait;
            public Color accentColor;
        }

        public const int MaxNicknameLength = 12;
        public const int RosterCount = 4;

        private static readonly string[] ExtraBlockedTerms =
        {
            "fuck", "shit", "bitch", "asshole", "bastard", "cunt", "dick", "pussy",
            "slut", "whore", "nigger", "faggot", "retard", "porn", "nazi", "caralho",
            "porra", "puta", "merda", "buceta", "viado", "cuzao", "fdp", "damn", "crap", "sex", "kill"
        };

        [Header("Default Settings")]
        public string defaultNickname = "Commander";
        [Range(0, 3)] public int defaultCharacterIndex = 0;

        [Header("Current Run State")]
        [Range(0, 3)] public int selectedCharacter = 0;
        public string playerNickname = "Commander";

        /// <summary>Runtime 3D face snapshot captured in CharacterSelect; preferred over static roster portraits.</summary>
        [NonSerialized] public Sprite runtimeCapturedPortrait;

        [Header("Optional Dialogue Speaker Binding")]
        [Tooltip("When assigned, ApplySelection updates the player speaker portrait and accent color for dialogue.")]
        public Speaker playerSpeaker;

        [Header("Playable Junior Astronaut Roster (0..3)")]
        public AstronautEntry[] astronauts =
        {
            new AstronautEntry
            {
                index = 0,
                characterId = "astronaut_0",
                callsign = "NOVA",
                roleTitle = "Mission Commander",
                accentColor = new Color(0.220f, 0.741f, 0.973f, 1f)
            },
            new AstronautEntry
            {
                index = 1,
                characterId = "astronaut_1",
                callsign = "LEO",
                roleTitle = "Systems Engineer",
                accentColor = new Color(0.961f, 0.620f, 0.043f, 1f)
            },
            new AstronautEntry
            {
                index = 2,
                characterId = "astronaut_2",
                callsign = "MAYA",
                roleTitle = "Lunar Botanist",
                accentColor = new Color(0.133f, 0.773f, 0.369f, 1f)
            },
            new AstronautEntry
            {
                index = 3,
                characterId = "astronaut_3",
                callsign = "KAI",
                roleTitle = "Rover Pilot",
                accentColor = new Color(0.659f, 0.333f, 0.969f, 1f)
            }
        };

        public string SelectedCharacterId => GetAstronaut(selectedCharacter).characterId;

        /// <summary>Returns the roster metadata for astronaut index 0..3.</summary>
        public AstronautEntry GetAstronaut(int index)
        {
            if (astronauts == null || astronauts.Length == 0)
            {
                return new AstronautEntry
                {
                    index = Mathf.Clamp(index, 0, 3),
                    characterId = "astronaut_" + Mathf.Clamp(index, 0, 3),
                    callsign = "NOVA",
                    roleTitle = "Junior Astronaut",
                    accentColor = new Color(0.220f, 0.741f, 0.973f, 1f)
                };
            }

            int clamped = Mathf.Clamp(index, 0, astronauts.Length - 1);
            return astronauts[clamped];
        }

        /// <summary>
        /// Validates and sanitizes a candidate nickname (<= 12 chars, no profanity, no emails/phone numbers or digits-only).
        /// Returns true if the input was accepted as-is (after trim), or false if a filter rule replaced/adjusted it.
        /// </summary>
        public bool ValidateAndSanitizeNickname(string raw, out string sanitized, out string hintMessage)
        {
            string fallback = string.IsNullOrWhiteSpace(defaultNickname) ? "Commander" : defaultNickname.Trim();
            if (fallback.Length > MaxNicknameLength)
                fallback = fallback.Substring(0, MaxNicknameLength).TrimEnd();

            if (string.IsNullOrWhiteSpace(raw))
            {
                sanitized = fallback;
                hintMessage = $"Empty callsign — using default \"{fallback}\".";
                return false;
            }

            string trimmed = raw.Trim();
            if (trimmed.Length > MaxNicknameLength)
                trimmed = trimmed.Substring(0, MaxNicknameLength).TrimEnd();

            // Check PII patterns (email, URL, phone digits, or numbers-only)
            if (trimmed.IndexOf('@') >= 0 || trimmed.IndexOf('.') >= 0)
            {
                sanitized = fallback;
                hintMessage = $"Please use a fun callsign (no emails or web links). Using \"{fallback}\".";
                return false;
            }

            int digits = 0;
            int letters = 0;
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (char.IsDigit(trimmed[i])) digits++;
                else if (char.IsLetter(trimmed[i])) letters++;
            }

            if (letters == 0 || digits >= 5)
            {
                sanitized = fallback;
                hintMessage = $"Callsign needs letters (no phone numbers or digits-only). Using \"{fallback}\".";
                return false;
            }

            string lower = trimmed.ToLowerInvariant();
            for (int i = 0; i < ExtraBlockedTerms.Length; i++)
            {
                if (lower.Contains(ExtraBlockedTerms[i]))
                {
                    sanitized = fallback;
                    hintMessage = $"Keep callsigns kid-friendly! Reset to \"{fallback}\".";
                    return false;
                }
            }

            sanitized = NicknameFilter.Sanitize(trimmed, fallback, MaxNicknameLength);
            if (!string.Equals(sanitized, trimmed, StringComparison.Ordinal))
            {
                hintMessage = $"Adjusted callsign to \"{sanitized}\".";
                return false;
            }

            hintMessage = "Callsign ready for launch!";
            return true;
        }

        /// <summary>Saves the selected character (0..3) and sanitized nickname into this profile and updates optional Speaker.</summary>
        public string ApplySelection(int characterIndex, string rawNickname, out string filterHint)
        {
            selectedCharacter = Mathf.Clamp(characterIndex, 0, RosterCount - 1);
            ValidateAndSanitizeNickname(rawNickname, out string cleanName, out filterHint);
            playerNickname = cleanName;
            handoffValid = true;
            handoffCharacter = selectedCharacter;
            handoffNickname = playerNickname;
            handoffPortrait = runtimeCapturedPortrait;
            ApplySpeakerPortrait();
            return playerNickname;
        }

        // Survives scene loads even if Unity reloads this asset (builds), so CharacterSelect -> HandMadeBase keeps the choice.
        private static bool handoffValid;
        private static int handoffCharacter;
        private static string handoffNickname;
        private static Sprite handoffPortrait;

        /// <summary>Restores the choice made in CharacterSelect after a scene load. No-op if none was made.</summary>
        public void RestoreHandoff()
        {
            if (!handoffValid) return;
            selectedCharacter = handoffCharacter;
            playerNickname = handoffNickname;
            if (handoffPortrait != null) runtimeCapturedPortrait = handoffPortrait;
        }

        /// <summary>Updates the linked player Speaker using the runtime 3D face photo (or roster fallback) and accent color.</summary>
        public void ApplySpeakerPortrait()
        {
            if (playerSpeaker == null) return;
            var entry = GetAstronaut(selectedCharacter);
            Sprite activePortrait = runtimeCapturedPortrait != null ? runtimeCapturedPortrait : entry.portrait;
            if (activePortrait != null)
            {
                playerSpeaker.portrait = activePortrait;
                playerSpeaker.happyPortrait = activePortrait;
                playerSpeaker.worriedPortrait = activePortrait;
            }
            playerSpeaker.accentColor = entry.accentColor;
        }
    }

    /// <summary>Extension helpers so DialogueRunner.SetPlayerProfile(...) can be called cleanly without touching Story scripts.</summary>
    public static class DialogueRunnerProfileExtensions
    {
        public static void SetPlayerProfile(this DialogueRunner runner, PlayerRunProfile profile)
        {
            if (runner == null || profile == null) return;
            runner.SetPlayerName(profile.playerNickname);
            runner.SetPlayerCharacter(profile.SelectedCharacterId);
        }

        public static void SetPlayerProfile(this DialogueRunner runner, string playerNickname, int selectedCharacter)
        {
            if (runner == null) return;
            runner.SetPlayerName(playerNickname);
            runner.SetPlayerCharacter("astronaut_" + Mathf.Clamp(selectedCharacter, 0, 3));
        }
    }
}
