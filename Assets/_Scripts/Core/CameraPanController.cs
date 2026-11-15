using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArtemisX.Core
{
    /// <summary>
    /// Isometric 3/4 diorama camera controller supporting ground-drag pan, scroll/pinch zoom, and right-drag tilt/orbit.
    /// A short tap/click under the drag threshold is left alone so world objects remain clickable.
    /// Put it on the Cinemachine camera object; the main camera follows that transform.
    /// </summary>
    public class CameraPanController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [SerializeField] private Camera viewCamera;

        private Vector3 lookTarget;
        private Vector3 grabPoint;
        private Vector3 velocity;
        private Vector2 pressStart;
        private float yaw;
        private float pitch;
        private float distance;
        private float targetDistance;
        private float prevPinchDistance;
        private bool wasPressed;
        private bool tracking;
        private bool dragging;
        private bool rightTilting;
        private bool suppressNextClick;

        /// <summary>True while actively dragging/tilting or immediately after a drag release on this frame.</summary>
        public bool IsInteracting => dragging || rightTilting || suppressNextClick;

        private void Awake()
        {
            ApplyHomePose();
        }

        /// <summary>Places the camera from the config pitch, yaw, distance and look target.</summary>
        [ContextMenu("Apply home pose")]
        public void ApplyHomePose()
        {
            if (config == null) return;
            lookTarget = config.homeLookTarget;
            yaw = config.yawDegrees;
            pitch = config.pitchDegrees;
            distance = targetDistance = config.distance;
            ApplyTransform();
        }

        private void Update()
        {
            if (config == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            suppressNextClick = false;

            HandleRightTilt();
            HandleZoom();
            HandlePan();

            distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-config.zoomDamping * Time.unscaledDeltaTime));
            ApplyTransform();
        }

        private void HandleRightTilt()
        {
            var mouse = Mouse.current;
            bool rightPressed = mouse != null && mouse.rightButton.isPressed;
            if (rightPressed && !rightTilting && !PointerOverUi())
            {
                rightTilting = true;
                dragging = false;
                tracking = false;
                velocity = Vector3.zero;
            }
            else if (!rightPressed && rightTilting)
            {
                rightTilting = false;
                suppressNextClick = true;
            }

            if (!rightTilting || mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * config.tiltSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * config.tiltSensitivity, config.pitchRange.x, config.pitchRange.y);
        }

        private void HandleZoom()
        {
            if (PointerOverUi()) return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetDistance = Mathf.Clamp(
                        targetDistance - Mathf.Sign(scroll) * config.zoomSpeed,
                        config.distanceRange.x,
                        config.distanceRange.y);
                }
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count >= 2 &&
                touch.touches[0].press.isPressed && touch.touches[1].press.isPressed)
            {
                float currentPinch = Vector2.Distance(
                    touch.touches[0].position.ReadValue(),
                    touch.touches[1].position.ReadValue());
                if (prevPinchDistance > 0f)
                {
                    float delta = currentPinch - prevPinchDistance;
                    targetDistance = Mathf.Clamp(
                        targetDistance - delta * config.pinchZoomSpeed,
                        config.distanceRange.x,
                        config.distanceRange.y);
                }
                prevPinchDistance = currentPinch;
                tracking = false;
                dragging = false;
            }
            else
            {
                prevPinchDistance = 0f;
            }
        }

        private void HandlePan()
        {
            var pointer = Pointer.current;
            bool primaryPressed = !rightTilting && IsPrimaryPressed(pointer);
            if (pointer != null && prevPinchDistance <= 0f)
            {
                Vector2 screen = pointer.position.ReadValue();
                if (primaryPressed && !wasPressed) BeginPan(screen);
                else if (primaryPressed && tracking) DragPan(screen);
                else if (!primaryPressed && wasPressed) EndPan();
            }
            wasPressed = primaryPressed;

            if (!dragging && velocity.sqrMagnitude > config.stopSpeed * config.stopSpeed)
            {
                MoveLookTarget(velocity * Time.unscaledDeltaTime);
                velocity *= Mathf.Exp(-config.inertiaDamping * Time.unscaledDeltaTime);
            }
            else if (!dragging)
            {
                velocity = Vector3.zero;
            }
        }

        private void BeginPan(Vector2 screen)
        {
            tracking = !PointerOverUi();
            dragging = false;
            velocity = Vector3.zero;
            pressStart = screen;
        }

        private void DragPan(Vector2 screen)
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
            Vector3 before = lookTarget;
            MoveLookTarget(delta);
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            velocity = Vector3.Lerp(velocity, (lookTarget - before) / dt, 0.35f);
        }

        private void EndPan()
        {
            if (dragging) suppressNextClick = true;
            tracking = false;
            dragging = false;
        }

        private void MoveLookTarget(Vector3 delta)
        {
            lookTarget += delta;
            lookTarget.x = Mathf.Clamp(
                lookTarget.x,
                config.homeLookTarget.x + config.panRangeX.x,
                config.homeLookTarget.x + config.panRangeX.y);
            lookTarget.z = Mathf.Clamp(
                lookTarget.z,
                config.homeLookTarget.z + config.panRangeZ.x,
                config.homeLookTarget.z + config.panRangeZ.y);
        }

        private void ApplyTransform()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(lookTarget - rotation * Vector3.forward * distance, rotation);
        }

        private bool TryGround(Vector2 screen, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = viewCamera.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float t))
            {
                point = ray.GetPoint(t);
                return true;
            }
            point = default;
            return false;
        }

        private static bool IsPrimaryPressed(Pointer pointer)
        {
            if (pointer == null) return false;
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
