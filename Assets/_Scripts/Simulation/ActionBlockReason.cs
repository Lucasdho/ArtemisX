namespace ArtemisX.Simulation
{
    /// <summary>Why a player action can't happen right now. UI uses it to pick a Bauzer tip.</summary>
    public enum ActionBlockReason
    {
        None,
        InputLocked,
        CrewRestricted,
        NotEnoughPower,
        NotEnoughParts,
        RoverOnMission,
        RoverBroken,
        RoverLockedByRadiation,
        UnknownSite,
        AllBuilt,
        NothingToRepair,
        NothingToCollect,
        WrongWeather,
        AlreadyChosen,
        NoActiveDemand,
        /// <summary>An Unexpected Demand must be answered before End Day.</summary>
        DemandPending
    }
}
