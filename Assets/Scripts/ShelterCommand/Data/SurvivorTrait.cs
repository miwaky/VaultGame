namespace ShelterCommand
{
    /// <summary>
    /// Positive personality traits. Affect survivor behaviour positively.
    /// </summary>
    public enum PositiveTrait
    {
        Travailleur,   // Bonus productivité (endurance lente à baisser)
        Calme,         // Stress monte plus lentement
        Courageux,     // Accepte les ordres risqués sans pénalité
        Genereux,      // Partage nourriture → réduit stress des voisins
        Loyal,         // Refuse rarement un ordre même à bas moral
        Createur,      // Bonus Technique lors de la fabrication
        Empathique,    // Réduit le stress des autres survivants proches
        Strategique,   // Bonus Intelligence dans les missions
    }

    /// <summary>
    /// Negative personality traits. Affect survivor behaviour negatively.
    /// </summary>
    public enum NegativeTrait
    {
        Peureux,       // Stress monte plus vite lors d'événements
        Belliqueux,    // Augmente le stress des voisins
        Egoiste,       // Consomme plus de nourriture
        Paresseux,     // Fatigue monte plus vite
        Impulsif,      // Peut déclencher des incidents aléatoires
        Pessimiste,    // Moral descend plus vite
        Kleptomann,    // Peut voler des ressources même à moral normal
        Fragile,       // Tombe malade plus facilement
    }

    /// <summary>
    /// Utility class providing French labels and gameplay descriptions for traits.
    /// </summary>
    public static class TraitLabels
    {
        /// <summary>Returns a French label for the given positive trait.</summary>
        public static string GetLabel(PositiveTrait trait) => trait switch
        {
            PositiveTrait.Travailleur  => "Travailleur",
            PositiveTrait.Calme        => "Calme",
            PositiveTrait.Courageux    => "Courageux",
            PositiveTrait.Genereux     => "Généreux",
            PositiveTrait.Loyal        => "Loyal",
            PositiveTrait.Createur     => "Créatif",
            PositiveTrait.Empathique   => "Empathique",
            PositiveTrait.Strategique  => "Stratège",
            _                          => "Inconnu",
        };

        /// <summary>Returns a French label for the given negative trait.</summary>
        public static string GetLabel(NegativeTrait trait) => trait switch
        {
            NegativeTrait.Peureux      => "Peureux",
            NegativeTrait.Belliqueux   => "Belliqueux",
            NegativeTrait.Egoiste      => "Égoïste",
            NegativeTrait.Paresseux    => "Paresseux",
            NegativeTrait.Impulsif     => "Impulsif",
            NegativeTrait.Pessimiste   => "Pessimiste",
            NegativeTrait.Kleptomann   => "Kleptomane",
            NegativeTrait.Fragile      => "Fragile",
            _                          => "Inconnu",
        };
    }
}
