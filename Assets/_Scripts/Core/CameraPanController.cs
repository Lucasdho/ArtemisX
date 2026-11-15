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
        private bool focusing;
        private float focusElapsed;
        private float focusDuration;
        private Vector3 focusFromTarget;
        private Vector3 focusLookTarget;
        private float focusFromDistance;
        private float focusToDistance;
        private System.Action onFocusArrived;

        /// <summary>True while actively dragging/tilting or immediately after a drag release on this frame.</summary>
        public bool IsInteracting => dragging || rightTilting || suppressNextClick;

        /// <summary>True while the camera is flying to a clicked object.</summary>
        public bool IsFocusing => focusing;

        private void Awake()
        {
            ApplyHomePose();
        }

        private void OnDisable()
        {
            // Another system took the camera (rover map, cutscene): finish so nobody waits forever.
            EndFocus(true);
        }

        /// <summary>Places the camera from the config pitch, yaw, distance and look target.</summary>
        [ContextMenu("Apply home pose")]
        public void ApplyHomePose()
        {
            if (config == null) return;
            EndFocus(false);
            lookTarget = config.homeLookTarget;
            yaw = config.yawDegrees;
            pitch = config.pitchDegrees;
            distance = targetDistance = config.distance;
            ApplyTransform();
        }

        /// <summary>
        /// Flies the camera from where it is to center a world point (shifted slightly down to leave room for a banner),
        /// then calls <paramref name="onArrived"/>. A new focus replaces the old one (its callback is dropped).
        /// </summary>
        public void FocusOn(Vector3 worldPoint, System.Action onArrived = null, float? zoomDistance = null)
        {
            if (config == null) { onArrived?.Invoke(); return; }
            onFocusArrived = null;
            velocity = Vector3.zero;
            tracking = false;
            dragging = false;

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 viewDir = rot * Vector3.forward;
            Vector3 groundPoint = worldPoint;
            if (Mathf.Abs(viewDir.y) > 0.001f)
            {
                float t = (config.homeLookTarget.y - worldPoint.y) / viewDir.y;
                groundPoint = worldPoint + viewDir * t;
            }
            groundPoint.y = config.homeLookTarget.y;

            Vector3 forwardXZ = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            forwardXZ.y = 0f;
            if (forwardXZ.sqrMagnitude > 0.0001f) forwardXZ.Normalize();

            float desiredZoom = zoomDistance ?? config.focusDistance;

            focusFromTarget = lookTarget;
            focusLookTarget = ClampLookTarget(groundPoint + forwardXZ * config.focusForwardOffset);
            focusFromDistance = distance;
            focusToDistance = desiredZoom > 0f
                ? Mathf.Clamp(desiredZoom, config.distanceRange.x, config.distanceRange.y)
                : targetDistance;

            // Already there: show the menu right away.
            bool nearTarget = (focusLookTarget - focusFromTarget).sqrMagnitude < 0.01f;
            bool nearZoom = Mathf.Abs(focusToDistance - focusFromDistance) < 0.1f;
            if (nearTarget && nearZoom || config.focusSeconds <= 0f)
            {
                lookTarget = focusLookTarget;
                distance = targetDistance = focusToDistance;
                focusing = false;
                ApplyTransform();
                onArrived?.Invoke();
                return;
            }

            focusElapsed = 0f;
            focusDuration = config.focusSeconds;
            onFocusArrived = onArrived;
            focusing = true;
        }

        /// <summary>
        /// Smoothly zooms out to the home look target at the overview distance so the whole base is visible,
        /// then calls <paramref name="onArrived"/>. Any player pan/zoom interrupts it (the callback still runs).
        /// </summary>
        public void ZoomOutToBase(System.Action onArrived = null)
        {
            if (config == null) { onArrived?.Invoke(); return; }
            onFocusArrived = null;
            velocity = Vector3.zero;
            tracking = false;
            dragging = false;

            focusFromTarget = lookTarget;
            focusLookTarget = ClampLookTarget(config.homeLookTarget);
            focusFromDistance = distance;
            focusToDistance = Mathf.Clamp(config.overviewDistance, config.distanceRange.x, config.distanceRange.y);

            bool nearTarget = (focusLookTarget - focusFromTarget).sqrMagnitude < 0.01f;
            bool nearZoom = Mathf.Abs(focusToDistance - focusFromDistance) < 0.1f;
            if (nearTarget && nearZoom || config.overviewSeconds <= 0f)
            {
                lookTarget = focusLookTarget;
                distance = targetDistance = focusToDistance;
                focusing = false;
                ApplyTransform();
                onArrived?.Invoke();
                return;
            }

            focusElapsed = 0f;
            focusDuration = config.overviewSeconds;
            onFocusArrived = onArrived;
            focusing = true;
        }

        /// <summary>Stops a running focus flight; optionally still runs its arrival callback.</summary>
        private void EndFocus(bool invokeCallback)
        {
            if (!focusing) { onFocusArrived = null; return; }
            focusing = false;
            targetDistance = distance;
            var callback = onFocusArrived;
            onFocusArrived = null;
            if (invokeCallback) callback?.Invoke();
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
            HandleFocus();

            if (!focusing)
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
                EndFocus(true);
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
                    EndFocus(true);
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
                    EndFocus(true);
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

        private void HandleFocus()
        {
            if (!focusing) return;
            focusElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(focusElapsed / Mathf.Max(0.0001f, focusDuration));
            float k = EaseInOutCubic(t);
            lookTarget = Vector3.LerpUnclamped(focusFromTarget, focusLookTarget, k);
            distance = targetDistance = Mathf.LerpUnclamped(focusFromDistance, focusToDistance, k);
            if (t >= 1f) EndFocus(true);
        }

        private static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

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
                EndFocus(true);
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
            lookTarget = ClampLookTarget(lookTarget + delta);
        }

        private Vector3 ClampLookTarget(Vector3 target)
        {
            target.x = Mathf.Clamp(
                target.x,
                config.homeLookTarget.x + config.panRangeX.x,
                config.homeLookTarget.x + config.panRangeX.y);
            target.z = Mathf.Clamp(
                target.z,
                config.homeLookTarget.z + config.panRangeZ.x,
                config.homeLookTarget.z + config.panRangeZ.y);
            return target;
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
