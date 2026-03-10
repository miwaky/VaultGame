using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Runtime state and behaviour of a single survivor inside the shelter.
    /// Handles needs, orders, room movement, and mission absence.
    /// </summary>
    public class SurvivorBehavior : MonoBehaviour
    {
        // ── Serialized fields ────────────────────────────────────────────────────
        [Header("Survivor Profile")]
        [SerializeField] private SurvivorData data;

        /// <summary>Fully generated profile (stats, profession, traits). Set by SurvivorInitializer.</summary>
        public SurvivorGeneratedProfile GeneratedProfile { get; private set; }

        [Header("Starting Room")]
        [SerializeField] private ShelterRoomType startRoom = ShelterRoomType.Dormitory;

        [Header("Runtime Needs (0-100)")]
        [SerializeField, Range(0, 100)] private int hunger = 50;
        [SerializeField, Range(0, 100)] private int fatigue = 30;
        [SerializeField, Range(0, 100)] private int stress = 20;

        // ── Events ───────────────────────────────────────────────────────────────
        public event Action<SurvivorBehavior> OnNeedsChanged;
        public event Action<SurvivorBehavior, OrderType> OnOrderReceived;
        public event Action<SurvivorBehavior> OnSurvivorDied;

        // ── Accessors ────────────────────────────────────────────────────────────
        public SurvivorData Data          => data;
        public string SurvivorName        => GeneratedProfile != null ? GeneratedProfile.survivorName :
                                             (data != null ? data.survivorName : gameObject.name);
        public int    Hunger              => hunger;
        public int    Fatigue             => fatigue;
        public int    Stress              => stress;
        public int    Morale              => CalculateMorale();
        public bool   IsAlive             { get; private set; } = true;
        public bool   IsSick              { get; private set; } = false;
        public bool   IsArrested          { get; private set; } = false;
        public bool   IsOnMission         { get; private set; } = false;
        public OrderType CurrentOrder     { get; private set; } = OrderType.GoSleep;
        public ShelterRoomType CurrentRoom{ get; private set; }

        // ── Constants ────────────────────────────────────────────────────────────
        private const int HungerDailyIncrease      = 12;
        private const int FatigueDailyIncrease     = 10;
        private const int StressDailyIncrease      = 5;
        private const int DeathThreshold           = 100;
        private const int RefuseOrderMoraleThreshold = 20;

        // Spawn positions per room, registered by SurvivorRoomRegistry at scene start
        private static readonly System.Collections.Generic.Dictionary<ShelterRoomType, Vector3[]>
            RoomSpawns = new System.Collections.Generic.Dictionary<ShelterRoomType, Vector3[]>();

        // Index used to spread survivors across a room's spawn slots
        private static readonly System.Collections.Generic.Dictionary<ShelterRoomType, int>
            SpawnCounters = new System.Collections.Generic.Dictionary<ShelterRoomType, int>();

        private Renderer[] cachedRenderers;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(includeInactive: false);
            CurrentRoom = startRoom;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Assigns SurvivorData at runtime (called by SurvivorManager).</summary>
        public void SetData(SurvivorData survivorData) => data = survivorData;

        /// <summary>Assigns the generated profile at runtime (called by SurvivorInitializer).</summary>
        public void SetGeneratedProfile(SurvivorGeneratedProfile profile)
        {
            GeneratedProfile = profile;
            // Keep the legacy data name in sync so existing HUD code still works.
            if (data == null && profile != null)
                gameObject.name = profile.survivorName;
        }

        /// <summary>Registers world-space spawn points for a room so survivors can teleport to them.</summary>
        public static void RegisterRoomSpawns(ShelterRoomType room, Vector3[] positions)
        {
            RoomSpawns[room] = positions;
            SpawnCounters[room] = 0;
        }

        /// <summary>Teleports this survivor to an available slot in the target room.</summary>
        public void SetRoom(ShelterRoomType room)
        {
            CurrentRoom = room;
            if (!IsOnMission && RoomSpawns.TryGetValue(room, out Vector3[] positions) && positions.Length > 0)
            {
                int idx = SpawnCounters.TryGetValue(room, out int c) ? c : 0;
                transform.position = positions[idx % positions.Length];
                SpawnCounters[room] = idx + 1;
            }
        }

        /// <summary>Hides or shows this survivor (used when departing/returning from mission).</summary>
        public void SetOnMission(bool onMission)
        {
            IsOnMission = onMission;
            // Use gameObject active state to guarantee full invisibility (no renderer quirks)
            gameObject.SetActive(!onMission);
        }

        /// <summary>Advances needs by one in-game day.</summary>
        public void TickDay(ShelterResources resources)
        {
            if (!IsAlive || IsOnMission) return;

            ModifyHunger(HungerDailyIncrease);
            ModifyFatigue(FatigueDailyIncrease);
            ModifyStress(StressDailyIncrease);
            ExecuteOrderEffects(resources);

            if (Morale < RefuseOrderMoraleThreshold)
                ApplyLowMoraleEffect(resources);

            if (hunger >= DeathThreshold || (IsSick && fatigue >= DeathThreshold))
                KillSurvivor();

            OnNeedsChanged?.Invoke(this);
        }

        /// <summary>Issues an order. Returns false if refused due to low morale.</summary>
        public bool IssueOrder(OrderType order, ShelterResources resources)
        {
            if (!IsAlive || IsArrested || IsOnMission) return false;
            if (Morale < RefuseOrderMoraleThreshold && !HasHighLoyalty())
            {
                ModifyStress(10);
                return false;
            }
            CurrentOrder = order;
            OnOrderReceived?.Invoke(this, order);
            return true;
        }

        public void Arrest()    { IsArrested = true;  ModifyStress(20);  OnNeedsChanged?.Invoke(this); }
        public void Release()   { IsArrested = false; OnNeedsChanged?.Invoke(this); }
        public void MakeSick()  { IsSick = true;  ModifyStress(15); ModifyFatigue(20); OnNeedsChanged?.Invoke(this); }
        public void Heal()      { IsSick = false; ModifyStress(-10); OnNeedsChanged?.Invoke(this); }

        // ── Private helpers ──────────────────────────────────────────────────────

        private int CalculateMorale()
        {
            int m = 100 - hunger / 2 - fatigue / 3 - stress / 2;
            if (IsSick)     m -= 20;
            if (IsArrested) m -= 30;
            return Mathf.Clamp(m, 0, 100);
        }

        private void ModifyHunger(int d)  => hunger  = Mathf.Clamp(hunger  + d, 0, 100);
        private void ModifyFatigue(int d) => fatigue = Mathf.Clamp(fatigue + d, 0, 100);
        private void ModifyStress(int d)  => stress  = Mathf.Clamp(stress  + d, 0, 100);

        private void ExecuteOrderEffects(ShelterResources resources)
        {
            switch (CurrentOrder)
            {
                case OrderType.GoEat:
                    if (resources.food > 0) { resources.food = Mathf.Max(0, resources.food - 5); ModifyHunger(-30); }
                    SetRoom(ShelterRoomType.Cafeteria);
                    break;
                case OrderType.GoSleep:
                    ModifyFatigue(-25); ModifyStress(-5);
                    SetRoom(ShelterRoomType.Dormitory);
                    break;
                case OrderType.GoToInfirmary:
                    if (resources.medicine > 0) { resources.medicine = Mathf.Max(0, resources.medicine - 2); Heal(); ModifyStress(-10); }
                    break;
                case OrderType.RepairGenerator:
                    if (data != null && data.technical > 40) resources.energy = Mathf.Min(100, resources.energy + 15);
                    ModifyFatigue(10);
                    break;
                case OrderType.TransportResources:
                    resources.materials = Mathf.Min(500, resources.materials + 5);
                    ModifyFatigue(8);
                    SetRoom(ShelterRoomType.Storage);
                    break;
                case OrderType.CraftTools:
                    if (resources.materials >= 10) { resources.materials -= 10; resources.energy = Mathf.Min(100, resources.energy + 5); }
                    ModifyFatigue(12);
                    break;
                case OrderType.PatrolZone:
                    ModifyStress(8); ModifyFatigue(8);
                    SetRoom(ShelterRoomType.Entrance);
                    break;
            }
        }

        private void ApplyLowMoraleEffect(ShelterResources resources)
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < 20)
            {
                int stolen = UnityEngine.Random.Range(5, 15);
                resources.food = Mathf.Max(0, resources.food - stolen);
                ModifyHunger(-20);
            }
            else if (roll < 35)
            {
                ModifyStress(10);
            }
        }

        private void KillSurvivor()
        {
            IsAlive = false;
            OnSurvivorDied?.Invoke(this);
            Debug.Log($"[SurvivorBehavior] {SurvivorName} est mort.");
        }

        /// <summary>Returns true if this survivor has sufficient loyalty to follow orders under low morale.</summary>
        private bool HasHighLoyalty()
        {
            if (GeneratedProfile != null)
                return GeneratedProfile.positiveTrait == PositiveTrait.Loyal ||
                       GeneratedProfile.GetStat(SurvivorStatIndex.Social) >= 60;
            return data != null && data.loyalty >= 40;
        }
    }
}
