using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Manages exterior missions resolved day-by-day via DayManager ticks.
    /// Survivors depart immediately on launch (hidden from cameras).
    /// Provisions (food/water) chosen by the player are deducted at launch.
    /// </summary>
    public class MissionSystem : MonoBehaviour
    {
        public event Action<MissionResult>     OnMissionCompleted;
        public event Action<MissionDefinition> OnMissionStarted;

        private readonly List<ActiveMission> activeMissions = new List<ActiveMission>();

        // ── Mission catalog ──────────────────────────────────────────────────────

        public static readonly MissionDefinition[] AvailableMissions =
        {
            new MissionDefinition("Ville abandonnée",    "Une cité déserte à explorer.",
                successChance: 0.70f, durationDays: 2,
                rewardFood: 40, rewardWater: 20, rewardMaterials: 30, rewardMedicine: 10),
            new MissionDefinition("Supermarché pillé",   "Restes de nourriture à récupérer.",
                successChance: 0.80f, durationDays: 1,
                rewardFood: 60, rewardWater: 10, rewardMaterials:  5, rewardMedicine:  5),
            new MissionDefinition("Hôpital en ruine",    "Médicaments potentiels.",
                successChance: 0.55f, durationDays: 3,
                rewardFood: 10, rewardWater:  5, rewardMaterials:  0, rewardMedicine: 50),
            new MissionDefinition("Entrepôt industriel", "Matériaux de construction.",
                successChance: 0.65f, durationDays: 2,
                rewardFood:  5, rewardWater:  0, rewardMaterials: 80, rewardMedicine:  0),
        };

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Launches a mission. Survivors depart immediately (hidden from cameras).
        /// Provisions are deducted now; rewards arrive at resolution after DurationDays ticks.
        /// Returns false if team is empty or provisions are insufficient.
        /// </summary>
        public bool LaunchMission(MissionDefinition mission, List<SurvivorBehavior> team,
                                   ShelterResources resources, int provisionFood, int provisionWater)
        {
            if (mission == null || team == null || team.Count == 0) return false;

            if (resources.food < provisionFood || resources.water < provisionWater)
            {
                Debug.LogWarning("[MissionSystem] Provisions insuffisantes.");
                return false;
            }

            resources.food  -= provisionFood;
            resources.water -= provisionWater;

            ActiveMission active = new ActiveMission(mission, team, provisionFood, provisionWater);
            activeMissions.Add(active);

            foreach (SurvivorBehavior sb in team)
                sb.SetOnMission(true);

            OnMissionStarted?.Invoke(mission);
            Debug.Log($"[MissionSystem] '{mission.LocationName}' lancée — {team.Count} survivants, " +
                      $"provisions {provisionFood}N {provisionWater}E, durée {mission.DurationDays}j");
            return true;
        }

        /// <summary>Called by DayManager each day to advance all active missions.</summary>
        public void TickDay()
        {
            for (int i = activeMissions.Count - 1; i >= 0; i--)
            {
                ActiveMission m = activeMissions[i];
                m.DaysElapsed++;
                Debug.Log($"[MissionSystem] '{m.Definition.LocationName}' — J{m.DaysElapsed}/{m.Definition.DurationDays}");
                if (m.DaysElapsed >= m.Definition.DurationDays)
                    ResolveMission(m);
            }
        }

        public List<ActiveMission> GetActiveMissions() => new List<ActiveMission>(activeMissions);

        // ── Private ──────────────────────────────────────────────────────────────

        private void ResolveMission(ActiveMission active)
        {
            activeMissions.Remove(active);

            bool success = UnityEngine.Random.value <= active.Definition.SuccessChance;
            MissionResult result = new MissionResult(active.Definition, active.Team, success);

            if (success)
            {
                result.FoodGained      = active.Definition.RewardFood;
                result.WaterGained     = active.Definition.RewardWater;
                result.MaterialsGained = active.Definition.RewardMaterials;
                result.MedicineGained  = active.Definition.RewardMedicine;
            }
            else
            {
                foreach (SurvivorBehavior sb in active.Team)
                    if (UnityEngine.Random.value < 0.3f) { sb.MakeSick(); result.Casualties.Add(sb); }
            }

            foreach (SurvivorBehavior sb in active.Team)
                sb.SetOnMission(false);

            OnMissionCompleted?.Invoke(result);
        }
    }

    // ── Data classes ─────────────────────────────────────────────────────────────

    [Serializable]
    public class MissionDefinition
    {
        public string LocationName;
        public string Description;
        public float  SuccessChance;
        public int    DurationDays;
        public int    RewardFood;
        public int    RewardWater;
        public int    RewardMaterials;
        public int    RewardMedicine;

        public MissionDefinition(string name, string desc, float successChance, int durationDays,
            int rewardFood, int rewardWater, int rewardMaterials, int rewardMedicine)
        {
            LocationName    = name;
            Description     = desc;
            SuccessChance   = successChance;
            DurationDays    = durationDays;
            RewardFood      = rewardFood;
            RewardWater     = rewardWater;
            RewardMaterials = rewardMaterials;
            RewardMedicine  = rewardMedicine;
        }
    }

    public class ActiveMission
    {
        public MissionDefinition      Definition;
        public List<SurvivorBehavior> Team;
        public int DaysElapsed;
        public int ProvisionFood;
        public int ProvisionWater;

        public ActiveMission(MissionDefinition def, List<SurvivorBehavior> team, int provFood, int provWater)
        {
            Definition     = def;
            Team           = new List<SurvivorBehavior>(team);
            ProvisionFood  = provFood;
            ProvisionWater = provWater;
        }
    }

    public class MissionResult
    {
        public MissionDefinition      Definition;
        public List<SurvivorBehavior> Team;
        public bool Success;
        public int  FoodGained;
        public int  WaterGained;
        public int  MaterialsGained;
        public int  MedicineGained;
        public List<SurvivorBehavior> Casualties = new List<SurvivorBehavior>();

        public MissionResult(MissionDefinition def, List<SurvivorBehavior> team, bool success)
        {
            Definition = def;
            Team       = team;
            Success    = success;
        }
    }
}
