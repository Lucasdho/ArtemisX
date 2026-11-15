using System;
using ArtemisX.Core;

namespace ArtemisX.Simulation
{
    /// <summary>Yesterday's summary for the morning Daily Report card (and the final night of the run).</summary>
    [Serializable]
    public class DailyReportData
    {
        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;

        /// <summary>The day that just ended.</summary>
        public int day;
        /// <summary>The day that starts with this report (day + 1), or day if the run ended.</summary>
        public int newDay;
        /// <summary>The DONKI event that hit during the night.</summary>
        public WeatherEventType nightWeather;
        public bool stormHit;
        public bool solarBroken;
        public bool crewRestricted;
        public bool missionEnded;

        /// <summary>Change of every resource since the night started, rover haul included (after - before, clamped).</summary>
        public int[] resourceDeltas = new int[ResourceTypeCount];
        /// <summary>Passive production minus consumption applied this night (before clamping).</summary>
        public int[] passiveDeltas = new int[ResourceTypeCount];

        public RoverExpeditionManager.ReturnResult rover;

        public bool supplyArrived;
        public int supplyFood;
        public int supplyParts;

        /// <summary>Weather for the new day (the Space Weather Choice card shows when not Calm).</summary>
        public WeatherEventType todayWeather;
        public bool demandRolled;

        /// <summary>
        /// Key for Bauzer's short comment under the report: mission_end, crew_restricted, solar_broken, rover_broken,
        /// supply_arrived, rover_haul, storm_safe, calm.
        /// </summary>
        public string bauzerSummaryKey = "calm";

        public int GetDelta(ResourceType type) => resourceDeltas[(int)type];
        public bool AnythingBroken => solarBroken || (rover.returned && rover.isBroken);
    }
}
