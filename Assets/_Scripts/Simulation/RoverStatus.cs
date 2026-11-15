namespace ArtemisX.Simulation
{
    /// <summary>State of the base's single rover.</summary>
    public enum RoverStatus
    {
        Ready,
        OnMission,
        Broken,
        /// <summary>Docked for today because of a Solar Radiation day (crew sheltered).</summary>
        LockedByRadiation
    }
}
