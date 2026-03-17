using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Centralized evaluation hub for survivor stats, talents and traits.
    ///
    /// All methods are static and accept either a single profile or a list,
    /// so they can be used during exploration events, resource calculations, etc.
    /// </summary>
    public static class SurvivorStatEvaluator
    {
        // ── Stat checks ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the best value of the given stat across all provided profiles.
        /// Useful for group checks where the strongest survivor determines success.
        /// </summary>
        public static int BestStat(IEnumerable<SurvivorGeneratedProfile> profiles, SurvivorStatIndex stat)
        {
            int best = 0;
            foreach (SurvivorGeneratedProfile p in profiles)
                if (p != null) best = Mathf.Max(best, p.GetStat(stat));
            return best;
        }

        /// <summary>
        /// Returns the average value of the given stat across all provided profiles.
        /// Useful for group tasks where every member contributes equally.
        /// </summary>
        public static float AverageStat(IEnumerable<SurvivorGeneratedProfile> profiles, SurvivorStatIndex stat)
        {
            int total = 0;
            int count = 0;
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;
                total += p.GetStat(stat);
                count++;
            }
            return count > 0 ? (float)total / count : 0f;
        }

        // ── Exploration ───────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the overall exploration success chance (0.0–1.0) for a group.
        ///
        /// Base formula:
        ///   (Intelligence + Technique + Endurance) / 300  → clamped to [0, 1]
        ///
        /// Modifiers applied on top:
        ///   • Explorateur talent  +20%
        ///   • Eclaireur talent    +10%
        ///   • Courageux trait     +10%
        ///   • Peureux trait       -15%
        ///   • Impulsif trait      -10%
        /// </summary>
        public static float ComputeExplorationSuccessChance(IReadOnlyList<SurvivorGeneratedProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0) return 0f;

            float intel    = AverageStat(profiles, SurvivorStatIndex.Intelligence);
            float tech     = AverageStat(profiles, SurvivorStatIndex.Technique);
            float end      = AverageStat(profiles, SurvivorStatIndex.Endurance);
            float baseRate = (intel + tech + end) / 300f;

            float bonus = 0f;

            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;

                // Talent bonuses
                foreach (SurvivorTalent talent in p.Talents)
                    bonus += TalentTable.GetExplorationBonus(talent);

                // Positive trait bonuses
                if (p.positiveTrait == PositiveTrait.Courageux)
                    bonus += 0.10f;
                if (p.positiveTrait == PositiveTrait.Strategique)
                    bonus += 0.05f;

                // Negative trait penalties
                if (p.negativeTrait == NegativeTrait.Peureux)
                    bonus -= 0.15f;
                if (p.negativeTrait == NegativeTrait.Impulsif)
                    bonus -= 0.10f;
                if (p.negativeTrait == NegativeTrait.Pessimiste)
                    bonus -= 0.05f;
            }

            // Normalize talent/trait modifiers across group size
            bonus /= profiles.Count;

            return Mathf.Clamp01(baseRate + bonus);
        }

        // ── Resource production ───────────────────────────────────────────────────

        /// <summary>
        /// Returns a food production multiplier (1.0 = normal) for the group.
        /// Agriculteur talent adds +25%.
        /// Travailleur positive trait adds +10%.
        /// Paresseux negative trait removes -15%.
        /// </summary>
        public static float GetFoodProductionMultiplier(IReadOnlyList<SurvivorGeneratedProfile> profiles)
        {
            return ComputeProductionMultiplier(
                profiles,
                t => TalentTable.GetFoodProductionBonus(t),
                PositiveTrait.Travailleur, 0.10f,
                NegativeTrait.Paresseux, -0.15f);
        }

        /// <summary>
        /// Returns a water production multiplier (1.0 = normal) for the group.
        /// Hydrologue talent adds +25%.
        /// Travailleur positive trait adds +10%.
        /// Paresseux negative trait removes -15%.
        /// </summary>
        public static float GetWaterProductionMultiplier(IReadOnlyList<SurvivorGeneratedProfile> profiles)
        {
            return ComputeProductionMultiplier(
                profiles,
                t => TalentTable.GetWaterProductionBonus(t),
                PositiveTrait.Travailleur, 0.10f,
                NegativeTrait.Paresseux, -0.15f);
        }

        // ── Combat ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a combat efficiency score (0.0–1.0) for a group.
        ///
        /// Base: (Force + Endurance) / 200
        /// Tireur talent: +25%
        /// Courageux: +10%
        /// Peureux: -20%
        /// Belliqueux: -5% (uncontrolled aggression)
        /// </summary>
        public static float ComputeCombatEfficiency(IReadOnlyList<SurvivorGeneratedProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0) return 0f;

            float force    = AverageStat(profiles, SurvivorStatIndex.Force);
            float endur    = AverageStat(profiles, SurvivorStatIndex.Endurance);
            float baseRate = (force + endur) / 200f;

            float bonus = 0f;
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;

                foreach (SurvivorTalent talent in p.Talents)
                    bonus += TalentTable.GetCombatBonus(talent);

                if (p.positiveTrait == PositiveTrait.Courageux) bonus += 0.10f;
                if (p.negativeTrait == NegativeTrait.Peureux)   bonus -= 0.20f;
                if (p.negativeTrait == NegativeTrait.Belliqueux) bonus -= 0.05f;
            }

            bonus /= profiles.Count;
            return Mathf.Clamp01(baseRate + bonus);
        }

        // ── Trait behavioural checks ──────────────────────────────────────────────

        /// <summary>
        /// Returns true when any survivor in the group would refuse a dangerous mission.
        /// A Peureux survivor refuses if the mission danger rating exceeds their Endurance.
        /// </summary>
        public static bool WouldAnyRefuseDanger(IReadOnlyList<SurvivorGeneratedProfile> profiles, int dangerLevel)
        {
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;
                if (p.negativeTrait == NegativeTrait.Peureux && p.GetStat(SurvivorStatIndex.Endurance) < dangerLevel)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true when any Belliqueux survivor might provoke a fight.
        /// Triggers when the group Social average is below socialThreshold.
        /// </summary>
        public static bool MightProvokeFight(IReadOnlyList<SurvivorGeneratedProfile> profiles, int socialThreshold = 30)
        {
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;
                if (p.negativeTrait == NegativeTrait.Belliqueux &&
                    p.GetStat(SurvivorStatIndex.Social) < socialThreshold)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a morale bonus (additive, can be negative) generated by the group's traits.
        /// Leader +15, Empathique +8, Genereux +5, Pessimiste -10, Belliqueux -8, Egoiste -5.
        /// </summary>
        public static int ComputeGroupMoraleBonus(IReadOnlyList<SurvivorGeneratedProfile> profiles)
        {
            int bonus = 0;
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;

                if (p.HasTalent(SurvivorTalent.Leader)) bonus += 15;

                bonus += p.positiveTrait switch
                {
                    PositiveTrait.Empathique => 8,
                    PositiveTrait.Genereux   => 5,
                    PositiveTrait.Calme      => 3,
                    _                        => 0,
                };

                bonus += p.negativeTrait switch
                {
                    NegativeTrait.Pessimiste  => -10,
                    NegativeTrait.Belliqueux  => -8,
                    NegativeTrait.Egoiste     => -5,
                    NegativeTrait.Impulsif    => -4,
                    _                         => 0,
                };
            }
            return bonus;
        }

        // ── Repair ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a repair efficiency multiplier (1.0 = normal) for a single survivor.
        /// Bricoleur: +30%. Createur: +10%. Paresseux: -15%.
        /// </summary>
        public static float GetRepairMultiplier(SurvivorGeneratedProfile profile)
        {
            if (profile == null) return 1f;

            float bonus = 0f;

            foreach (SurvivorTalent talent in profile.Talents)
                bonus += TalentTable.GetRepairBonus(talent);

            if (profile.positiveTrait == PositiveTrait.Createur) bonus += 0.10f;
            if (profile.negativeTrait == NegativeTrait.Paresseux) bonus -= 0.15f;

            return Mathf.Max(0.1f, 1f + bonus);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static float ComputeProductionMultiplier(
            IReadOnlyList<SurvivorGeneratedProfile> profiles,
            System.Func<SurvivorTalent, float> talentBonusFunc,
            PositiveTrait posBonus, float posAmount,
            NegativeTrait negPenalty, float negAmount)
        {
            if (profiles == null || profiles.Count == 0) return 1f;

            float bonus = 0f;
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;

                foreach (SurvivorTalent talent in p.Talents)
                    bonus += talentBonusFunc(talent);

                if (p.positiveTrait == posBonus)     bonus += posAmount;
                if (p.negativeTrait == negPenalty)   bonus += negAmount;
            }

            bonus /= profiles.Count;
            return Mathf.Max(0.1f, 1f + bonus);
        }
    }
}
