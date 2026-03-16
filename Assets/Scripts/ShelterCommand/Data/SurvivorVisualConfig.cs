using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// ScriptableObject holding the pools of male and female NPC visual prefabs.
    /// Each prefab must contain a SkinnedMeshRenderer (or MeshRenderer) and an Animator.
    /// </summary>
    [CreateAssetMenu(fileName = "SurvivorVisualConfig", menuName = "ShelterCommand/Survivor Visual Config")]
    public class SurvivorVisualConfig : ScriptableObject
    {
        [Header("Male Visual Prefabs")]
        [Tooltip("Prefabs used for male survivors. Each should have a Renderer and an Animator.")]
        public GameObject[] malePrefabs = System.Array.Empty<GameObject>();

        [Header("Female Visual Prefabs")]
        [Tooltip("Prefabs used for female survivors. Each should have a Renderer and an Animator.")]
        public GameObject[] femalePrefabs = System.Array.Empty<GameObject>();

        /// <summary>Returns a random prefab matching the given gender. Returns null if the pool is empty.</summary>
        public GameObject GetRandomPrefab(SurvivorGender gender)
        {
            GameObject[] pool = gender == SurvivorGender.Female ? femalePrefabs : malePrefabs;

            if (pool == null || pool.Length == 0)
            {
                Debug.LogWarning($"[SurvivorVisualConfig] Pool vide pour le genre : {gender}.");
                return null;
            }

            return pool[Random.Range(0, pool.Length)];
        }
    }
}
