using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Pool de lignes de dialogue d'un PNJ.
    /// Une ligne aléatoire est tirée à chaque interaction selon l'état courant du survivant.
    /// Le DialogueManager affiche le résultat via le DialoguePanelUI existant.
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/Dialogue/SurvivorNpcDialogue", fileName = "NpcDialogue_New")]
    public class SurvivorNpcDialogue : ScriptableObject
    {
        [Header("Lignes repos (Idle)")]
        [Tooltip("Textes affichés quand le PNJ est au repos. {name} est remplacé par son nom.")]
        [TextArea(2, 4)]
        public string[] idleLines = Array.Empty<string>();

        [Header("Lignes travail (Working)")]
        [Tooltip("Textes affichés quand le PNJ travaille. {name} est remplacé par son nom.")]
        [TextArea(2, 4)]
        public string[] workingLines = Array.Empty<string>();

        [Header("Lignes de déplacement (Moving)")]
        [Tooltip("Textes affichés quand le PNJ est en déplacement. {name} est remplacé par son nom.")]
        [TextArea(2, 4)]
        public string[] movingLines = Array.Empty<string>();

        /// <summary>Retourne une ligne aléatoire selon l'état du survivant.</summary>
        public string GetRandomLine(SurvivorBehavior survivor)
        {
            string[] pool = PickPool(survivor);

            if (pool == null || pool.Length == 0)
                return $"{survivor.SurvivorName} ne dit rien.";

            string line = pool[UnityEngine.Random.Range(0, pool.Length)];
            return line.Replace("{name}", survivor.SurvivorName);
        }

        private string[] PickPool(SurvivorBehavior survivor)
        {
            if (survivor.IsWorking && workingLines.Length > 0)  return workingLines;

            UnityEngine.AI.NavMeshAgent agent = survivor.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool isMoving = agent != null && agent.velocity.magnitude > 0.05f;
            if (isMoving && movingLines.Length > 0) return movingLines;

            return idleLines.Length > 0 ? idleLines : workingLines;
        }
    }
}
