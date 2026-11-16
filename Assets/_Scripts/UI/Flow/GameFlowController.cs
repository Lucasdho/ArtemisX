using System.Collections;
using ArtemisX.Core;
using ArtemisX.Story;
using ArtemisX.UI.StartScreen;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Screen flow for HandMadeBase: Start Screen -> intro cutscene (camera orbits the lander while Bauzer plays
    /// Seq_Intro, skippable) -> Day 1 onboarding -> free play. Play Again reloads the scene and skips the intro.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private ScreensConfig config;
        [SerializeField] private GameplayUIConfig uiConfig;
        [SerializeField] private PlayerRunProfile profile;
        [SerializeField] private StartScreenView startScreen;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private StoryDirector story;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private Day1Onboarding onboarding;
        [Tooltip("The camera rig the main camera follows (CM_BaseCamera).")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private CameraPanController panController;
        [Tooltip("What the intro camera orbits (the lander).")]
        [SerializeField] private Transform introFocus;
        [SerializeField] private GameObject skipRoot;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text skipLabel;

        /// <summary>Set by Play Again: the reloaded scene skips the intro cutscene and onboarding.</summary>
        private static bool replaying;

        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 landerRestPos;
        private Sequence cameraTween;
        private Tween landerTween;
        private bool introDialogueDone;
        private bool cutsceneRunning;
        private DialogueSequence replaySequence;

        public bool IsCutscenePlaying => cutsceneRunning;

        private void Awake()
        {
            if (introFocus != null) landerRestPos = introFocus.position;
            if (skipButton != null) skipButton.onClick.AddListener(SkipIntro);
            if (skipLabel != null && config != null) skipLabel.text = config.skipLabel;
            if (skipRoot != null) skipRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (skipButton != null) skipButton.onClick.RemoveListener(SkipIntro);
            if (startScreen != null) startScreen.OnMissionLaunchRequested -= OnLaunch;
            if (replaySequence != null) Destroy(replaySequence);
        }

        private void Start()
        {
            if (profile != null)
            {
                profile.RestoreHandoff();
                profile.ApplySpeakerPortrait();
                if (runner != null) runner.SetPlayerProfile(profile);
            }

            if (hud != null) hud.Cinematic = true;
            if (startScreen != null)
            {
                startScreen.OnMissionLaunchRequested += OnLaunch;
                startScreen.Show();
            }
            else
            {
                OnLaunch(profile != null ? profile.selectedCharacter : 0,
                         profile != null ? profile.playerNickname : null);
            }
        }

        private void OnLaunch(int character, string nickname)
        {
            if (replaying)
            {
                replaying = false;
                EndCinematic();
                PlayReplayLine();
                return;
            }
            StartCoroutine(PlayIntro());
        }

        /// <summary>Reloads the scene for a fresh run (start screen again, no intro cutscene or onboarding).</summary>
        public static void PlayAgain()
        {
            replaying = true;
            DOTween.KillAll();
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            if (scene.buildIndex < 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex);
        }

        private IEnumerator PlayIntro()
        {
            cutsceneRunning = true;
            introDialogueDone = false;
            if (skipRoot != null) skipRoot.SetActive(true);

            if (introFocus != null && config.landerStartHeight > 0f)
            {
                introFocus.position = landerRestPos + Vector3.up * config.landerStartHeight;
                landerTween = introFocus.DOMove(landerRestPos, config.landerDescentSeconds)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject);
            }

            if (cameraRig != null)
            {
                if (panController != null) panController.enabled = false;
                homePosition = cameraRig.position;
                homeRotation = cameraRig.rotation;

                Vector3 startFocus = (introFocus != null ? introFocus.position : Vector3.zero) + config.introFocusOffset;
                float yaw = homeRotation.eulerAngles.y;
                Pose(startFocus, yaw + config.introStartYawOffset, config.introStartPitch, config.introStartDistance, out Vector3 p0, out Quaternion r0);
                cameraRig.SetPositionAndRotation(p0, r0);

                float t = config.introOrbitSeconds;
                cameraTween = DOTween.Sequence().SetLink(gameObject)
                    .Append(DOVirtual.Float(0f, 1f, t, k =>
                    {
                        // Orbit: interpolate yaw/pitch/distance around the descending lander instead of a straight line.
                        Vector3 focus = (introFocus != null ? introFocus.position : Vector3.zero) + config.introFocusOffset;
                        float yk = Mathf.Lerp(config.introStartYawOffset, config.introEndYawOffset, k);
                        float pk = Mathf.Lerp(config.introStartPitch, config.introEndPitch, k);
                        float dk = Mathf.Lerp(config.introStartDistance, config.introEndDistance, k);
                        Pose(focus, yaw + yk, pk, dk, out Vector3 p, out Quaternion r);
                        cameraRig.SetPositionAndRotation(p, r);
                    }).SetEase(Ease.InOutSine));
            }

            // The dialogue box must not cover the landing: wait until the lander has touched down (or the intro was skipped).
            yield return new WaitUntil(() => !cutsceneRunning || landerTween == null || !landerTween.IsActive() || landerTween.IsComplete());
            if (!cutsceneRunning) yield break; // skipped

            var intro = story != null && story.Schedule != null ? story.Schedule.introMilestone : null;
            if (runner != null && intro != null) runner.Enqueue(intro, () => introDialogueDone = true, DialogueQueueSlot.Intro);
            else introDialogueDone = true;

            yield return new WaitUntil(() => introDialogueDone || !cutsceneRunning);
            if (!cutsceneRunning) yield break; // skipped

            if (cameraRig != null)
            {
                cameraTween?.Kill();
                cameraTween = DOTween.Sequence().SetLink(gameObject)
                    .Join(cameraRig.DOMove(homePosition, config.introReturnSeconds).SetEase(Ease.InOutCubic))
                    .Join(cameraRig.DORotateQuaternion(homeRotation, config.introReturnSeconds).SetEase(Ease.InOutCubic));
                yield return cameraTween.WaitForCompletion();
            }
            FinishIntro();
        }

        /// <summary>Skip button: closes Bauzer's intro, snaps the camera home and starts the onboarding.</summary>
        public void SkipIntro()
        {
            if (!cutsceneRunning) return;
            cutsceneRunning = false;
            cameraTween?.Kill();
            landerTween?.Kill();
            if (runner != null && runner.IsOpen && story != null && story.Schedule != null && runner.CurrentSequence == story.Schedule.introMilestone)
                runner.SkipCurrentSequence();
            FinishIntro();
        }

        private void FinishIntro()
        {
            cutsceneRunning = false;
            landerTween?.Kill();
            if (introFocus != null) introFocus.position = landerRestPos;
            if (cameraRig != null) cameraRig.SetPositionAndRotation(homePosition, homeRotation);
            EndCinematic();
            if (onboarding != null) onboarding.Begin();
        }

        private void EndCinematic()
        {
            if (skipRoot != null) skipRoot.SetActive(false);
            if (panController != null) panController.enabled = true;
            if (hud != null) hud.Cinematic = false;
        }

        private void PlayReplayLine()
        {
            if (runner == null || config == null || string.IsNullOrEmpty(config.replayLine)) return;
            replaySequence = ScriptableObject.CreateInstance<DialogueSequence>();
            replaySequence.name = "runtime_replay";
            replaySequence.sequenceId = "replay";
            replaySequence.lines = new[]
            {
                new DialogueSequence.DialogueLine { speaker = uiConfig != null ? uiConfig.bauzer : null, mood = SpeakerMood.Happy, text = config.replayLine }
            };
            runner.Enqueue(replaySequence, null, DialogueQueueSlot.Intro);
        }

        private static void Pose(Vector3 focus, float yaw, float pitch, float distance, out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            position = focus - rotation * Vector3.forward * distance;
        }
    }
}
