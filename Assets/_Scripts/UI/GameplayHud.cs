using ArtemisX.Audio;
using ArtemisX.Simulation;
using ArtemisX.Story;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Hub for the gameplay scene UI: knows when world clicks are blocked, shows Bauzer tips, plays UI sounds and
    /// hides the bottom action buttons while Bauzer is talking.
    /// </summary>
    public class GameplayHud : MonoBehaviour
    {
        [SerializeField] private GameplayUIConfig config;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private DayResolutionController resolution;
        [SerializeField] private ContextConfirmPopupView popup;
        [SerializeField] private MorningSequenceController morning;
        [SerializeField] private RoverMapController roverMap;
        [SerializeField] private ResourceFlyFx flyFx;
        [SerializeField] private Visuals.DayNightTransitionPlayer transition;
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Hidden while a dialogue is open or the rover map is shown.")]
        [SerializeField] private CanvasGroup[] bottomActions = new CanvasGroup[0];
        [SerializeField] private Button helpButton;
        [Tooltip("Top bar groups hidden during the start screen and intro cutscene.")]
        [SerializeField] private CanvasGroup[] topBar = new CanvasGroup[0];

        private DialogueSequence tipSequence;

        public GameplayUIConfig Config => config;
        public ContextConfirmPopupView Popup => popup;
        public RoverMapController RoverMap => roverMap;
        public ResourceFlyFx FlyFx => flyFx;
        public MorningSequenceController Morning => morning;
        public Camera ViewCamera => viewCamera != null ? viewCamera : (viewCamera = Camera.main);

        /// <summary>True when a Kepler/story dialogue box is open.</summary>
        public bool IsDialogueOpen => runner != null && runner.IsOpen;

        /// <summary>Start screen / intro cutscene: hides the HUD and blocks world clicks.</summary>
        public bool Cinematic { get; set; }

        /// <summary>True when clicks on 3D objects must be ignored.</summary>
        public bool IsWorldInputBlocked =>
            Cinematic ||
            (runner != null && runner.IsOpen) ||
            (morning != null && morning.IsShowing) ||
            (roverMap != null && roverMap.IsOpen) ||
            (transition != null && transition.IsPlaying) ||
            (resolution != null && (resolution.IsResolving || resolution.IsMissionOver));

        /// <summary>True when a full-screen overlay or transition should hide floating HUD alerts.</summary>
        public bool IsOverlayCoveringHud =>
            Cinematic ||
            (morning != null && morning.IsShowing) ||
            (roverMap != null && roverMap.IsOpen) ||
            (transition != null && transition.IsPlaying) ||
            (resolution != null && (resolution.IsResolving || resolution.IsMissionOver));

        private void Awake()
        {
            if (helpButton != null) helpButton.onClick.AddListener(ShowHelp);
        }

        private void OnDestroy()
        {
            if (helpButton != null) helpButton.onClick.RemoveListener(ShowHelp);
            if (tipSequence != null) Destroy(tipSequence);
        }

        private void Update()
        {
            bool hide = Cinematic || (runner != null && runner.IsOpen) || (roverMap != null && roverMap.IsOpen);
            FadeGroups(topBar, Cinematic);
            for (int i = 0; i < bottomActions.Length; i++)
            {
                var group = bottomActions[i];
                if (group == null) continue;
                float alpha = hide ? 0f : 1f;
                if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = Mathf.MoveTowards(group.alpha, alpha, Time.unscaledDeltaTime * 8f);
                group.blocksRaycasts = !hide;
                group.interactable = !hide;
            }
        }

        private static void FadeGroups(CanvasGroup[] groups, bool hide)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group == null) continue;
                // Hide at once (no HUD flash behind the start screen), fade back in.
                group.alpha = hide ? 0f : Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime * 3f);
                group.blocksRaycasts = !hide;
            }
        }

        public void ShowHelp() => ShowTip(config != null ? config.helpText : string.Empty, SpeakerMood.Happy);

        /// <summary>One short Bauzer line in the dialogue box. Ignored while another dialogue is open.</summary>
        public void ShowTip(string text, SpeakerMood mood = SpeakerMood.Neutral)
        {
            if (runner == null || string.IsNullOrEmpty(text) || runner.IsOpen) return;
            if (popup != null) popup.Close();
            if (tipSequence == null)
            {
                tipSequence = ScriptableObject.CreateInstance<DialogueSequence>();
                tipSequence.name = "runtime_tip";
                tipSequence.sequenceId = "runtime_tip";
            }
            tipSequence.lines = new[]
            {
                new DialogueSequence.DialogueLine { speaker = config != null ? config.bauzer : null, mood = mood, text = text }
            };
            runner.Enqueue(tipSequence, null, DialogueQueueSlot.EventCard);
        }

        /// <summary>Bauzer explains why an action is blocked.</summary>
        public void ShowBlockedTip(ActionBlockReason reason)
        {
            if (AudioManager.Instance != null) AudioManager.Play(SoundCue.ActionBlocked);
            else PlaySfx(config != null ? config.errorClip : null);
            if (config == null) return;
            switch (reason)
            {
                case ActionBlockReason.None:
                case ActionBlockReason.InputLocked:
                    return;
                case ActionBlockReason.CrewRestricted: ShowTip(config.tipCrewRestricted, SpeakerMood.Worried); break;
                case ActionBlockReason.NotEnoughPower: ShowTip(config.tipNotEnoughPower, SpeakerMood.Worried); break;
                case ActionBlockReason.NotEnoughParts: ShowTip(config.tipNotEnoughParts, SpeakerMood.Worried); break;
                case ActionBlockReason.RoverOnMission: ShowTip(config.tipRoverOnMission); break;
                case ActionBlockReason.RoverBroken: ShowTip(config.tipRoverBroken, SpeakerMood.Worried); break;
                case ActionBlockReason.RoverLockedByRadiation: ShowTip(config.tipRoverLocked, SpeakerMood.Worried); break;
                default: ShowTip(config.tipGeneric); break;
            }
        }

        /// <summary>Fallback clip; skipped when the AudioManager is in the scene (its bridge plays the same moments).</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (AudioManager.Instance != null) return;
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }
    }
}
