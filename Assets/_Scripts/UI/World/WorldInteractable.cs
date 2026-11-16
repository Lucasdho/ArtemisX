using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Base for 3D objects the player can click in the gameplay scene (needs a collider on it or a child).</summary>
    public abstract class WorldInteractable : MonoBehaviour
    {
        [SerializeField] protected GameplayHud hud;
        [SerializeField] private CameraPanController panController;
        [Tooltip("Extra height above the collider top where the pop-up card is anchored.")]
        [SerializeField] private float anchorPadding = 0.3f;

        private Collider anchorCollider;

        /// <summary>Top center of the collider (or the transform), where pop-up cards point to.</summary>
        public Vector3 AnchorWorldPosition
        {
            get
            {
                if (anchorCollider == null) anchorCollider = GetComponentInChildren<Collider>();
                if (anchorCollider == null) return transform.position + Vector3.up * (1.5f + anchorPadding);
                Bounds b = anchorCollider.bounds;
                return new Vector3(b.center.x, b.max.y + anchorPadding, b.center.z);
            }
        }

        protected virtual void Awake()
        {
            if (hud == null) hud = FindAnyObjectByType<GameplayHud>();
            if (panController == null) panController = FindAnyObjectByType<CameraPanController>();
        }

        /// <summary>The player clicked this object (not a drag) while world input is allowed.</summary>
        public abstract void OnWorldClicked();

        /// <summary>Flies the camera to this object, then runs <paramref name="onArrived"/> (immediately if there is no camera rig).</summary>
        protected void FocusCamera(System.Action onArrived = null)
        {
            if (panController == null) panController = FindAnyObjectByType<CameraPanController>();
            if (panController != null && panController.isActiveAndEnabled) panController.FocusOn(AnchorWorldPosition, onArrived);
            else onArrived?.Invoke();
        }

        /// <summary>Flies the camera to this object first, then shows the card once it arrives.</summary>
        protected void OpenCard(string title, string description, string confirmLabel, bool canConfirm,
            System.Action confirm, ContextConfirmPopupView.Cost[] costs)
        {
            if (hud == null || hud.Popup == null) return;
            var popup = hud.Popup;
            int ticket = popup.BeginPending();
            FocusCamera(() =>
            {
                // Skipped if the player closed it (Esc / clicked empty ground) or clicked something else meanwhile.
                if (!popup.IsPendingFor(ticket)) return;
                popup.Open(() => AnchorWorldPosition, title, description, confirmLabel, canConfirm, confirm, costs, hud.ViewCamera);
            });
        }
    }
}
