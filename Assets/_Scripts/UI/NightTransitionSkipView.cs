using ArtemisX.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Shown during the Next Day transition: a full-screen invisible button that only keeps clicks off the base and
    /// the HUD (it does not skip), plus a small Skip button, the only way to skip.
    /// </summary>
    public class NightTransitionSkipView : MonoBehaviour
    {
        [SerializeField] private DayNightTransitionPlayer transition;
        [SerializeField] private GameObject root;
        [SerializeField] private Button tapAnywhere;
        [SerializeField] private Button skipButton;

        private void Awake()
        {
            // tapAnywhere stays as a pure input blocker: only the Skip button skips.
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
        }

        private void OnEnable()
        {
            if (transition == null) return;
            transition.TransitionStarted += Show;
            transition.TransitionFinished += Hide;
        }

        private void OnDisable()
        {
            if (transition == null) return;
            transition.TransitionStarted -= Show;
            transition.TransitionFinished -= Hide;
        }

        private void Show()
        {
            if (root != null) root.SetActive(true);
        }

        private void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void Skip()
        {
            if (transition != null) transition.Skip();
        }
    }
}
