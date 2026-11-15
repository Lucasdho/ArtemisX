using ArtemisX.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Shown during the Next Day transition: a full-screen invisible button (tap anywhere to skip, and it keeps clicks
    /// off the base and the HUD) plus a small Skip button.
    /// </summary>
    public class NightTransitionSkipView : MonoBehaviour
    {
        [SerializeField] private DayNightTransitionPlayer transition;
        [SerializeField] private GameObject root;
        [SerializeField] private Button tapAnywhere;
        [SerializeField] private Button skipButton;

        private void Awake()
        {
            if (tapAnywhere != null) tapAnywhere.onClick.AddListener(Skip);
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (tapAnywhere != null) tapAnywhere.onClick.RemoveListener(Skip);
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
