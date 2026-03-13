using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Runtime data for a single active exploration mission.
    /// Supports both zone-based missions (legacy) and data-driven <see cref="MissionData"/> missions.
    /// </summary>
    public class ActiveMission
    {
        public List<SurvivorBehavior> Survivors     { get; }
        public ExplorationZone        Zone           { get; }
        public MissionData            MissionDef     { get; }   // null for legacy zone-only missions
        public int                    StartDay       { get; }
        public int                    MissionDay     => currentMissionDay;
        public bool                   HasDeparted    { get; private set; }

        // Resources accumulated during the mission (applied physically on return)
        private readonly Dictionary<ResourceType, int> accumulatedResources =
            new Dictionary<ResourceType, int>();

        private int currentMissionDay = 0;

        // ── Constructors ──────────────────────────────────────────────────────────

        /// <summary>Legacy constructor — zone owns the radio calls.</summary>
        public ActiveMission(List<SurvivorBehavior> survivors, ExplorationZone zone, int startDay)
        {
            Survivors  = survivors;
            Zone       = zone;
            MissionDef = null;
            StartDay   = startDay;
            ResetCallFlags(zone?.radioCalls);
        }

        /// <summary>MissionData constructor — mission asset owns the radio calls.</summary>
        public ActiveMission(List<SurvivorBehavior> survivors, MissionData mission, int startDay)
        {
            Survivors  = survivors;
            Zone       = mission?.zone;
            MissionDef = mission;
            StartDay   = startDay;
            ResetCallFlags(mission?.radioCalls);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Called every day-start. Advances mission day and sets survivors active.</summary>
        public void Tick(int absoluteDay)
        {
            if (!HasDeparted)
            {
                HasDeparted = true;
                foreach (SurvivorBehavior s in Survivors)
                    s.SetOnMission(true);

                string dest = MissionDef != null ? MissionDef.displayName : Zone?.zoneName ?? "?";
                Debug.Log($"[ActiveMission] Départ des explorateurs → {dest}.");
            }

            currentMissionDay++;
        }

        /// <summary>Returns radio calls that should fire on the current mission day.</summary>
        public IEnumerable<RadioCallEvent> PendingCalls()
        {
            // MissionData owns the calls when defined; fall back to Zone.
            RadioCallEvent[] calls = MissionDef?.radioCalls ?? Zone?.radioCalls;
            if (calls == null) yield break;

            foreach (RadioCallEvent rc in calls)
            {
                if (rc == null) continue;
                if (rc.triggerDay != currentMissionDay) continue;
                if (rc.fireOnce && rc.HasFired) continue;
                yield return rc;
            }
        }

        /// <summary>Accumulates resources to be physically placed on shelves when the mission ends.</summary>
        public void AccumulateResource(ResourceType type, int amount)
        {
            if (!accumulatedResources.ContainsKey(type))
                accumulatedResources[type] = 0;
            accumulatedResources[type] += amount;
        }

        /// <summary>Read-only access to accumulated resources for spawning on return.</summary>
        public IReadOnlyDictionary<ResourceType, int> AccumulatedResources => accumulatedResources;

        // ── Private ───────────────────────────────────────────────────────────────

        private static void ResetCallFlags(RadioCallEvent[] calls)
        {
            if (calls == null) return;
            foreach (RadioCallEvent rc in calls)
                if (rc != null) rc.HasFired = false;
        }
    }

    /// <summary>
    /// Orchestrates exploration missions:
    /// — Receives a scheduled exploration from <see cref="ExplorationPanelUI"/>.
    /// — On the next day's Work start (07:00), sets survivors to OnMission.
    /// — Each subsequent day, checks for <see cref="RadioCallEvent"/>s and fires them via <see cref="DialogueManager"/>.
    /// </summary>
    public class RadioCallManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static RadioCallManager Instance { get; private set; }

        // ── Active missions ───────────────────────────────────────────────────────
        private readonly List<ActiveMission>                   activeMissions = new List<ActiveMission>();
        private readonly Queue<(RadioCallEvent, ActiveMission)> callQueue      = new Queue<(RadioCallEvent, ActiveMission)>();

        // ── Dependencies ──────────────────────────────────────────────────────────
        private DayManager      dayManager;
        private DayCycleManager dayCycleManager;
        private DialogueManager dialogueManager;

        // ── Pending (scheduled for next day's departure) ──────────────────────────
        private readonly List<(List<SurvivorBehavior>, ExplorationZone)> pendingMissions =
            new List<(List<SurvivorBehavior>, ExplorationZone)>();

        // MissionData-based pending missions
        private readonly List<(List<SurvivorBehavior>, MissionData)> pendingDataMissions =
            new List<(List<SurvivorBehavior>, MissionData)>();

        // ── Follow-up calls (scheduled mid-mission via dialogue) ──────────────────
        // (RadioCallEvent, ActiveMission, missionDay on which it should fire)
        private readonly List<(RadioCallEvent, ActiveMission, int)> followUpCalls =
            new List<(RadioCallEvent, ActiveMission, int)>();

        // ── Today's timed calls ───────────────────────────────────────────────────
        // Resolved at OnWorkStart; fired when the clock passes their trigger minute.
        // (RadioCallEvent, ActiveMission, triggerMinuteOfDay, alreadyFired)
        private readonly List<(RadioCallEvent rc, ActiveMission mission, int triggerMinute, bool fired)>
            scheduledToday = new List<(RadioCallEvent, ActiveMission, int, bool)>();

        // ── Public read ───────────────────────────────────────────────────────────
        public IReadOnlyList<ActiveMission> ActiveMissions => activeMissions;

        /// <summary>Zone-based missions scheduled for next 07:00 departure.</summary>
        public IReadOnlyList<(List<SurvivorBehavior> survivors, ExplorationZone zone)>
            PendingZoneMissions => pendingMissions;

        /// <summary>Data-driven missions scheduled for next 07:00 departure.</summary>
        public IReadOnlyList<(List<SurvivorBehavior> survivors, MissionData data)>
            PendingDataMissions => pendingDataMissions;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Tooltip("Global encounter pool applied to every mission every day. " +
                 "Zone/Mission-specific pools are merged on top of this one.")]
        [SerializeField] private EncounterEventPool globalEncounterPool;

        [Header("Debug")]
        [Tooltip("Force a radio call at 08:00 every mission day, regardless of pool chance. TEST ONLY.")]
        [SerializeField] private bool debugForceCallAt10h = false;
        [Tooltip("RadioCallEvent to use when debugForceCallAt10h is active.")]
        [SerializeField] private RadioCallEvent debugForcedCall;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            dayManager      = FindFirstObjectByType<DayManager>();
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
            dialogueManager = FindFirstObjectByType<DialogueManager>();

            if (dayManager == null)
                Debug.LogWarning("[RadioCallManager] DayManager introuvable.");

            if (dayCycleManager != null)
            {
                dayCycleManager.OnPreWorkStart  += OnPreWorkStart;
                dayCycleManager.OnWorkStart     += OnWorkStart;
                dayCycleManager.OnPostWorkStart += OnPostWorkStart;
                dayCycleManager.OnTimeChanged   += OnTimeChanged;
            }
            else
            {
                Debug.LogWarning("[RadioCallManager] DayCycleManager introuvable.");
            }
        }

        private void OnDestroy()
        {
            if (dayCycleManager != null)
            {
                dayCycleManager.OnPreWorkStart  -= OnPreWorkStart;
                dayCycleManager.OnWorkStart     -= OnWorkStart;
                dayCycleManager.OnPostWorkStart -= OnPostWorkStart;
                dayCycleManager.OnTimeChanged   -= OnTimeChanged;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Schedules an exploration. Survivors will depart at 07:00 the next day.
        /// </summary>
        public void ScheduleExploration(List<SurvivorBehavior> survivors, ExplorationZone zone)
        {
            if (survivors == null || survivors.Count == 0 || zone == null) return;
            pendingMissions.Add((new List<SurvivorBehavior>(survivors), zone));
            Debug.Log($"[RadioCallManager] Exploration planifiée → {zone.zoneName} ({survivors.Count} survivant(s)) — départ demain 07:00.");
        }

        /// <summary>
        /// Starts a <see cref="MissionData"/>-driven mission immediately (next OnWorkStart tick).
        /// <para>If <paramref name="survivorSource"/> is provided those survivors carry over;
        /// otherwise the mission must be launched from the exploration panel with survivors selected.</para>
        /// </summary>
        public void StartMissionFromData(MissionData mission, ActiveMission survivorSource)
        {
            if (mission == null) return;

            List<SurvivorBehavior> survivors = survivorSource != null
                ? new List<SurvivorBehavior>(survivorSource.Survivors)
                : new List<SurvivorBehavior>();  // trigger zone path: no survivors pre-assigned

            // If triggered mid-mission from a dialogue, end the old mission silently
            // (survivors don't return home — they continue into the new mission)
            if (survivorSource != null && activeMissions.Contains(survivorSource))
            {
                activeMissions.Remove(survivorSource);
                Debug.Log($"[RadioCallManager] Ancienne mission terminée — enchaînement vers '{mission.missionID}'.");
            }

            pendingDataMissions.Add((survivors, mission));
            Debug.Log($"[RadioCallManager] Mission '{mission.missionID}' planifiée — départ demain 07:00.");
        }

        /// <summary>
        /// Schedules a follow-up radio call from within a dialogue tree.
        /// The call fires after <paramref name="delayDays"/> mission days from now.
        /// </summary>
        public void ScheduleFollowUpCall(RadioCallEvent call, ActiveMission mission, int delayDays)
        {
            if (call == null || mission == null) return;
            int targetDay = mission.MissionDay + delayDays;
            followUpCalls.Add((call, mission, targetDay));
            Debug.Log($"[RadioCallManager] Follow-up '{call.name}' programmé au jour mission {targetDay}.");
        }

        /// <summary>
        /// Immediately recalls all survivors from a mission and spawns accumulated
        /// resources physically onto the storage shelves.
        /// </summary>
        public void RecallMission(ActiveMission mission)
        {
            if (!activeMissions.Contains(mission)) return;

            // ── Return survivors ──────────────────────────────────────────────────
            foreach (SurvivorBehavior s in mission.Survivors)
                s.SetOnMission(false);

            // ── Spawn gathered resources on shelves ───────────────────────────────
            StorageSpawner spawner = StorageSpawner.Instance;
            if (spawner != null)
            {
                foreach (KeyValuePair<ResourceType, int> kv in mission.AccumulatedResources)
                {
                    int placed = spawner.SpawnItems(kv.Key, kv.Value);
                    Debug.Log($"[RadioCallManager] Retour mission : +{placed} {kv.Key} sur les étagères.");
                }
            }
            else
            {
                Debug.LogWarning("[RadioCallManager] StorageSpawner absent — ressources non matérialisées.");
            }

            activeMissions.Remove(mission);
            Debug.Log($"[RadioCallManager] Mission vers {mission.Zone?.zoneName} terminée.");
        }

        // ── Private ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Fires at 06:00. Pending missions are activated here so survivors disappear
        /// the moment the new day starts, not one hour later at 07:00.
        /// </summary>
        private void OnPreWorkStart()
        {
            int today = dayManager != null ? dayManager.CurrentDay : 0;

            foreach ((List<SurvivorBehavior> survivors, ExplorationZone zone) in pendingMissions)
            {
                ActiveMission mission = new ActiveMission(survivors, zone, today);
                mission.Tick(today);   // sets HasDeparted = true, calls SetOnMission(true)
                activeMissions.Add(mission);
            }
            pendingMissions.Clear();

            foreach ((List<SurvivorBehavior> survivors, MissionData data) in pendingDataMissions)
            {
                ActiveMission mission = new ActiveMission(survivors, data, today);
                mission.Tick(today);
                activeMissions.Add(mission);
            }
            pendingDataMissions.Clear();
        }

        /// <summary>
        /// Fires at 19:00. Non-mission survivors move to the Dorm until 07:00.
        /// </summary>
        private void OnPostWorkStart()
        {
            SurvivorManager survivorManager = FindFirstObjectByType<SurvivorManager>();
            if (survivorManager == null) return;

            // Find the Dorm room (same key as ScheduleExecutor).
            ShelterRoom dorm = null;
            foreach (ShelterRoom room in FindObjectsByType<ShelterRoom>(FindObjectsSortMode.None))
            {
                if (room.RoomName == "Dorm") { dorm = room; break; }
            }

            if (dorm == null)
            {
                Debug.LogWarning("[RadioCallManager] Salle 'Dorm' introuvable — retour nuit ignoré.");
                return;
            }

            foreach (SurvivorBehavior s in survivorManager.Survivors)
            {
                if (s == null || !s.IsAlive || s.IsOnMission) continue;
                s.MoveToRoom(dorm);
            }

            Debug.Log("[RadioCallManager] 19:00 — PNJ envoyés dans la Dorm pour la nuit.");
        }

        /// <summary>Fires at 07:00 each in-game day via DayCycleManager.OnWorkStart.</summary>
        private void OnWorkStart()
        {
            int today = dayManager != null ? dayManager.CurrentDay : 0;

            // Clear previous day's schedule
            scheduledToday.Clear();

            // ── Debug override — schedule forced call at 10:00 for ALL active missions ─
            // Runs before the skip-check so day-1 missions are also covered.
            if (debugForceCallAt10h && debugForcedCall != null)
            {
                foreach (ActiveMission mission in activeMissions)
                {
                    if (!mission.HasDeparted) continue;
                    debugForcedCall.HasFired    = false;
                    debugForcedCall.timeMode    = TriggerTimeMode.Fixed;
                    debugForcedCall.fixedHour   = 8;
                    debugForcedCall.fixedMinute = 0;
                    ScheduleCall(debugForcedCall, mission);
                    Debug.Log($"[RadioCallManager] DEBUG — appel forcé à 08:00 pour mission {mission.Zone?.zoneName ?? mission.MissionDef?.missionID}.");
                }
            }

            // ── Tick active missions and schedule today's radio calls ─────────────
            // Missions activated at 06:00 (OnPreWorkStart) have MissionDay==1 and
            // StartDay==today: their first Tick already ran, skip them here.
            foreach (ActiveMission mission in activeMissions)
            {
                if (!mission.HasDeparted) continue;
                if (mission.MissionDay == 1 && mission.StartDay == today) continue;

                mission.Tick(today);

                // Zone-defined calls
                foreach (RadioCallEvent rc in mission.PendingCalls())
                {
                    rc.HasFired = true;
                    ScheduleCall(rc, mission);
                }

                // Random encounter draw (global pool + zone/mission pool)
                RadioCallEvent encounter = DrawEncounter(mission);
                if (encounter != null)
                    ScheduleCall(encounter, mission);

                // Follow-up calls scheduled dynamically from dialogue trees
                for (int i = followUpCalls.Count - 1; i >= 0; i--)
                {
                    (RadioCallEvent rc, ActiveMission m, int targetDay) = followUpCalls[i];
                    if (m != mission || mission.MissionDay != targetDay) continue;
                    ScheduleCall(rc, mission);
                    followUpCalls.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Merges global + zone/mission pools and draws one encounter event.
        /// Returns null if no event fires this day.
        /// </summary>
        private RadioCallEvent DrawEncounter(ActiveMission mission)
        {
            EncounterEventPool zonePool = mission.MissionDef?.encounterPool
                                       ?? mission.Zone?.encounterPool;

            // Both null — nothing to draw
            if (globalEncounterPool == null && zonePool == null) return null;

            // Only one pool — draw directly
            if (globalEncounterPool == null) return zonePool.TryDraw();
            if (zonePool == null)            return globalEncounterPool.TryDraw();

            // Merge: build a combined weighted list and draw once
            var merged = new System.Collections.Generic.List<WeightedEncounter>();
            if (globalEncounterPool.encounters != null)
                merged.AddRange(globalEncounterPool.encounters);
            if (zonePool.encounters != null)
                merged.AddRange(zonePool.encounters);

            // Use the higher of the two daily chances
            float chance = Mathf.Max(globalEncounterPool.dailyEventChance,
                                     zonePool.dailyEventChance);

            if (UnityEngine.Random.Range(0f, 100f) > chance) return null;

            float total = 0f;
            foreach (WeightedEncounter e in merged)
                if (e.radioCall != null) total += e.weight;

            if (total <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;
            foreach (WeightedEncounter e in merged)
            {
                if (e.radioCall == null) continue;
                cumulative += e.weight;
                if (roll <= cumulative) return e.radioCall;
            }
            return null;
        }

        /// <summary>
        /// Resolves the trigger time of <paramref name="rc"/> and adds it to today's schedule.
        /// </summary>
        private void ScheduleCall(RadioCallEvent rc, ActiveMission mission)
        {
            int triggerMinute = rc.ResolveTriggerMinutes();
            scheduledToday.Add((rc, mission, triggerMinute, false));

            int h = triggerMinute / 60;
            int m = triggerMinute % 60;
            Debug.Log($"[RadioCallManager] Appel '{rc.name}' prévu à {h:D2}:{m:D2}.");
        }

        /// <summary>
        /// Fires every in-game minute via DayCycleManager.OnTimeChanged.
        /// Checks scheduledToday and enqueues calls whose trigger time has been reached.
        /// </summary>
        private void OnTimeChanged(int hour, int minute)
        {
            int currentMinute = hour * 60 + minute;
            bool anyFired = false;

            for (int i = 0; i < scheduledToday.Count; i++)
            {
                (RadioCallEvent rc, ActiveMission mission, int triggerMinute, bool fired) = scheduledToday[i];
                if (fired || currentMinute < triggerMinute) continue;

                Debug.Log($"[RadioCallManager] ⏰ {hour:D2}:{minute:D2} — déclenchement appel '{rc.name}' (prévu à {triggerMinute/60:D2}:{triggerMinute%60:D2})");
                callQueue.Enqueue((rc, mission));
                scheduledToday[i] = (rc, mission, triggerMinute, true);
                anyFired = true;
            }

            if (anyFired) DrainCallQueue();
        }

        private void DrainCallQueue()
        {
            if (dialogueManager == null)
            {
                dialogueManager = FindFirstObjectByType<DialogueManager>();
                if (dialogueManager == null)
                {
                    Debug.LogError("[RadioCallManager] ❌ DialogueManager introuvable — appels radio ignorés.");
                    callQueue.Clear();
                    return;
                }
            }

            if (callQueue.Count == 0) return;

            (RadioCallEvent rc, ActiveMission mission) = callQueue.Dequeue();

            Debug.Log($"[RadioCallManager] ▶ DrainCallQueue → StartDialogue('{rc.name}', dialogue={rc.dialogue?.name ?? "NULL"})");

            if (rc.dialogue == null)
            {
                Debug.LogError($"[RadioCallManager] ❌ RadioCallEvent '{rc.name}' n'a pas de dialogue assigné — appel ignoré.");
                DrainCallQueue();
                return;
            }

            // Chain: after dialogue ends, drain the next call
            dialogueManager.StartDialogue(rc, mission, () => DrainCallQueue());
        }
    }
}
