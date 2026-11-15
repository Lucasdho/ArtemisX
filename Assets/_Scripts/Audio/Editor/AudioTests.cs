using System;
using System.Collections.Generic;
using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Story;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtemisX.Audio.Tests
{
    /// <summary>EditMode tests for AudioConfig, AudioManager, AudioManager.prefab, and SimulationAudioBridge.</summary>
    public class AudioTests
    {
        private const string AudioConfigPath = "Assets/_Config/Audio/AudioConfig.asset";
        private const string AudioPrefabPath = "Assets/_Prefabs/Audio/AudioManager.prefab";
        private const string GameConfigPath = "Assets/_Config/GameConfig.asset";
        private const string WeatherConfigPath = "Assets/_Config/Simulation/SpaceWeatherConfig.asset";
        private const string SiteConfigPath = "Assets/_Config/Simulation/ExpeditionSiteConfig.asset";
        private const string BuildingConfigPath = "Assets/_Config/Simulation/BuildingConfig.asset";
        private const string DemandConfigPath = "Assets/_Config/Simulation/UnexpectedDemandConfig.asset";

        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    Object.DestroyImmediate(createdObjects[i]);
            }
            createdObjects.Clear();
        }

        private GameObject Track(GameObject go)
        {
            createdObjects.Add(go);
            return go;
        }

        [Test]
        public void AudioConfig_HasAllSixteenSoundCuesWithRealClips()
        {
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            Assert.IsNotNull(config, "Missing AudioConfig.asset at " + AudioConfigPath);
            Assert.That(config.masterVolume, Is.InRange(0.01f, 1f));
            Assert.IsFalse(config.muted, "AudioConfig should not start muted by default.");

            var allCues = (SoundCue[])Enum.GetValues(typeof(SoundCue));
            Assert.AreEqual(16, allCues.Length, "SoundCue enum must define all 16 cues.");

            foreach (SoundCue cue in allCues)
            {
                bool found = config.TryGetEntry(cue, out var entry);
                Assert.IsTrue(found, $"Missing CueEntry for {cue}");
                Assert.IsNotNull(entry.clip, $"Null AudioClip for {cue}");

                string clipPath = AssetDatabase.GetAssetPath(entry.clip);
                Assert.That(clipPath, Does.StartWith("Assets/GameAssets/kenney_interface-sounds/Audio/"),
                    $"Clip for {cue} should come from kenney_interface-sounds/Audio");
                Assert.That(entry.volume, Is.InRange(0.05f, 1f), $"Invalid volume for {cue}");
                Assert.That(entry.pitchMin, Is.InRange(0.8f, 1.2f), $"Invalid pitchMin for {cue}");
                Assert.That(entry.pitchMax, Is.InRange(0.8f, 1.2f), $"Invalid pitchMax for {cue}");
                Assert.LessOrEqual(entry.pitchMin, entry.pitchMax, $"pitchMin > pitchMax for {cue}");
            }
        }

        [Test]
        public void AudioManagerPrefab_HasComponentsAndWiredConfig()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefabPath);
            Assert.IsNotNull(prefab, "Missing AudioManager.prefab at " + AudioPrefabPath);

            var manager = prefab.GetComponent<AudioManager>();
            var bridge = prefab.GetComponent<SimulationAudioBridge>();
            var source = prefab.GetComponent<AudioSource>();

            Assert.IsNotNull(manager, "AudioManager component missing on prefab.");
            Assert.IsNotNull(bridge, "SimulationAudioBridge component missing on prefab.");
            Assert.IsNotNull(source, "AudioSource component missing on prefab.");
            Assert.IsNotNull(manager.Config, "AudioManager.Config is not assigned on prefab.");
        }

        [Test]
        public void AudioManager_StaticNoOpAndCuePlaybackAndMute()
        {
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            var go = Track(new GameObject("AudioManagerTest"));
            var manager = go.AddComponent<AudioManager>();
            manager.Initialize(config);

            var playedCues = new List<SoundCue>();
            manager.CuePlayed += (cue, _) => playedCues.Add(cue);

            var allCues = (SoundCue[])Enum.GetValues(typeof(SoundCue));
            foreach (SoundCue cue in allCues)
            {
                AudioManager.Play(cue);
                Assert.AreEqual(cue, manager.LastPlayedCue);
                Assert.IsNotNull(manager.LastPlayedClip);
            }
            Assert.AreEqual(allCues.Length, manager.PlayCount);
            Assert.AreEqual(allCues.Length, playedCues.Count);

            manager.PlayClick();
            Assert.AreEqual(SoundCue.UiClick, manager.LastPlayedCue);

            manager.PlayConfirm();
            Assert.AreEqual(SoundCue.BuildSuccess, manager.LastPlayedCue);

            manager.PlayCancel();
            Assert.AreEqual(SoundCue.UiClose, manager.LastPlayedCue);

            int countBeforeMute = manager.PlayCount;
            manager.ToggleMute();
            Assert.IsTrue(manager.IsMuted);
            Assert.AreEqual(0f, AudioListener.volume, 0.0001f);

            manager.PlayCue(SoundCue.StormAlert);
            Assert.AreEqual(SoundCue.StormAlert, manager.LastPlayedCue);
            Assert.AreEqual(countBeforeMute, manager.PlayCount, "Muted AudioManager should not increment active PlayCount.");

            manager.ToggleMute();
            Assert.IsFalse(manager.IsMuted);
            Assert.AreEqual(1f, AudioListener.volume, 0.0001f);
            manager.PlayCue(SoundCue.StormAlert);
            Assert.AreEqual(countBeforeMute + 1, manager.PlayCount);

            Object.DestroyImmediate(go);
            Assert.IsNull(AudioManager.Instance);
            Assert.DoesNotThrow(() => AudioManager.Play(SoundCue.UiClick));
        }

        [Test]
        public void SimulationAudioBridge_SubscribesAndUnwiresCleanly()
        {
            var audioConfig = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            var weatherConfig = AssetDatabase.LoadAssetAtPath<SpaceWeatherConfig>(WeatherConfigPath);
            var siteConfig = AssetDatabase.LoadAssetAtPath<ExpeditionSiteConfig>(SiteConfigPath);
            var buildingConfig = AssetDatabase.LoadAssetAtPath<BuildingConfig>(BuildingConfigPath);
            var demandConfig = AssetDatabase.LoadAssetAtPath<UnexpectedDemandConfig>(DemandConfigPath);

            var go = Track(new GameObject("SimAudioTest"));
            var days = go.AddComponent<DayManager>();
            var res = go.AddComponent<ResourceManager>();
            var solar = go.AddComponent<SolarFieldManager>();
            var weather = go.AddComponent<SpaceWeatherManager>();
            var rover = go.AddComponent<RoverExpeditionManager>();
            var buildings = go.AddComponent<BuildingManager>();
            var supply = go.AddComponent<SupplyManager>();
            var demands = go.AddComponent<UnexpectedDemandManager>();
            var runner = go.AddComponent<DialogueRunner>();
            var story = go.AddComponent<StoryDirector>();
            var audio = go.AddComponent<AudioManager>();
            var bridge = go.AddComponent<SimulationAudioBridge>();

            res.Initialize(gameConfig);
            solar.Initialize(gameConfig, res);
            weather.Initialize(weatherConfig, days, res, solar);
            rover.Initialize(siteConfig, gameConfig, res, weather);
            buildings.Initialize(buildingConfig, res);
            supply.Initialize(gameConfig, res, days);
            demands.Initialize(demandConfig, res, days, weather);
            audio.Initialize(audioConfig);

            var played = new List<SoundCue>();
            audio.CuePlayed += (cue, _) => played.Add(cue);

            bridge.Initialize(audio, buildings, rover, solar, supply, weather, days, story, runner, demands);

            // 1. Building completed + Star earned
            res.Add(ResourceType.Parts, 50);
            res.Add(ResourceType.Power, 50);
            played.Clear();
            Assert.IsTrue(buildings.TryBuildNext());
            CollectionAssert.Contains(played, SoundCue.BuildSuccess);
            CollectionAssert.Contains(played, SoundCue.StarEarned);

            // 2. Solar toggle
            played.Clear();
            Assert.IsTrue(solar.ToggleRetract());
            Assert.AreEqual(new[] { SoundCue.ToggleSwitch }, played);

            // 3. Rover dispatch + calm return
            played.Clear();
            rover.RandomValue = () => 0.99f;
            Assert.IsTrue(rover.TryDispatchRover(siteConfig.sites[0].siteId));
            Assert.AreEqual(new[] { SoundCue.RoverDispatch }, played);

            played.Clear();
            rover.ResolveReturn();
            Assert.AreEqual(new[] { SoundCue.RoverReturn }, played);

            // 4. Rover broken + repaired
            rover.TryDispatchRover(siteConfig.sites[0].siteId);
            rover.NotifyStorm();
            rover.RandomValue = () => 0f; // 30% storm rule: force the break roll
            played.Clear();
            rover.ResolveReturn();
            CollectionAssert.Contains(played, SoundCue.ComponentBroken);

            played.Clear();
            Assert.IsTrue(rover.TryRepairRover());
            Assert.AreEqual(new[] { SoundCue.ComponentRepaired }, played);

            // 5. Supply drop + collect
            played.Clear();
            supply.OnDayStarted(5);
            Assert.AreEqual(new[] { SoundCue.RoverReturn }, played);

            played.Clear();
            Assert.IsTrue(supply.CollectSupply());
            Assert.AreEqual(new[] { SoundCue.SupplyCollect }, played);

            // 6. Storm alert + Weather choice (ShieldAndRetract should not double-fire ToggleSwitch) + Solar break & repair
            solar.ToggleRetract(); // back to Active
            played.Clear();
            weather.SetDay(4);
            CollectionAssert.Contains(played, SoundCue.StormAlert);

            played.Clear();
            Assert.IsTrue(weather.ApplyWeatherChoice(WeatherChoice.ShieldAndRetract));
            Assert.AreEqual(new[] { SoundCue.ToggleSwitch }, played, "ShieldAndRetract should fire ToggleSwitch once.");

            solar.ToggleRetract(); // back to Active so storm can break it
            solar.RandomValue = () => 0f;
            played.Clear();
            Assert.IsTrue(solar.ResolveStorm());
            Assert.AreEqual(new[] { SoundCue.ComponentBroken }, played);

            played.Clear();
            Assert.IsTrue(solar.TryRepairSolar());
            Assert.AreEqual(new[] { SoundCue.ComponentRepaired }, played, "Repairing broken solar field should only fire ComponentRepaired.");

            // 7. Day advance
            played.Clear();
            days.AdvanceDay();
            CollectionAssert.Contains(played, SoundCue.DayAdvance);

            // 8. DialogueRunner 1-line choice sequence fires EventPrompt once (not twice when typing finishes)
            var choiceSeq = ScriptableObject.CreateInstance<DialogueSequence>();
            createdObjects.Add(choiceSeq);
            choiceSeq.lines = new[]
            {
                new DialogueSequence.DialogueLine { text = "Quick decision needed!" }
            };
            choiceSeq.choices = new[]
            {
                new DialogueSequence.DialogueChoice { choiceId = "opt_a", label = "Option A" }
            };

            played.Clear();
            runner.Enqueue(choiceSeq);
            Assert.AreEqual(new[] { SoundCue.EventPrompt }, played);

            played.Clear();
            runner.AdvanceOrSkip(); // finishes typing line 0 -> presents choices
            Assert.AreEqual(new[] { SoundCue.UiClick }, played, "Finishing typing on a 1-line choice card should not re-trigger EventPrompt.");

            played.Clear();
            runner.SelectChoice("opt_a");
            CollectionAssert.Contains(played, SoundCue.UiClose);

            // 9. MissionWin via StoryDirector
            played.Clear();
            story.TriggerMissionEnd(MissionEndReason.EarlyWin, 3);
            CollectionAssert.Contains(played, SoundCue.MissionWin);

            // 10. Unsubscribe stops event forwarding
            bridge.Unsubscribe();
            played.Clear();
            solar.ToggleRetract();
            Assert.IsEmpty(played, "Unsubscribed SimulationAudioBridge must not fire cues.");
        }
    }
}
