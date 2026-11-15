using System;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Configurable placement and composition of one resource gathering hot spot.</summary>
    [Serializable]
    public struct HotspotEntry
    {
        public string id;
        public string displayName;
        public HotspotResourceKind primaryResource;
        public Vector2 center;
        [Min(0.5f)] public float depositRadius;
        [Min(0.8f)] public float roverStopDistance;
        [Min(1)] public int primaryNodeCount;
        [Min(0)] public int borderRockCount;
    }
}
