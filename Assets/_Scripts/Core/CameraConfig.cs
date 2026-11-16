using UnityEngine;

namespace ArtemisX.Core
{
    /// <summary>Tunable numbers for the isometric 3/4 diorama camera: pan, zoom, and right-tilt. One asset lives in Assets/_Config/.</summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Artemis X/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Home pose (3/4 Diorama)")]
        [Tooltip("Ground point the camera looks at when the game starts.")]
        public Vector3 homeLookTarget = new Vector3(0.5f, 0f, 0.5f);
        [Range(15f, 75f)] public float pitchDegrees = 36f;
        [Range(-180f, 180f)] public float yawDegrees = -32f;
        [Min(1f)] public float distance = 28f;

        [Header("Pan")]
        [Min(0f)] public float dragThresholdPixels = 10f;
        [Tooltip("How fast the coast after a flick fades out. Higher stops sooner.")]
        [Min(0.1f)] public float inertiaDamping = 7f;
        [Min(0f)] public float stopSpeed = 0.05f;
        [Tooltip("How far the look target may move from home, in world units: x = left/right, y = back/forward.")]
        public Vector2 panRangeX = new Vector2(-18f, 18f);
        public Vector2 panRangeZ = new Vector2(-16f, 18f);

        [Header("Zoom")]
        public Vector2 distanceRange = new Vector2(12f, 42f);
        [Min(0.1f)] public float zoomSpeed = 2.4f;
        [Min(1f)] public float zoomDamping = 10f;
        [Min(0.001f)] public float pinchZoomSpeed = 0.03f;

        [Header("Right-Tilt")]
        public Vector2 pitchRange = new Vector2(22f, 62f);
        [Min(0.01f)] public float tiltSensitivity = 0.22f;

        [Header("Focus")]
        [Tooltip("Seconds for the camera to fly to a clicked component before its menu opens. 0 = snap.")]
        [Min(0f)] public float focusSeconds = 0.6f;
        [Tooltip("Zoom distance when focusing an object. Set 0 to keep current zoom.")]
        [Min(0f)] public float focusDistance = 22f;
        [Tooltip("Shifts the camera look target forward along camera yaw so the object sits slightly below center, leaving room for its banner above.")]
        public float focusForwardOffset = 1.8f;

        [Header("Base overview")]
        [Tooltip("Zoom distance used to show the whole base (building completed, End Day).")]
        [Min(1f)] public float overviewDistance = 36f;
        [Tooltip("Seconds the camera takes to zoom out to the base overview.")]
        [Min(0f)] public float overviewSeconds = 1.2f;

        [Header("End Day view")]
        [Tooltip("Extra yaw (degrees) applied while zooming out for the End Day animation.")]
        [Range(-45f, 45f)] public float endDayYawOffset = 12f;
        [Tooltip("Shifts the look point in camera space during End Day: x = screen right (negative = left), y = forward.")]
        public Vector2 endDayLookOffset = new Vector2(-3f, 0f);
    }
}
