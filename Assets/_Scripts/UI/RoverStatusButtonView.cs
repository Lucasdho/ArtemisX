using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>HUD Rover Status button: shows Ready / On Mission / Broken / Locked; the only way to operate the rover.</summary>
    public class RoverStatusButtonView : MonoBehaviour
    {
        [SerializeField] private RoverExpeditionManager rover;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private RoverInteractable roverInteractable;
        [SerializeField] private UIStyle style;
        [SerializeField] private Button button;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Graphic statusDot;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void OnEnable()
        {
            if (rover != null) rover.RoverStatusChanged += OnStatusChanged;
            if (resources != null) resources.CrewRestrictionChanged += OnRestriction;
        }

        private void OnDisable()
        {
            if (rover != null) rover.RoverStatusChanged -= OnStatusChanged;
            if (resources != null) resources.CrewRestrictionChanged -= OnRestriction;
        }

        private void Start() => Refresh();
        private void OnStatusChanged(RoverStatus _) => Refresh();
        private void OnRestriction(bool _, Core.ResourceType __) => Refresh();

        private void Refresh()
        {
            if (rover == null) return;
            string text;
            Color color;
            var status = rover.Status;
            bool restricted = resources != null && resources.IsCrewRestricted && status == RoverStatus.Ready;
            if (restricted || status == RoverStatus.LockedByRadiation) { text = "Locked"; color = new Color(0.55f, 0.55f, 0.6f); }
            else if (status == RoverStatus.OnMission) { text = "On Mission"; color = style != null ? style.progressColor : Color.cyan; }
            else if (status == RoverStatus.Broken) { text = "Broken"; color = style != null ? style.negativeColor : Color.red; }
            else { text = "Ready"; color = style != null ? style.doneColor : Color.green; }

            if (statusLabel != null) statusLabel.text = text;
            if (statusDot != null) statusDot.color = color;
        }

        private void OnClicked()
        {
            if (roverInteractable != null) roverInteractable.Activate();
        }
    }
}
