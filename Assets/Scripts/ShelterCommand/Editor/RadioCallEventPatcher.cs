using UnityEditor;
using UnityEngine;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Patches RadioCallEvent assets whose dialogue reference is missing.
    /// Menu: Window → ShelterCommand → Patch RadioCallEvent Dialogues
    /// </summary>
    public static class RadioCallEventPatcher
    {
        private const string EncountersDir = "Assets/Data/Missions/Example/Encounters";
        private const string DialoguesDir  = "Assets/Data/Missions/Example/Dialogues";

        [MenuItem("Window/ShelterCommand/Patch RadioCallEvent Dialogues")]
        public static void PatchAll()
        {
            int patched = 0;

            string[] guids = AssetDatabase.FindAssets("t:RadioCallEvent", new[] { EncountersDir });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RadioCallEvent rc = AssetDatabase.LoadAssetAtPath<RadioCallEvent>(path);

                if (rc == null || rc.dialogue != null) continue;

                // Try to find a matching ExplorationDialogue by convention:
                // "Encounter_Arrivée_Maison" → looks for "Dialogue_Arrivée_Maison"
                string encounterName = rc.name;                               // e.g. Encounter_Arrivée_Maison
                string dialogueName  = encounterName.Replace("Encounter_", "Dialogue_");

                string[] dGuids = AssetDatabase.FindAssets($"t:ExplorationDialogue {dialogueName}", new[] { DialoguesDir });

                if (dGuids.Length == 0)
                {
                    Debug.LogWarning($"[RadioCallEventPatcher] Aucun dialogue trouvé pour '{encounterName}' (cherché: '{dialogueName}').");
                    continue;
                }

                string dPath = AssetDatabase.GUIDToAssetPath(dGuids[0]);
                ExplorationDialogue dialogue = AssetDatabase.LoadAssetAtPath<ExplorationDialogue>(dPath);

                if (dialogue == null) continue;

                rc.dialogue = dialogue;
                EditorUtility.SetDirty(rc);
                patched++;

                Debug.Log($"[RadioCallEventPatcher] '{rc.name}' → dialogue '{dialogue.name}' assigné.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[RadioCallEventPatcher] Patch terminé : {patched} RadioCallEvent(s) mis à jour.");
        }
    }
}
