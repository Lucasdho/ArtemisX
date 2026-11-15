using UnityEngine;

namespace ArtemisX.Core
{
    /// <summary>Tunable numbers for the base camera and its drag-to-pan. One asset lives in Assets/_Config/.</summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Artemis X/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Home pose")]
        [Tooltip("Ground point the camera looks at when the game starts.")]
        public Vector3 homeLookTarget = new Vector3(0f, 0f, 1f);
        [Range(20f, 80f)] public float pitchDegrees = 42f;
        [Min(1f)] public float distance = 21f;

        [Header("Pan")]
        [Min(0f)] public float dragThresholdPixels = 10f;
        [Tooltip("How fast the coast after a flick fades out. Higher stops sooner.")]
        [Min(0.1f)] public float inertiaDamping = 7f;
        [Min(0f)] public float stopSpeed = 0.05f;
        [Tooltip("How far the camera may move from home, in world units: x = left/right, y = back/forward.")]
        public Vector2 panRangeX = new Vector2(-15f, 15f);
        public Vector2 panRangeZ = new Vector2(-9f, 13f);
    }
}
