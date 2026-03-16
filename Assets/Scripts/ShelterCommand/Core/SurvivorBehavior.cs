using System;
using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Single source of truth for a survivor.
    /// Holds the generated profile, needs, room reference, and gameplay logic.
    /// SurvivorInitializer creates and configures this component entirely by code —
    /// no prefab configuration required.
    /// </summary>
    public class SurvivorBehavior : MonoBehaviour
    {
        // ── Profile (set by SurvivorInitializer.SetProfile) ──────────────────────
        public SurvivorGeneratedProfile GeneratedProfile { get; private set; }

        // ── Starting room (set by SurvivorInitializer.SetStartingRoom) ───────────
        private ShelterRoom startingRoom;

        // ── Runtime needs ────────────────────────────────────────────────────────
        private int hunger  = 50;
        private int fatigue = 30;
        private int stress  = 20;

        // ── Runtime state ────────────────────────────────────────────────────────
        public bool      IsAlive      { get; private set; } = true;
        public bool      IsSick       { get; private set; } = false;
        public bool      IsArrested   { get; private set; } = false;
        public bool      IsOnMission  { get; private set; } = false;
        public bool      IsWorking    { get; private set; } = false;
        public OrderType CurrentOrder { get; private set; } = OrderType.GoSleep;
        public ShelterRoom CurrentRoom { get; private set; }

        // ── Accessors ────────────────────────────────────────────────────────────
        public string SurvivorName => GeneratedProfile != null ? GeneratedProfile.survivorName : gameObject.name;
        public int    Hunger       => hunger;
        public int    Fatigue      => fatigue;
        public int    Stress       => stress;
        public int    Morale       => ComputeMorale();

        // Legacy shim — existing HUD scripts that read Data get null gracefully
        public SurvivorData Data => null;

        // ── Events ───────────────────────────────────────────────────────────────
        public event Action<SurvivorBehavior>            OnNeedsChanged;
        public event Action<SurvivorBehavior, OrderType> OnOrderReceived;
        public event Action<SurvivorBehavior>            OnSurvivorDied;

        // ── Constants ────────────────────────────────────────────────────────────
        private const int HungerPerDay   = 12;
        private const int FatiguePerDay  = 10;
        private const int StressPerDay   = 5;
        private const int DeathHunger    = 100;
        private const int LowMoraleLimit = 20;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Guarantee a collider so raycasts can hit this survivor
            if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
            {
                CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.5f;
                col.center = new Vector3(0f, 0.9f, 0f);
            }
        }

        // ── Setup API (called exclusively by SurvivorInitializer) ────────────────

        /// <summary>
        /// Assigns the fully generated profile. Must be called before PlaceInStartingRoom().
        /// </summary>
        public void SetProfile(SurvivorGeneratedProfile profile)
        {
            GeneratedProfile = profile;
            gameObject.name  = profile.survivorName;
        }

        /// <summary>
        /// Assigns the ShelterRoom trigger where this survivor appears on day 1.
        /// </summary>
        public void SetStartingRoom(ShelterRoom room)
        {
            // Guard against destroyed Unity objects
            startingRoom = room != null && room.gameObject != null ? room : null;
            CurrentRoom  = startingRoom;
        }

        /// <summary>
        /// Warps the survivor to a free point inside their starting room.
        /// Must be called after the NavMeshAgent is on the mesh.
        /// </summary>
        public void PlaceInStartingRoom()
        {
            if (startingRoom == null || !startingRoom.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[SurvivorBehavior] {SurvivorName} : startingRoom null ou détruite — spawn ignoré.");
                return;
            }

            Vector3 target = startingRoom.GetRandomSpawnPoint();
            NavMeshAgent agent = GetComponent<NavMeshAgent>();

            if (agent != null && agent.isOnNavMesh)
                agent.Warp(target);
            else
                transform.position = target;
        }

        // ── Gameplay API ─────────────────────────────────────────────────────────

        /// <summary>Advances needs by one day and executes the current order.</summary>
        public void TickDay(ShelterResources resources)
        {
            if (!IsAlive || IsOnMission) return;

            Add(ref hunger,  HungerPerDay);
            Add(ref fatigue, FatiguePerDay);
            Add(ref stress,  StressPerDay);

            ExecuteOrder(resources);

            if (Morale < LowMoraleLimit) ApplyLowMoraleEffect(resources);
            if (hunger >= DeathHunger || (IsSick && fatigue >= DeathHunger)) Kill();

            OnNeedsChanged?.Invoke(this);
        }

        /// <summary>Issues an order. Returns false if refused due to low morale.</summary>
        public bool IssueOrder(OrderType order, ShelterResources resources)
        {
            if (!IsAlive || IsArrested || IsOnMission) return false;
            if (Morale < LowMoraleLimit && !HasHighLoyalty()) { Add(ref stress, 10); return false; }
            CurrentOrder = order;
            OnOrderReceived?.Invoke(this, order);
            return true;
        }

        /// <summary>Sends this survivor to a room via NavMesh.</summary>
        public void MoveToRoom(ShelterRoom room)
        {
            if (room == null || IsOnMission) return;
            CurrentRoom = room;

            // Stop any ongoing idle movement so it doesn't overwrite our destination
            SurvivorIdleMovement idle = GetComponent<SurvivorIdleMovement>();
            if (idle != null) idle.enabled = false;

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            Vector3 target = room.GetRandomSpawnPoint();

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped      = false;
                agent.updatePosition = true;
                agent.updateRotation = true;

                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target);
                }
                else
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        agent.SetDestination(hit.position);
                    }
                    else
                    {
                        transform.position = target;
                    }
                }
            }
            else
            {
                transform.position = target;
                Debug.LogWarning($"[SurvivorBehavior] {SurvivorName} : pas de NavMeshAgent actif — téléportation.");
            }
        }

        public void Arrest()   { IsArrested = true;  Add(ref stress, 20); OnNeedsChanged?.Invoke(this); }
        public void Release()  { IsArrested = false; OnNeedsChanged?.Invoke(this); }
        public void MakeSick() { IsSick = true;  Add(ref stress, 15); Add(ref fatigue, 20); OnNeedsChanged?.Invoke(this); }
        public void Heal()     { IsSick = false; Add(ref stress, -10); OnNeedsChanged?.Invoke(this); }

        /// <summary>Called by SurvivorWorkZone when the survivor enters/exits a work area.</summary>
        public void SetWorking(bool working) { IsWorking = working; }

        public void SetOnMission(bool onMission)
        {
            IsOnMission = onMission;
            gameObject.SetActive(!onMission);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private int ComputeMorale()
        {
            int m = 100 - hunger / 2 - fatigue / 3 - stress / 2;
            if (IsSick)     m -= 20;
            if (IsArrested) m -= 30;
            return Mathf.Clamp(m, 0, 100);
        }

        private static void Add(ref int field, int delta)
            => field = Mathf.Clamp(field + delta, 0, 100);

        private void ExecuteOrder(ShelterResources resources)
        {
            switch (CurrentOrder)
            {
                case OrderType.GoEat:
                    if (resources.food > 0) { resources.food = Mathf.Max(0f, resources.food - 5f); Add(ref hunger, -30); }
                    break;
                case OrderType.GoSleep:
                    Add(ref fatigue, -25); Add(ref stress, -5);
                    break;
                case OrderType.GoToInfirmary:
                    if (resources.medicine > 0) { resources.medicine = Mathf.Max(0, resources.medicine - 2); Heal(); Add(ref stress, -10); }
                    break;
                case OrderType.RepairGenerator:
                    if (GeneratedProfile != null && GeneratedProfile.Technique > 40)
                        resources.energy = Mathf.Min(100, resources.energy + 15);
                    Add(ref fatigue, 10);
                    break;
                case OrderType.TransportResources:
                    resources.materials = Mathf.Min(500, resources.materials + 5);
                    Add(ref fatigue, 8);
                    break;
                case OrderType.CraftTools:
                    if (resources.materials >= 10) { resources.materials -= 10; resources.energy = Mathf.Min(100, resources.energy + 5); }
                    Add(ref fatigue, 12);
                    break;
                case OrderType.PatrolZone:
                    Add(ref stress, 8); Add(ref fatigue, 8);
                    break;
            }
        }

        private void ApplyLowMoraleEffect(ShelterResources resources)
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < 20) { resources.food = Mathf.Max(0f, resources.food - UnityEngine.Random.Range(5f, 15f)); Add(ref hunger, -20); }
            else if (roll < 35) Add(ref stress, 10);
        }

        private void Kill()
        {
            IsAlive = false;
            OnSurvivorDied?.Invoke(this);
            Debug.Log($"[SurvivorBehavior] {SurvivorName} est mort.");
        }

        private bool HasHighLoyalty()
        {
            if (GeneratedProfile != null)
                return GeneratedProfile.positiveTrait == PositiveTrait.Loyal ||
                       GeneratedProfile.GetStat(SurvivorStatIndex.Social) >= 60;
            return false;
        }
    }
}
