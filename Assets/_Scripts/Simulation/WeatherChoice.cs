namespace ArtemisX.Simulation
{
    /// <summary>The player's answer on the Space Weather Choice card.</summary>
    public enum WeatherChoice
    {
        /// <summary>Solar Storm: retract the exposed solar field (safe, less Power).</summary>
        ShieldAndRetract,
        /// <summary>Solar Storm: keep the field Active (full Power, may break).</summary>
        KeepFullPower,
        /// <summary>Solar Radiation: rover stays docked today, no damage.</summary>
        ShelterCrewAndLockRover,
        /// <summary>Solar Radiation: pay Power now, rover may go out today.</summary>
        OverclockShields
    }
}
