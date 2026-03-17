using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Fully generated profile for a survivor.
    /// Created at runtime by SurvivorProfileGenerator and carried by SurvivorBehavior.
    /// Replaces the static ScriptableObject approach for procedurally generated survivors.
    /// </summary>
    /// <summary>Biological gender used to pick the correct visual model pool.</summary>
    public enum SurvivorGender { Male, Female }

    [System.Serializable]
    public class SurvivorGeneratedProfile
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        public string         survivorName;
        public int            age;
        public SurvivorGender gender;

        // ── Profession ───────────────────────────────────────────────────────────
        public SurvivorProfession profession;

        // ── Traits ───────────────────────────────────────────────────────────────
        public PositiveTrait positiveTrait;
        public NegativeTrait negativeTrait;

        // ── Talents (1 to 3 special skills) ──────────────────────────────────────
        [SerializeField] private List<SurvivorTalent> talents = new List<SurvivorTalent>();

        /// <summary>Read-only list of talents assigned to this survivor.</summary>
        public IReadOnlyList<SurvivorTalent> Talents => talents;

        /// <summary>Returns true when this survivor possesses the given talent.</summary>
        public bool HasTalent(SurvivorTalent talent) => talents.Contains(talent);

        /// <summary>Assigns the talent list (replaces any existing talents).</summary>
        public void SetTalents(IEnumerable<SurvivorTalent> newTalents)
        {
            talents.Clear();
            if (newTalents != null)
                talents.AddRange(newTalents);
        }

        // ── Stats (indexed by SurvivorStatIndex) ─────────────────────────────────
        // Order: Force, Intelligence, Technique, Social, Endurance
        [SerializeField] private int[] stats = new int[5];

        public int Force        => stats[(int)SurvivorStatIndex.Force];
        public int Intelligence => stats[(int)SurvivorStatIndex.Intelligence];
        public int Technique    => stats[(int)SurvivorStatIndex.Technique];
        public int Social       => stats[(int)SurvivorStatIndex.Social];
        public int Endurance    => stats[(int)SurvivorStatIndex.Endurance];

        public int TotalStats
        {
            get
            {
                int total = 0;
                foreach (int s in stats) total += s;
                return total;
            }
        }

        // ── Presentation text ─────────────────────────────────────────────────────
        /// <summary>Short narrative presentation line shown in the interaction UI.</summary>
        public string PresentationText { get; set; }

        // ── Constructor ───────────────────────────────────────────────────────────

        public SurvivorGeneratedProfile()
        {
            stats   = new int[5];
            talents = new List<SurvivorTalent>();
        }

        /// <summary>Sets a raw stat value, clamped to [0, 100].</summary>
        public void SetStat(SurvivorStatIndex index, int value)
        {
            stats[(int)index] = Mathf.Clamp(value, 0, 100);
        }

        /// <summary>Gets a raw stat value by index.</summary>
        public int GetStat(SurvivorStatIndex index) => stats[(int)index];

        /// <summary>Adds a delta to a stat, clamped to [0, 100].</summary>
        public void AddToStat(SurvivorStatIndex index, int delta)
        {
            SetStat(index, stats[(int)index] + delta);
        }

        /// <summary>Returns formatted stats block for display in UI.</summary>
        public string GetStatsDisplayText()
        {
            return $"Force {Force}  •  Intel. {Intelligence}  •  Tech. {Technique}" +
                   $"\nSocial {Social}  •  Endurance {Endurance}";
        }

        /// <summary>Returns a formatted talents line for display in UI.</summary>
        public string GetTalentsDisplayText()
        {
            if (talents == null || talents.Count == 0)
                return "Aucun talent";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < talents.Count; i++)
            {
                if (i > 0) sb.Append("  •  ");
                sb.Append(TalentTable.GetLabel(talents[i]));
            }
            return sb.ToString();
        }

        /// <summary>Returns formatted identity block for display in UI.</summary>
        public string GetIdentityDisplayText()
        {
            string profLabel = ProfessionBonusTable.GetLabel(profession);
            string posLabel  = TraitLabels.GetLabel(positiveTrait);
            string negLabel  = TraitLabels.GetLabel(negativeTrait);
            return $"Âge : {age} ans  |  Métier : {profLabel}\n" +
                   $"Trait + : {posLabel}  |  Trait - : {negLabel}";
        }
    }
}
