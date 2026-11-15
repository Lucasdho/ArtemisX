using UnityEngine;
using UnityEngine.Events;

namespace ArtemisX.Core
{
    /// <summary>Marks a base building or slot as clickable to open a contextual confirmation pop-up.</summary>
    public class ClickableBuilding : MonoBehaviour
    {
        [SerializeField] private string title = "Module";
        [TextArea(1, 3)]
        [SerializeField] private string description = "Inspect or construct this module?";
        [SerializeField] private string confirmLabel = "Confirm";
        [SerializeField] private float popupAnchorHeight = 1.6f;
        [SerializeField] private UnityEvent onConfirmed;

        public string Title => title;
        public string Description => description;
        public string ConfirmLabel => confirmLabel;
        public Vector3 AnchorWorldPosition => transform.position + Vector3.up * popupAnchorHeight;

        public void Confirm()
        {
            onConfirmed?.Invoke();
            Debug.Log($"[Base] Confirmed action on '{title}' ({name}).", this);
        }
    }
}
