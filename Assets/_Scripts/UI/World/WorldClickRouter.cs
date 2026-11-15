using ArtemisX.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArtemisX.UI
{
    /// <summary>Sends a click (press and release without panning, not over UI) to the WorldInteractable under the pointer.</summary>
    public class WorldClickRouter : MonoBehaviour
    {
        [SerializeField] private GameplayHud hud;
        [SerializeField] private CameraPanController panController;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private float maxDistance = 300f;

        /// <summary>Raised after a world object was clicked (used by the Day 1 onboarding).</summary>
        public static event System.Action<WorldInteractable> Clicked;

        private bool pressedOnUi;
        private bool wasPrimaryPressed;

        private void Update()
        {
            if (hud == null) return;
            var popup = hud.Popup;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && popup != null && popup.IsOpen)
            {
                popup.Close();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null) return;

            bool isPrimaryPressed = IsPrimaryPressed(pointer);
            if (isPrimaryPressed && !wasPrimaryPressed) pressedOnUi = PointerOverUi();
            else if (!isPrimaryPressed && wasPrimaryPressed) HandleRelease(pointer.position.ReadValue());
            wasPrimaryPressed = isPrimaryPressed;
        }

        private void HandleRelease(Vector2 screen)
        {
            if (pressedOnUi || PointerOverUi()) return;
            if (panController != null && panController.IsInteracting) return;
            if (hud.IsWorldInputBlocked) return;
            Click(screen);
        }

        /// <summary>Raycasts from a screen point and clicks what it hits. Public so tests can drive it.</summary>
        public bool Click(Vector2 screen)
        {
            var cam = hud.ViewCamera;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableMask))
            {
                var target = hit.collider.GetComponentInParent<WorldInteractable>();
                if (target != null)
                {
                    target.OnWorldClicked();
                    Clicked?.Invoke(target);
                    return true;
                }
            }
            if (hud.Popup != null && hud.Popup.IsOpen) hud.Popup.Close();
            return false;
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
