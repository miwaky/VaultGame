using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Defines how a failed condition is handled in the dialogue UI.
    /// </summary>
    public enum ConditionFailBehaviour
    {
        /// <summary>The choice button is hidden entirely.</summary>
        Hide,
        /// <summary>The choice button is shown but greyed out and unclickable.</summary>
        Disable,
    }

    /// <summary>
    /// A single stat threshold requirement: a stat must meet or exceed the threshold value.
    /// </summary>
    [Serializable]
    public class StatRequirement
    {
        [Tooltip("Which stat is tested.")]
        public SurvivorStatIndex stat = SurvivorStatIndex.Force;

        [Tooltip("Minimum value (inclusive) the stat must reach.")]
        [Min(0)]
        public int minimumValue = 20;

        /// <summary>Returns true when the supplied stat value satisfies this requirement.</summary>
        public bool IsMet(int statValue) => statValue >= minimumValue;

        /// <summary>French hint label shown when the requirement is not met, e.g. "(Force ≥ 25)".</summary>
        public string GetHintLabel() =>
            $"({StatLabel(stat)} ≥ {minimumValue})";

        private static string StatLabel(SurvivorStatIndex s) => s switch
        {
            SurvivorStatIndex.Force        => "Force",
            SurvivorStatIndex.Intelligence => "Intelligence",
            SurvivorStatIndex.Technique    => "Technique",
            SurvivorStatIndex.Social       => "Social",
            SurvivorStatIndex.Endurance    => "Endurance",
            _                              => s.ToString(),
        };
    }

    /// <summary>
    /// A single talent requirement: at least one survivor in the group must possess the talent.
    /// </summary>
    [Serializable]
    public class TalentRequirement
    {
        [Tooltip("Required talent. At least one survivor in the mission must have it.")]
        public SurvivorTalent requiredTalent = SurvivorTalent.Explorateur;

        /// <summary>Returns true when at least one of the profiles has the required talent.</summary>
        public bool IsMet(IEnumerable<SurvivorGeneratedProfile> profiles)
        {
            foreach (SurvivorGeneratedProfile p in profiles)
                if (p != null && p.HasTalent(requiredTalent)) return true;
            return false;
        }

        /// <summary>French hint label shown when the requirement is not met.</summary>
        public string GetHintLabel() =>
            $"(Talent : {TalentTable.GetLabel(requiredTalent)})";
    }

    /// <summary>
    /// A single trait requirement: at least one survivor must (or must not) have the specified trait.
    /// </summary>
    [Serializable]
    public class TraitRequirement
    {
        [Tooltip("If true, tests PositiveTrait; if false, tests NegativeTrait.")]
        public bool isPositive = true;

        [Tooltip("Positive trait tested when isPositive is true.")]
        public PositiveTrait positiveTrait = PositiveTrait.Courageux;

        [Tooltip("Negative trait tested when isPositive is false.")]
        public NegativeTrait negativeTrait = NegativeTrait.Peureux;

        [Tooltip("If true, the choice requires the trait to be ABSENT (blocked by trait).")]
        public bool mustBeAbsent = false;

        /// <summary>
        /// Returns true when the requirement is satisfied by the supplied profiles.
        /// </summary>
        public bool IsMet(IEnumerable<SurvivorGeneratedProfile> profiles)
        {
            bool found = false;
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                if (p == null) continue;
                if (isPositive && p.positiveTrait == positiveTrait) { found = true; break; }
                if (!isPositive && p.negativeTrait == negativeTrait) { found = true; break; }
            }
            return mustBeAbsent ? !found : found;
        }

        /// <summary>French hint label shown in the choice button.</summary>
        public string GetHintLabel()
        {
            string label = isPositive
                ? TraitLabels.GetLabel(positiveTrait)
                : TraitLabels.GetLabel(negativeTrait);
            return mustBeAbsent ? $"(Sans : {label})" : $"(Trait : {label})";
        }
    }

    /// <summary>
    /// Aggregates all conditions that must be satisfied for a <see cref="DialogueChoice"/>
    /// to be available. Evaluated against the survivors currently on mission by
    /// <see cref="SurvivorStatEvaluator"/>.
    ///
    /// All non-empty requirement lists must be individually satisfied (AND logic).
    /// </summary>
    [Serializable]
    public class DialogueChoiceCondition
    {
        [Tooltip("How this choice is displayed when one or more conditions are not met.")]
        public ConditionFailBehaviour failBehaviour = ConditionFailBehaviour.Disable;

        [Tooltip("Stat requirements — each stat is tested against the best value across all mission survivors.")]
        public StatRequirement[] statRequirements = Array.Empty<StatRequirement>();

        [Tooltip("Talent requirements — at least one survivor must have each listed talent.")]
        public TalentRequirement[] talentRequirements = Array.Empty<TalentRequirement>();

        [Tooltip("Trait requirements — evaluated per survivor.")]
        public TraitRequirement[] traitRequirements = Array.Empty<TraitRequirement>();

        /// <summary>True when there are no requirements at all (unconditional choice).</summary>
        public bool IsEmpty =>
            (statRequirements == null || statRequirements.Length == 0) &&
            (talentRequirements == null || talentRequirements.Length == 0) &&
            (traitRequirements == null || traitRequirements.Length == 0);

        /// <summary>
        /// Evaluates all requirements against the given survivor profiles.
        /// Returns true only when every requirement is satisfied.
        /// </summary>
        public bool Evaluate(IReadOnlyList<SurvivorGeneratedProfile> profiles, out string failHint)
        {
            failHint = string.Empty;

            if (IsEmpty) return true;

            var hints = new System.Text.StringBuilder();
            bool allMet = true;

            // Stat checks — use the best value across all survivors for each stat
            if (statRequirements != null)
            {
                foreach (StatRequirement req in statRequirements)
                {
                    int best = 0;
                    foreach (SurvivorGeneratedProfile p in profiles)
                        if (p != null) best = Mathf.Max(best, p.GetStat(req.stat));

                    if (!req.IsMet(best))
                    {
                        if (hints.Length > 0) hints.Append(' ');
                        hints.Append(req.GetHintLabel());
                        allMet = false;
                    }
                }
            }

            // Talent checks
            if (talentRequirements != null)
            {
                foreach (TalentRequirement req in talentRequirements)
                {
                    if (!req.IsMet(profiles))
                    {
                        if (hints.Length > 0) hints.Append(' ');
                        hints.Append(req.GetHintLabel());
                        allMet = false;
                    }
                }
            }

            // Trait checks
            if (traitRequirements != null)
            {
                foreach (TraitRequirement req in traitRequirements)
                {
                    if (!req.IsMet(profiles))
                    {
                        if (hints.Length > 0) hints.Append(' ');
                        hints.Append(req.GetHintLabel());
                        allMet = false;
                    }
                }
            }

            if (!allMet)
                failHint = hints.ToString();

            return allMet;
        }
    }
}
