namespace ShelterCommand
{
    /// <summary>
    /// Daily tasks assignable to a survivor via the schedule panel.
    /// Each task maps to a room destination via RoomNavigationConfig.
    /// Reset every morning by ScheduleManager.
    /// </summary>
    public enum DailyTask
    {
        SeReposer,
        SoccuperDeLeau,
        Soigner,
        SeFaireSoigner,
        SoccuperDeLaFerme,
        SoccuperDuStockage,
    }

    /// <summary>
    /// French display labels for DailyTask values.
    /// </summary>
    public static class DailyTaskLabels
    {
        /// <summary>Returns the French display label for a task.</summary>
        public static string GetLabel(DailyTask task) => task switch
        {
            DailyTask.SeReposer            => "Se reposer",
            DailyTask.SoccuperDeLeau       => "S'occuper de l'eau",
            DailyTask.Soigner              => "Soigner",
            DailyTask.SeFaireSoigner       => "Se faire soigner",
            DailyTask.SoccuperDeLaFerme    => "S'occuper de la ferme",
            DailyTask.SoccuperDuStockage   => "S'occuper du stockage",
            _                              => "Inconnu",
        };

        /// <summary>Returns all DailyTask values (used to populate the task cycle buttons).</summary>
        public static DailyTask[] All => (DailyTask[])System.Enum.GetValues(typeof(DailyTask));

        /// <summary>Count of available tasks.</summary>
        public static int Count => All.Length;
    }
}
