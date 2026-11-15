using System;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>The rover and its 5 fixed expedition sites. Site ids match the scene's ResourceHotspot ids.</summary>
    [CreateAssetMenu(fileName = "ExpeditionSiteConfig", menuName = "Artemis X/Simulation/Expedition Site Config")]
    public class ExpeditionSiteConfig : ScriptableObject
    {
        [Serializable]
        public struct Site
        {
            [Tooltip("Matches ResourceHotspot.hotspotId in the scene.")]
            public string siteId;
            public string displayName;
            public SiteDistance distance;
            [Min(0)] public int powerCost;
            [Min(0)] public int waterReward;
            [Min(0)] public int partsReward;
            [Tooltip("Chance the rover comes back broken on a calm night. Design tiers: 0, 0.25 or 0.5.")]
            [Range(0f, 1f)] public float calmBreakdownChance;
        }

        public string roverName = "Luna";

        [Tooltip("Chance the rover comes back broken if a solar storm hits while it is out (GDD: 30%).")]
        [Range(0f, 1f)] public float stormBreakdownChance = 0.3f;

        public Site[] sites =
        {
            new Site { siteId = "Hotspot_Ice_NW",       displayName = "Frostline Hollow",   distance = SiteDistance.Near,   powerCost = 10, waterReward = 18, partsReward = 0,  calmBreakdownChance = 0f },
            new Site { siteId = "Hotspot_Ice_SW",       displayName = "Cryo Abyss",        distance = SiteDistance.Far,    powerCost = 18, waterReward = 32, partsReward = 0,  calmBreakdownChance = 0.5f },
            new Site { siteId = "Hotspot_Minerals_SSE", displayName = "Gleam Gully",       distance = SiteDistance.Near,   powerCost = 10, waterReward = 0,  partsReward = 8,  calmBreakdownChance = 0f },
            new Site { siteId = "Hotspot_Minerals_NE",  displayName = "Titan's Vein",      distance = SiteDistance.Far,    powerCost = 18, waterReward = 0,  partsReward = 16, calmBreakdownChance = 0.5f },
            new Site { siteId = "Hotspot_Rocks_East",   displayName = "Crossroads Basin",  distance = SiteDistance.Medium, powerCost = 14, waterReward = 16, partsReward = 8,  calmBreakdownChance = 0.25f },
        };

        public bool TryGetSite(string siteId, out Site site)
        {
            if (sites != null)
            {
                for (int i = 0; i < sites.Length; i++)
                {
                    if (string.Equals(sites[i].siteId, siteId, StringComparison.Ordinal))
                    {
                        site = sites[i];
                        return true;
                    }
                }
            }
            site = default;
            return false;
        }
    }
}
