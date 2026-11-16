using ArtemisX.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArtemisX.Core
{
    /// <summary>Opens the contextual confirmation pop-up when clicking a ClickableBuilding without panning.</summary>
    public class BuildingClickInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private CameraPanController panController;
        [SerializeField] private ContextConfirmPopupView popup;
        [SerializeField] private LayerMask interactableMask = ~0;

        private bool pressedOnUi;
        private bool wasPrimaryPressed;

        private void Update()
        {
            if (popup == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && popup.IsOpen)
            {
                popup.Close();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null) return;

            bool isPrimaryPressed = IsPrimaryPressed(pointer);
            if (isPrimaryPressed && !wasPrimaryPressed)
            {
                pressedOnUi = PointerOverUi();
            }
            else if (!isPrimaryPressed && wasPrimaryPressed)
            {
                HandleClickRelease(pointer.position.ReadValue());
            }

            wasPrimaryPressed = isPrimaryPressed;
        }

        private void HandleClickRelease(Vector2 screenPosition)
        {
            if (pressedOnUi || PointerOverUi()) return;
            if (panController != null && panController.IsInteracting) return;

            Ray ray = viewCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 250f, interactableMask))
            {
                var clickable = hit.collider.GetComponentInParent<ClickableBuilding>();
                if (clickable != null)
                {
                    if (panController != null && panController.isActiveAndEnabled)
                    {
                        int ticket = popup.BeginPending();
                        panController.FocusOn(clickable.AnchorWorldPosition, () =>
                        {
                            if (popup.IsPendingFor(ticket)) popup.Open(clickable, viewCamera);
                        });
                    }
                    else popup.Open(clickable, viewCamera);
                    return;
                }
            }

            if (popup.IsOpen) popup.Close();
        }

        private static bool IsPrimaryPressed(Pointer pointer)
        {
            if (pointer is Mouse mouse) return mouse.leftButton.isPressed;
            return pointer.press.isPressed;
        }

        private static bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                return EventSystem.current.IsPointerOverGameObject(touch.primaryTouch.touchId.ReadValue());
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
