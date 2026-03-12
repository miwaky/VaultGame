using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Reads the validated daily schedule from ScheduleManager and sends each alive survivor
    /// to the room corresponding to their assigned task via NavMeshAgent.
    ///
    /// Call Execute() when the player clicks the "Valider" button in the Schedule panel.
    /// Multiple survivors can share the same destination — they spread to offset positions.
    ///
    /// Requires a RoomNavigationConfig asset assigned in the Inspector.
    /// </summary>
    public class ScheduleExecutor : MonoBehaviour
    {
        // ── Task → Room name mapping ──────────────────────────────────────────────
        // Must match the ShelterRoom.RoomName values configured in the scene.
        private static readonly Dictionary<DailyTask, string> TaskToRoomName = new Dictionary<DailyTask, string>
        {
            { DailyTask.SeReposer,          "Dorm"     },
            { DailyTask.SoccuperDeLeau,     "Water"    },
            { DailyTask.Soigner,            "Infirmary"},
            { DailyTask.SeFaireSoigner,     "Infirmary"},
            { DailyTask.SoccuperDeLaFerme,  "Farm"     },
            { DailyTask.SoccuperDuStockage, "Storage"  },
        };

        // ── State ─────────────────────────────────────────────────────────────────
        private SurvivorManager survivorManager;
        private ScheduleManager  scheduleManager;

        // Cached ShelterRoom instances indexed by RoomName.
        private readonly Dictionary<string, ShelterRoom> roomCache = new Dictionary<string, ShelterRoom>();

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            survivorManager = FindFirstObjectByType<SurvivorManager>();
            scheduleManager  = FindFirstObjectByType<ScheduleManager>();
            BuildRoomCache();

            // Auto-execute schedule at Work phase start
            DayCycleManager cycle = FindFirstObjectByType<DayCycleManager>();
            if (cycle != null) cycle.OnWorkStart += Execute;
        }

        private void OnDestroy()
        {
            DayCycleManager cycle = FindFirstObjectByType<DayCycleManager>();
            if (cycle != null) cycle.OnWorkStart -= Execute;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Validates the schedule and moves every alive, non-mission survivor to their room.
        /// Prefers explicit IdlePoints on the ShelterRoom; falls back to a random point
        /// inside the room trigger bounds when no spawn points are assigned.
        /// </summary>
        public void Execute()
        {
            if (survivorManager == null || scheduleManager == null)
            {
                survivorManager = FindFirstObjectByType<SurvivorManager>();
                scheduleManager  = FindFirstObjectByType<ScheduleManager>();
            }

            // Rebuild cache each time — rooms may have been added/removed since Start()
            BuildRoomCache();

            if (roomCache.Count == 0)
            {
                Debug.LogWarning("[ScheduleExecutor] Aucune ShelterRoom trouvée dans la scène. " +
                                 "Assure-toi que chaque salle a un composant ShelterRoom avec RoomName configuré.");
                return;
            }

            foreach (ShelterRoom room in roomCache.Values)
                room.ResetOccupancy();

            int dispatched = 0;

            foreach (SurvivorBehavior survivor in survivorManager.Survivors)
            {
                if (survivor == null || !survivor.IsAlive || survivor.IsOnMission) continue;

                DailyTask task = scheduleManager.GetTask(survivor);

                if (!TaskToRoomName.TryGetValue(task, out string roomName))
                {
                    Debug.LogWarning($"[ScheduleExecutor] Aucune salle mappée pour '{DailyTaskLabels.GetLabel(task)}'.");
                    continue;
                }

                if (!roomCache.TryGetValue(roomName, out ShelterRoom room))
                {
                    Debug.LogWarning($"[ScheduleExecutor] ShelterRoom '{roomName}' absente. " +
                                     $"Salles disponibles : {string.Join(", ", roomCache.Keys)}");
                    continue;
                }

                // Use SurvivorBehavior.MoveToRoom — updates CurrentRoom and handles NavMesh/teleport
                survivor.MoveToRoom(room);
                dispatched++;
                Debug.Log($"[ScheduleExecutor] {survivor.SurvivorName} → {roomName} ({DailyTaskLabels.GetLabel(task)})");
            }

            Debug.Log($"[ScheduleExecutor] {dispatched} survivant(s) envoyé(s) vers leurs salles.");
        }

        // ── Private ──────────────────────────────────────────────────────────────

        /// <summary>Scans all ShelterRoom components in the scene and indexes them by RoomName.</summary>
        private void BuildRoomCache()
        {
            roomCache.Clear();
            foreach (ShelterRoom room in FindObjectsByType<ShelterRoom>(FindObjectsSortMode.None))
            {
                string key = room.RoomName;
                if (!roomCache.ContainsKey(key))
                    roomCache[key] = room;
                else
                    Debug.LogWarning($"[ScheduleExecutor] RoomName en doublon : '{key}' — seule la première instance est utilisée.");
            }
            Debug.Log($"[ScheduleExecutor] Salles indexées : {string.Join(", ", roomCache.Keys)}");
        }
    }
}
