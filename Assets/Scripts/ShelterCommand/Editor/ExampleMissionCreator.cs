using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Generates a complete set of example mission assets demonstrating the full
    /// encounter system: global pool, zone pool, fixed calls, timed choices, and mission chaining.
    ///
    /// Menu: Window → ShelterCommand → Generate Example Mission
    ///
    /// Assets are created in Assets/Data/Missions/Example/.
    /// Run once — re-running overwrites existing files.
    /// </summary>
    public static class ExampleMissionCreator
    {
        private const string RootDir = "Assets/Data/Missions/Example";

        [MenuItem("Window/ShelterCommand/Generate Example Mission")]
        public static void Generate()
        {
            EnsureDirectory(RootDir);
            EnsureDirectory(RootDir + "/Dialogues");
            EnsureDirectory(RootDir + "/Encounters");

            // ── 1. Dialogue nodes ─────────────────────────────────────────────────

            // Transit — all good
            var d_ok = Dialogue("Transit_Ok",
                speakerName:  "Marco",
                text:         "Base, ici Marco. Tout se passe bien. " +
                              "On a encore {amount} {resource} pour tenir.",
                hasLimit:     false);
            d_ok.nodeEvent = new DialogueEventData
            {
                eventType     = DialogueEventType.AddResource,
                selectionMode = ResourceSelectionMode.RandomOne,
                resources     = new[]
                {
                    new ResourceEntry { resourceType = ResourceType.Food,  useRandomAmount = true, minAmount = 2, maxAmount = 6 },
                    new ResourceEntry { resourceType = ResourceType.Water, useRandomAmount = true, minAmount = 1, maxAmount = 4 }
                }
            };
            Save(d_ok, "Dialogues/Dialogue_Transit_Ok");

            // Transit — bad weather
            var d_weather = Dialogue("Transit_Météo",
                speakerName:  "Marco",
                text:         "Base, mauvaise météo. On avance au ralenti. Rien de grave.",
                hasLimit:     false);
            Save(d_weather, "Dialogues/Dialogue_Transit_Météo");

            // Transit — injury (choice + timer)
            var d_injury_next = Dialogue("Transit_Blessure_Suite",
                speakerName:  "Marco",
                text:         "Ok, on le prend en charge. On continue quand même.",
                hasLimit:     false);
            Save(d_injury_next, "Dialogues/Dialogue_Transit_Blessure_Suite");

            var d_injury = Dialogue("Transit_Blessure",
                speakerName:  "Marco",
                text:         "Base, un de nos gars s'est blessé à la jambe. On rentre ?",
                hasLimit:     true,
                timeSec:      40f,
                timeoutIdx:   1);   // default = continuer
            d_injury.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText    = "Rentrez immédiatement.",
                    eventTrigger  = new DialogueEventData { eventType = DialogueEventType.MissionReturn },
                    nextDialogue  = null
                },
                new DialogueChoice
                {
                    choiceText    = "Soignez-le et continuez.",
                    eventTrigger  = new DialogueEventData { eventType = DialogueEventType.Injury },
                    nextDialogue  = d_injury_next
                }
            };
            Save(d_injury, "Dialogues/Dialogue_Transit_Blessure");

            // Transit — found cache
            var d_cache = Dialogue("Transit_Cache",
                speakerName:  "Marco",
                text:         "Base ! On a trouvé une cache. {amount} {resource} ! On les prend.",
                hasLimit:     false);
            d_cache.nodeEvent = new DialogueEventData
            {
                eventType     = DialogueEventType.AddResource,
                selectionMode = ResourceSelectionMode.RandomOne,
                resources     = new[]
                {
                    new ResourceEntry { resourceType = ResourceType.Food,      useRandomAmount = true, minAmount = 3, maxAmount = 8 },
                    new ResourceEntry { resourceType = ResourceType.Water,     useRandomAmount = true, minAmount = 2, maxAmount = 5 },
                    new ResourceEntry { resourceType = ResourceType.Medicine,  amount = 2 },
                    new ResourceEntry { resourceType = ResourceType.Materials, useRandomAmount = true, minAmount = 1, maxAmount = 4 }
                }
            };
            Save(d_cache, "Dialogues/Dialogue_Transit_Cache");

            // Transit — threat
            var d_threat = Dialogue("Transit_Menace",
                speakerName:  "Marco",
                text:         "Base, on voit des silhouettes au loin. On se planque. Ne répondez pas.",
                hasLimit:     false);
            Save(d_threat, "Dialogues/Dialogue_Transit_Menace");

            // Arrival Day 2 — house found, timed choice
            var d_arrive_explore = Dialogue("Arrivée_Explorer",
                speakerName:  "Marco",
                text:         "Base, on a fouillé la maison. {amount} {resource}. On rentre avec ça.",
                hasLimit:     false);
            d_arrive_explore.nodeEvent = new DialogueEventData
            {
                eventType     = DialogueEventType.AddResource,
                selectionMode = ResourceSelectionMode.All,
                resources     = new[]
                {
                    new ResourceEntry { resourceType = ResourceType.Food,  useRandomAmount = true, minAmount = 4, maxAmount = 10 },
                    new ResourceEntry { resourceType = ResourceType.Water, useRandomAmount = true, minAmount = 2, maxAmount = 6  }
                }
            };
            d_arrive_explore.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Rentrez avec ce que vous avez.",
                    eventTrigger = new DialogueEventData { eventType = DialogueEventType.MissionReturn },
                    nextDialogue = null
                }
            };
            Save(d_arrive_explore, "Dialogues/Dialogue_Arrivée_Explorer");

            var d_arrive = Dialogue("Arrivée_Maison",
                speakerName:  "Marco",
                text:         "Base, on aperçoit une maison abandonnée. On entre fouiller ?",
                hasLimit:     true,
                timeSec:      30f,
                timeoutIdx:   1);   // default = ne pas entrer
            d_arrive.choices = new[]
            {
                new DialogueChoice
                {
                    choiceText   = "Oui, entrez fouiller.",
                    eventTrigger = new DialogueEventData { eventType = DialogueEventType.None },
                    nextDialogue = d_arrive_explore
                },
                new DialogueChoice
                {
                    choiceText   = "Non, rentrez directement.",
                    eventTrigger = new DialogueEventData { eventType = DialogueEventType.MissionReturn },
                    nextDialogue = null
                }
            };
            Save(d_arrive, "Dialogues/Dialogue_Arrivée_Maison");

            // ── 2. RadioCallEvents pour les encounters ────────────────────────────

            var rc_ok      = Encounter("Encounter_Transit_Ok",      d_ok,      9, 17, 9, 17);
            var rc_weather = Encounter("Encounter_Transit_Météo",   d_weather, 7, 11, 7, 11);
            var rc_injury  = Encounter("Encounter_Transit_Blessure",d_injury,  8, 16, 8, 16);
            var rc_cache   = Encounter("Encounter_Transit_Cache",   d_cache,   10, 15, 10, 15);
            var rc_threat  = Encounter("Encounter_Transit_Menace",  d_threat,  14, 20, 14, 20);
            var rc_arrive  = Encounter("Encounter_Arrivée_Maison",  d_arrive,  9, 12, 9, 12);

            // ── 3. Global encounter pool (transit days) ────────────────────────────

            var globalPool = ScriptableObject.CreateInstance<EncounterEventPool>();
            globalPool.poolID            = "pool_global_transit";
            globalPool.dailyEventChance  = 85f;
            globalPool.encounters        = new[]
            {
                new WeightedEncounter { weight = 40f, radioCall = rc_ok      },
                new WeightedEncounter { weight = 25f, radioCall = rc_weather },
                new WeightedEncounter { weight = 10f, radioCall = rc_injury  },
                new WeightedEncounter { weight = 15f, radioCall = rc_cache   },
                new WeightedEncounter { weight = 10f, radioCall = rc_threat  }
            };
            SaveAsset(globalPool, "Pool_Global_Transit");

            // ── 4. Arrival pool (last day) ─────────────────────────────────────────

            var arrivalPool = ScriptableObject.CreateInstance<EncounterEventPool>();
            arrivalPool.poolID           = "pool_arrivee";
            arrivalPool.dailyEventChance = 100f;  // always fires on arrival day
            arrivalPool.encounters       = new[]
            {
                new WeightedEncounter { weight = 100f, radioCall = rc_arrive }
            };
            SaveAsset(arrivalPool, "Pool_Arrivée");

            // ── 5. Example ExplorationZone ─────────────────────────────────────────

            var zone = ScriptableObject.CreateInstance<ExplorationZone>();
            zone.zoneName      = "Zone Commerciale";
            zone.daysFromBase  = 2;
            zone.description   = "Un vieux centre commercial à la lisière de la ville.";
            zone.zoneColor     = new Color(0.3f, 0.6f, 0.8f);
            zone.radioCalls    = new RadioCallEvent[0];  // no fixed calls — only encounters
            zone.encounterPool = arrivalPool;            // arrival pool on day 2
            SaveAsset(zone, "Zone_Commerciale");

            // ── 6. MissionData ─────────────────────────────────────────────────────

            var mission = ScriptableObject.CreateInstance<MissionData>();
            mission.missionID    = "mission_zone_commerciale";
            mission.displayName  = "Zone Commerciale (2 jours)";
            mission.zone         = zone;
            mission.radioCalls   = new RadioCallEvent[0]; // driven entirely by pools
            mission.encounterPool = null;                 // relies on zone + global pool
            mission.followUps    = new MissionFollowUp[0];
            mission.requiresLocationTrigger = false;
            SaveAsset(mission, "Mission_ZoneCommerciale");

            // ── Done ──────────────────────────────────────────────────────────────

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ExampleMissionCreator] Assets générés dans Assets/Data/Missions/Example/.\n" +
                      "→ Assignez 'Pool_Global_Transit' au champ Global Encounter Pool du RadioCallManager.\n" +
                      "→ Assignez 'Mission_ZoneCommerciale' à l'ExplorationPanel ou à un MissionTriggerZone.");

            EditorUtility.DisplayDialog(
                "Exemple généré !",
                "Assets créés dans Assets/Data/Missions/Example/\n\n" +
                "Étape suivante :\n" +
                "• Assignez Pool_Global_Transit au RadioCallManager (Global Encounter Pool).\n" +
                "• Lancez Mission_ZoneCommerciale depuis l'exploration panel.",
                "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static ExplorationDialogue Dialogue(
            string id, string speakerName, string text,
            bool hasLimit = false, float timeSec = 30f, int timeoutIdx = 0)
        {
            var d = ScriptableObject.CreateInstance<ExplorationDialogue>();
            d.dialogueID       = id;
            d.speakerName      = speakerName;
            d.dialogueText     = text;
            d.hasTimeLimit     = hasLimit;
            d.timeLimitSeconds = timeSec;
            d.timeoutChoiceIndex = timeoutIdx;
            d.nodeEvent        = new DialogueEventData();
            d.choices          = new DialogueChoice[0];
            return d;
        }

        private static RadioCallEvent Encounter(
            string assetName, ExplorationDialogue dialogue,
            int fixedH, int fixedM, int rndMin, int rndMax)
        {
            var rc = ScriptableObject.CreateInstance<RadioCallEvent>();
            rc.triggerDay   = 1;          // ignored for pool-drawn events
            rc.timeMode     = TriggerTimeMode.Random;
            rc.randomHourMin = rndMin;
            rc.randomHourMax = rndMax;
            rc.dialogue      = dialogue;
            rc.fireOnce      = false;     // pool events can repeat across missions
            SaveAsset(rc, "Encounters/" + assetName);
            return rc;
        }

        private static void Save(ExplorationDialogue d, string relativePath)
            => SaveAsset(d, relativePath);

        private static void SaveAsset(Object asset, string relativePath)
        {
            string path = $"{RootDir}/{relativePath}.asset";
            EnsureDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
