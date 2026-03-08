using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// ScriptableObject defining a survivor archetype's base stats and name.
    /// </summary>
    [CreateAssetMenu(fileName = "SurvivorData", menuName = "ShelterCommand/Survivor Data")]
    public class SurvivorData : ScriptableObject
    {
        [Header("Identity")]
        public string survivorName = "Unknown";
        public int age = 30;
        public Sprite portrait;

        [Header("Base Stats (0-100)")]
        [Range(0, 100)] public int strength = 50;
        [Range(0, 100)] public int intelligence = 50;
        [Range(0, 100)] public int technical = 50;
        [Range(0, 100)] public int loyalty = 50;
        [Range(0, 100)] public int endurance = 50;
    }
}
