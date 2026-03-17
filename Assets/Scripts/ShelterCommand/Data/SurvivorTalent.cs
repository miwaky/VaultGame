using System.Collections.Generic;

namespace ShelterCommand
{
    /// <summary>
    /// Special skills a survivor can possess (1 to 3 per survivor).
    /// Each talent grants a specific gameplay bonus used by SurvivorStatEvaluator.
    /// </summary>
    public enum SurvivorTalent
    {
        Bricoleur,    // +30% repair efficiency (Technique actions)
        Explorateur,  // +20% exploration event success rate
        Medecin,      // more effective healing actions
        Agriculteur,  // improved food production
        Hydrologue,   // improved water production
        Tireur,       // better combat efficiency (Force actions)
        Eclaireur,    // reduced exploration time
        Leader,       // increases group morale
    }

    /// <summary>
    /// Provides French labels, descriptions and stat multipliers for each talent.
    /// </summary>
    public static class TalentTable
    {
        // ── Labels ────────────────────────────────────────────────────────────────

        /// <summary>Returns a French display label for the given talent.</summary>
        public static string GetLabel(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Bricoleur   => "Bricoleur",
            SurvivorTalent.Explorateur => "Explorateur",
            SurvivorTalent.Medecin     => "Médecin",
            SurvivorTalent.Agriculteur => "Agriculteur",
            SurvivorTalent.Hydrologue  => "Hydrologue",
            SurvivorTalent.Tireur      => "Tireur d'élite",
            SurvivorTalent.Eclaireur   => "Éclaireur",
            SurvivorTalent.Leader      => "Leader",
            _                          => "Inconnu",
        };

        /// <summary>Returns a short French description of the bonus granted by the talent.</summary>
        public static string GetDescription(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Bricoleur   => "+30% efficacité réparations",
            SurvivorTalent.Explorateur => "+20% réussite événements exploration",
            SurvivorTalent.Medecin     => "Soins plus efficaces",
            SurvivorTalent.Agriculteur => "Production nourriture améliorée",
            SurvivorTalent.Hydrologue  => "Production eau améliorée",
            SurvivorTalent.Tireur      => "Meilleure efficacité au combat",
            SurvivorTalent.Eclaireur   => "Temps d'exploration réduit",
            SurvivorTalent.Leader      => "Augmente le moral du groupe",
            _                          => string.Empty,
        };

        // ── Multipliers used by SurvivorStatEvaluator ─────────────────────────────

        /// <summary>
        /// Returns the exploration success chance bonus (0.0–1.0) granted by a talent.
        /// Example: Explorateur returns 0.20f for +20%.
        /// </summary>
        public static float GetExplorationBonus(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Explorateur => 0.20f,
            SurvivorTalent.Eclaireur   => 0.10f,
            _                          => 0f,
        };

        /// <summary>
        /// Returns the Technique action efficiency multiplier bonus (0.0–1.0) for repair contexts.
        /// </summary>
        public static float GetRepairBonus(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Bricoleur => 0.30f,
            _                        => 0f,
        };

        /// <summary>
        /// Returns the Force action efficiency multiplier bonus (0.0–1.0) for combat contexts.
        /// </summary>
        public static float GetCombatBonus(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Tireur => 0.25f,
            _                     => 0f,
        };

        /// <summary>
        /// Returns the production bonus multiplier (0.0–1.0) for food production.
        /// </summary>
        public static float GetFoodProductionBonus(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Agriculteur => 0.25f,
            _                          => 0f,
        };

        /// <summary>
        /// Returns the production bonus multiplier (0.0–1.0) for water production.
        /// </summary>
        public static float GetWaterProductionBonus(SurvivorTalent talent) => talent switch
        {
            SurvivorTalent.Hydrologue => 0.25f,
            _                         => 0f,
        };

        /// <summary>
        /// Pools talents that can be assigned during generation,
        /// weighted so every talent has equal probability.
        /// </summary>
        public static readonly SurvivorTalent[] AllTalents =
        {
            SurvivorTalent.Bricoleur,
            SurvivorTalent.Explorateur,
            SurvivorTalent.Medecin,
            SurvivorTalent.Agriculteur,
            SurvivorTalent.Hydrologue,
            SurvivorTalent.Tireur,
            SurvivorTalent.Eclaireur,
            SurvivorTalent.Leader,
        };

        // ── Profession affinity ───────────────────────────────────────────────────

        /// <summary>
        /// Returns talents that have a strong affinity with the given profession.
        /// Used by the generator to increase the probability of coherent talent assignment.
        /// </summary>
        public static IReadOnlyList<SurvivorTalent> GetAffinityTalents(SurvivorProfession profession)
        {
            return profession switch
            {
                SurvivorProfession.BTP        => new[] { SurvivorTalent.Bricoleur },
                SurvivorProfession.Mecanicien => new[] { SurvivorTalent.Bricoleur },
                SurvivorProfession.Electricien=> new[] { SurvivorTalent.Bricoleur },
                SurvivorProfession.Militaire  => new[] { SurvivorTalent.Tireur, SurvivorTalent.Explorateur },
                SurvivorProfession.Agriculteur=> new[] { SurvivorTalent.Agriculteur },
                SurvivorProfession.Medecin    => new[] { SurvivorTalent.Medecin },
                SurvivorProfession.Infirmier  => new[] { SurvivorTalent.Medecin },
                SurvivorProfession.Enseignant => new[] { SurvivorTalent.Leader },
                SurvivorProfession.Journaliste=> new[] { SurvivorTalent.Eclaireur, SurvivorTalent.Explorateur },
                SurvivorProfession.Athlete    => new[] { SurvivorTalent.Explorateur, SurvivorTalent.Eclaireur },
                _ => System.Array.Empty<SurvivorTalent>(),
            };
        }
    }
}
