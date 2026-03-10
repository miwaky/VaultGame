using System.Collections.Generic;

namespace ShelterCommand
{
    /// <summary>
    /// Available professions for a survivor. Each profession grants stat bonuses at generation time.
    /// </summary>
    public enum SurvivorProfession
    {
        BTP,            // Technique + Force
        Universitaire,  // Intelligence
        Infirmier,      // Social + Intelligence
        Militaire,      // Force + Endurance
        Mecanicien,     // Technique + Endurance
        Agriculteur,    // Endurance + Force
        Enseignant,     // Intelligence + Social
        Medecin,        // Intelligence + Social (heavy)
        Electricien,    // Technique
        Cuisinier,      // Social + Endurance
        Journaliste,    // Social + Intelligence (light)
        Athlete,        // Force + Endurance (heavy)
    }

    /// <summary>
    /// Provides stat bonus definitions per profession.
    /// Each entry maps a profession to a set of (stat, bonus) pairs.
    /// </summary>
    public static class ProfessionBonusTable
    {
        private const int SmallBonus  = 8;
        private const int MediumBonus = 12;
        private const int LargeBonus  = 16;

        // Returns a dictionary of bonus values indexed by SurvivorStatIndex.
        public static Dictionary<SurvivorStatIndex, int> GetBonuses(SurvivorProfession profession)
        {
            return profession switch
            {
                SurvivorProfession.BTP           => Bonuses(( SurvivorStatIndex.Technique, MediumBonus ), ( SurvivorStatIndex.Force, SmallBonus )),
                SurvivorProfession.Universitaire  => Bonuses(( SurvivorStatIndex.Intelligence, LargeBonus )),
                SurvivorProfession.Infirmier      => Bonuses(( SurvivorStatIndex.Social, MediumBonus ), ( SurvivorStatIndex.Intelligence, SmallBonus )),
                SurvivorProfession.Militaire      => Bonuses(( SurvivorStatIndex.Force, MediumBonus ), ( SurvivorStatIndex.Endurance, SmallBonus )),
                SurvivorProfession.Mecanicien     => Bonuses(( SurvivorStatIndex.Technique, MediumBonus ), ( SurvivorStatIndex.Endurance, SmallBonus )),
                SurvivorProfession.Agriculteur    => Bonuses(( SurvivorStatIndex.Endurance, MediumBonus ), ( SurvivorStatIndex.Force, SmallBonus )),
                SurvivorProfession.Enseignant     => Bonuses(( SurvivorStatIndex.Intelligence, SmallBonus ), ( SurvivorStatIndex.Social, MediumBonus )),
                SurvivorProfession.Medecin        => Bonuses(( SurvivorStatIndex.Intelligence, LargeBonus ), ( SurvivorStatIndex.Social, MediumBonus )),
                SurvivorProfession.Electricien    => Bonuses(( SurvivorStatIndex.Technique, LargeBonus )),
                SurvivorProfession.Cuisinier      => Bonuses(( SurvivorStatIndex.Social, MediumBonus ), ( SurvivorStatIndex.Endurance, SmallBonus )),
                SurvivorProfession.Journaliste    => Bonuses(( SurvivorStatIndex.Social, SmallBonus ), ( SurvivorStatIndex.Intelligence, SmallBonus )),
                SurvivorProfession.Athlete        => Bonuses(( SurvivorStatIndex.Force, MediumBonus ), ( SurvivorStatIndex.Endurance, LargeBonus )),
                _                                 => new Dictionary<SurvivorStatIndex, int>(),
            };
        }

        /// <summary>Returns a human-readable French label for the given profession.</summary>
        public static string GetLabel(SurvivorProfession profession) => profession switch
        {
            SurvivorProfession.BTP           => "Ouvrier BTP",
            SurvivorProfession.Universitaire  => "Universitaire",
            SurvivorProfession.Infirmier      => "Infirmier(ère)",
            SurvivorProfession.Militaire      => "Militaire",
            SurvivorProfession.Mecanicien     => "Mécanicien(ne)",
            SurvivorProfession.Agriculteur    => "Agriculteur(rice)",
            SurvivorProfession.Enseignant     => "Enseignant(e)",
            SurvivorProfession.Medecin        => "Médecin",
            SurvivorProfession.Electricien    => "Électricien(ne)",
            SurvivorProfession.Cuisinier      => "Cuisinier(ère)",
            SurvivorProfession.Journaliste    => "Journaliste",
            SurvivorProfession.Athlete        => "Athlète",
            _                                 => "Inconnu",
        };

        private static Dictionary<SurvivorStatIndex, int> Bonuses(
            params (SurvivorStatIndex stat, int value)[] entries)
        {
            Dictionary<SurvivorStatIndex, int> dict = new Dictionary<SurvivorStatIndex, int>();
            foreach ((SurvivorStatIndex stat, int value) in entries)
                dict[stat] = value;
            return dict;
        }
    }

    /// <summary>Indices used to address the stats array in SurvivorGeneratedProfile.</summary>
    public enum SurvivorStatIndex
    {
        Force        = 0,
        Intelligence = 1,
        Technique    = 2,
        Social       = 3,
        Endurance    = 4,
    }
}
