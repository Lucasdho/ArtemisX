using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Marks a lunar resource gathering hot spot where rovers can be dispatched.</summary>
    public class ResourceHotspot : MonoBehaviour
    {
        public string hotspotId;
        public string displayName;
        public HotspotResourceKind primaryResource;
        [Tooltip("World point and facing where a dispatched rover parks to gather from this hot spot.")]
        public Transform roverStopPoint;
        [Min(0.5f)] public float gatherRadius = 2.6f;
    }
}
