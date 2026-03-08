namespace ShelterCommand
{
    /// <summary>
    /// All possible orders the player can issue to a survivor.
    /// </summary>
    public enum OrderType
    {
        // Work
        RepairGenerator,
        TransportResources,
        CraftTools,

        // Needs
        GoEat,
        GoSleep,
        GoToInfirmary,

        // Security
        ArrestSurvivor,
        PatrolZone
    }
}
