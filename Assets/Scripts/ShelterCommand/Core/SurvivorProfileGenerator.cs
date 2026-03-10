using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Generates a SurvivorGeneratedProfile procedurally.
    /// Rules:
    ///   - Age between 18 and 70.
    ///   - Base stat pool between 100 and 150 total.
    ///   - Profession grants additional bonuses on top (may push individual stats beyond base).
    ///   - Age influences Endurance (young) and Intelligence (older).
    ///   - One random positive trait, one random negative trait.
    ///   - A narrative presentation text is assembled from profession + age + traits.
    /// </summary>
    public static class SurvivorProfileGenerator
    {
        private const int MinAge        = 18;
        private const int MaxAge        = 70;
        private const int MinStatPool   = 100;
        private const int MaxStatPool   = 150;
        private const int MaxSingleStat = 90;   // cap before profession bonus
        private const int YoungAgeThreshold = 30;
        private const int OldAgeThreshold  = 55;
        private const int AgeBonusAmount    = 8;

        private static readonly int StatCount = System.Enum.GetValues(typeof(SurvivorStatIndex)).Length;

        // ── Name pools ────────────────────────────────────────────────────────────

        private static readonly string[] FirstNamesMale =
        {
            "Marc", "Jules", "Théo", "Adrien", "Luca", "Maxime", "Antoine", "Raphaël",
            "Nathan", "Pierre", "Hugo", "Florian", "Romain", "Sébastien", "Dimitri",
        };

        private static readonly string[] FirstNamesFemale =
        {
            "Léa", "Marie", "Clara", "Sophie", "Emma", "Jade", "Camille", "Inès",
            "Lucie", "Manon", "Sarah", "Pauline", "Élise", "Noémie", "Amandine",
        };

        private static readonly string[] LastNames =
        {
            "Martin", "Bernard", "Dupont", "Thomas", "Robert", "Petit", "Leroy",
            "Simon", "Laurent", "Michel", "Garcia", "Lefebvre", "Moreau", "Girard", "Blanc",
        };

        // ── Presentation text templates ───────────────────────────────────────────

        private static readonly string[] YoungPrefixes    = { "Jeune", "Jeune survivant(e).", "Pas encore trente ans." };
        private static readonly string[] MiddlePrefixes   = { "Ancien(ne)", "Ex-", "Ancien(ne) " };
        private static readonly string[] OldPrefixes      = { "Vétéran(e).", "Une vie d'expérience.", "Âgé(e) mais solide." };
        private static readonly string[] CalmSuffixes     = { "Semble calme.", "Regard posé.", "Voix assurée." };
        private static readonly string[] TiredSuffixes    = { "Paraît épuisé(e).", "Des cernes profondes.", "Fatigué(e) mais debout." };
        private static readonly string[] WorriedSuffixes  = { "Les yeux inquiets.", "Tendu(e).", "Sur les nerfs." };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a complete SurvivorGeneratedProfile.
        /// Optionally provide a fixed name; if null, a random name is generated.
        /// </summary>
        public static SurvivorGeneratedProfile Generate(string fixedName = null)
        {
            SurvivorGeneratedProfile profile = new SurvivorGeneratedProfile();

            // Identity
            bool isFemale    = Random.value > 0.5f;
            profile.survivorName = fixedName ?? GenerateName(isFemale);
            profile.age          = Random.Range(MinAge, MaxAge + 1);

            // Traits & profession
            profile.positiveTrait = PickRandom<PositiveTrait>();
            profile.negativeTrait = PickRandom<NegativeTrait>();
            profile.profession    = PickRandom<SurvivorProfession>();

            // Base stat distribution
            DistributeBaseStats(profile);

            // Age influence
            ApplyAgeBonuses(profile);

            // Profession bonus
            ApplyProfessionBonuses(profile);

            // Narrative text
            profile.PresentationText = BuildPresentationText(profile);

            return profile;
        }

        /// <summary>
        /// Generates a profile that mirrors an existing SurvivorData ScriptableObject.
        /// Used for the hand-crafted named survivors (Aria, Borek, etc.).
        /// </summary>
        public static SurvivorGeneratedProfile FromSurvivorData(SurvivorData data)
        {
            SurvivorGeneratedProfile profile = new SurvivorGeneratedProfile();

            profile.survivorName = data.survivorName;
            profile.age          = data.age;
            profile.profession   = PickRandom<SurvivorProfession>();
            profile.positiveTrait = PickRandom<PositiveTrait>();
            profile.negativeTrait = PickRandom<NegativeTrait>();

            // Map existing stats
            profile.SetStat(SurvivorStatIndex.Force,        data.strength);
            profile.SetStat(SurvivorStatIndex.Intelligence, data.intelligence);
            profile.SetStat(SurvivorStatIndex.Technique,    data.technical);
            profile.SetStat(SurvivorStatIndex.Social,       data.loyalty);   // loyalty maps to Social
            profile.SetStat(SurvivorStatIndex.Endurance,    data.endurance);

            profile.PresentationText = BuildPresentationText(profile);
            return profile;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static void DistributeBaseStats(SurvivorGeneratedProfile profile)
        {
            int pool = Random.Range(MinStatPool, MaxStatPool + 1);

            // Distribute pool across 5 stats using weighted random split
            int[] raw = new int[StatCount];
            int remaining = pool;

            for (int i = 0; i < StatCount - 1; i++)
            {
                int max  = Mathf.Min(remaining - (StatCount - 1 - i), MaxSingleStat);
                int min  = 5; // guaranteed minimum per stat
                raw[i]   = Random.Range(min, Mathf.Max(min + 1, max));
                remaining -= raw[i];
            }
            raw[StatCount - 1] = Mathf.Clamp(remaining, 5, MaxSingleStat);

            // Shuffle to avoid always leaving the residual on Endurance
            Shuffle(raw);

            for (int i = 0; i < StatCount; i++)
                profile.SetStat((SurvivorStatIndex)i, raw[i]);
        }

        private static void ApplyAgeBonuses(SurvivorGeneratedProfile profile)
        {
            if (profile.age <= YoungAgeThreshold)
            {
                // Young → bonus Endurance
                profile.AddToStat(SurvivorStatIndex.Endurance, AgeBonusAmount);
            }
            else if (profile.age >= OldAgeThreshold)
            {
                // Older → bonus Intelligence, slight endurance penalty
                profile.AddToStat(SurvivorStatIndex.Intelligence, AgeBonusAmount);
                profile.AddToStat(SurvivorStatIndex.Endurance, -AgeBonusAmount / 2);
            }
        }

        private static void ApplyProfessionBonuses(SurvivorGeneratedProfile profile)
        {
            Dictionary<SurvivorStatIndex, int> bonuses = ProfessionBonusTable.GetBonuses(profile.profession);
            foreach (KeyValuePair<SurvivorStatIndex, int> kvp in bonuses)
                profile.AddToStat(kvp.Key, kvp.Value);
        }

        private static string BuildPresentationText(SurvivorGeneratedProfile profile)
        {
            string profLabel = ProfessionBonusTable.GetLabel(profile.profession);
            string agePart;

            if (profile.age <= YoungAgeThreshold)
                agePart = Pick(YoungPrefixes) + " " + profLabel.ToLower() + ".";
            else if (profile.age < OldAgeThreshold)
                agePart = Pick(MiddlePrefixes) + profLabel.ToLower() + ".";
            else
                agePart = Pick(OldPrefixes) + " " + profLabel.ToLower() + ".";

            string posLabel = TraitLabels.GetLabel(profile.positiveTrait);
            string negLabel = TraitLabels.GetLabel(profile.negativeTrait);

            // Pick mood suffix based on negative trait category
            string mood = profile.negativeTrait switch
            {
                NegativeTrait.Peureux    or NegativeTrait.Pessimiste => Pick(WorriedSuffixes),
                NegativeTrait.Paresseux  or NegativeTrait.Fragile    => Pick(TiredSuffixes),
                _                                                     => Pick(CalmSuffixes),
            };

            return $"{agePart} {mood}\n{posLabel} mais {negLabel.ToLower()}.";
        }

        private static string GenerateName(bool female)
        {
            string first = female
                ? FirstNamesFemale[Random.Range(0, FirstNamesFemale.Length)]
                : FirstNamesMale[Random.Range(0, FirstNamesMale.Length)];
            string last = LastNames[Random.Range(0, LastNames.Length)];
            return $"{first} {last}";
        }

        private static T PickRandom<T>() where T : System.Enum
        {
            System.Array values = System.Enum.GetValues(typeof(T));
            return (T)values.GetValue(Random.Range(0, values.Length));
        }

        private static string Pick(string[] array) => array[Random.Range(0, array.Length)];

        private static void Shuffle(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
