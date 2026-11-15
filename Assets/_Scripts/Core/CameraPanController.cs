using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArtemisX.Core
{
    /// <summary>
    /// Drag-to-pan for the base camera. Mouse drag and one-finger touch drag both grab the ground under the pointer
    /// and slide the camera over it. A short press that never passes the drag threshold is left alone so taps still click things.
    /// Put it on the Cinemachine camera object; the main camera follows that transform.
    /// </summary>
    public class CameraPanController : MonoBehaviour
    {
        [SerializeField] CameraConfig config;
        [SerializeField] Camera viewCamera;

        Vector3 home;
        Vector3 grabPoint;
        Vector3 velocity;
        Vector2 pressStart;
        bool wasPressed, tracking, dragging;

        void Awake()
        {
            ApplyHomePose();
            home = transform.position;
        }

        /// <summary>Places the camera from the config pitch, distance and look target.</summary>
        [ContextMenu("Apply home pose")]
        public void ApplyHomePose()
        {
            if (config == null) return;
            var rotation = Quaternion.Euler(config.pitchDegrees, 0f, 0f);
            transform.SetPositionAndRotation(config.homeLookTarget - rotation * Vector3.forward * config.distance, rotation);
            home = transform.position;
        }

        void Update()
        {
            if (config == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            var pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed;
            if (pointer != null)
            {
                Vector2 screen = pointer.position.ReadValue();
                if (pressed && !wasPressed) Begin(screen);
                else if (pressed && tracking) Drag(screen);
                else if (!pressed && wasPressed) End();
            }
            wasPressed = pressed;

            if (!dragging && velocity.sqrMagnitude > config.stopSpeed * config.stopSpeed)
            {
                Move(velocity * Time.unscaledDeltaTime);
                velocity *= Mathf.Exp(-config.inertiaDamping * Time.unscaledDeltaTime);
            }
            else if (!dragging) velocity = Vector3.zero;
        }

        void Begin(Vector2 screen)
        {
            tracking = !PointerOverUi();
            dragging = false;
            velocity = Vector3.zero;
            pressStart = screen;
        }

        void Drag(Vector2 screen)
        {
            if (!dragging)
            {
                if ((screen - pressStart).magnitude < config.dragThresholdPixels) return;
                if (!TryGround(screen, out grabPoint)) return;
                dragging = true;
            }

            if (!TryGround(screen, out Vector3 now)) return;
            Vector3 delta = grabPoint - now;
            delta.y = 0f;
            Vector3 before = transform.position;
            Move(delta);
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            velocity = Vector3.Lerp(velocity, (transform.position - before) / dt, 0.35f);
        }

        void End()
        {
            tracking = false;
            dragging = false;
        }

        void Move(Vector3 delta)
        {
            Vector3 p = transform.position + delta;
            p.x = Mathf.Clamp(p.x, home.x + config.panRangeX.x, home.x + config.panRangeX.y);
            p.z = Mathf.Clamp(p.z, home.z + config.panRangeZ.x, home.z + config.panRangeZ.y);
            transform.position = p;
        }

        bool TryGround(Vector2 screen, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = viewCamera.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float t)) { point = ray.GetPoint(t); return true; }
            point = default;
            return false;
        }

        static bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                return EventSystem.current.IsPointerOverGameObject(touch.primaryTouch.touchId.ReadValue());
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
