namespace ShelterCommand
{
    /// <summary>
    /// The three phases of a simulated in-game day.
    /// </summary>
    public enum DayPhase
    {
        /// <summary>06:00 → 07:00 — Player configures NPC tasks; NPCs are idle.</summary>
        PreWork,

        /// <summary>07:00 → 19:00 — NPCs execute their assigned tasks; production runs.</summary>
        Work,

        /// <summary>19:00 → 00:00 — NPCs return to common areas; social events may fire.</summary>
        PostWork,
    }
}
