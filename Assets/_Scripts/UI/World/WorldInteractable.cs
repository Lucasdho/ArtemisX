using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Base for 3D objects the player can click in the gameplay scene (needs a collider on it or a child).</summary>
    public abstract class WorldInteractable : MonoBehaviour
    {
        [SerializeField] protected GameplayHud hud;
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
        }

        /// <summary>The player clicked this object (not a drag) while world input is allowed.</summary>
        public abstract void OnWorldClicked();

        protected void OpenCard(string title, string description, string confirmLabel, bool canConfirm,
            System.Action confirm, ContextConfirmPopupView.Cost[] costs)
        {
            if (hud == null || hud.Popup == null) return;
            hud.Popup.Open(() => AnchorWorldPosition, title, description, confirmLabel, canConfirm, confirm, costs, hud.ViewCamera);
        }
    }
}
