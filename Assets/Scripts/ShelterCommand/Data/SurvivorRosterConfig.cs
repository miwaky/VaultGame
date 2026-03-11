using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// ScriptableObject that holds a readable snapshot of all generated survivor profiles.
    /// Populated at runtime by SurvivorInitializer so you can inspect the full roster
    /// in the Inspector without relying on individual SurvivorData assets.
    ///
    /// Create one via Assets > Create > ShelterCommand > Survivor Roster Config
    /// and assign it to SurvivorInitializer.
    /// </summary>
    [CreateAssetMenu(fileName = "SurvivorRosterConfig", menuName = "ShelterCommand/Survivor Roster Config")]
    public class SurvivorRosterConfig : ScriptableObject
    {
        [System.Serializable]
        public class SurvivorSnapshot
        {
            public string name;
            public int    age;
            public string profession;
            public string positiveTrait;
            public string negativeTrait;
            public int    force;
            public int    intelligence;
            public int    technique;
            public int    social;
            public int    endurance;
            public int    totalStats;
            [TextArea(2, 4)]
            public string presentationText;
        }

        [Tooltip("Filled automatically at runtime by SurvivorInitializer. Read-only at edit time.")]
        public List<SurvivorSnapshot> survivors = new List<SurvivorSnapshot>();

        /// <summary>Clears the list and rebuilds it from the given profiles.</summary>
        public void Populate(IReadOnlyList<SurvivorGeneratedProfile> profiles)
        {
            survivors.Clear();
            foreach (SurvivorGeneratedProfile p in profiles)
            {
                survivors.Add(new SurvivorSnapshot
                {
                    name             = p.survivorName,
                    age              = p.age,
                    profession       = ProfessionBonusTable.GetLabel(p.profession),
                    positiveTrait    = TraitLabels.GetLabel(p.positiveTrait),
                    negativeTrait    = TraitLabels.GetLabel(p.negativeTrait),
                    force            = p.Force,
                    intelligence     = p.Intelligence,
                    technique        = p.Technique,
                    social           = p.Social,
                    endurance        = p.Endurance,
                    totalStats       = p.TotalStats,
                    presentationText = p.PresentationText,
                });
            }

#if UNITY_EDITOR
            // Mark dirty so the Inspector shows updated values during Play mode
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
