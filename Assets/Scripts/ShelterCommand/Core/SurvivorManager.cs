using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Manages all survivors. Collects SurvivorBehavior components already present in the scene.
    /// Does NOT spawn new survivors — the scene builder places them.
    /// </summary>
    public class SurvivorManager : MonoBehaviour
    {
        [Header("Survivor Data Assets (assigned by index to scene survivors)")]
        [SerializeField] private List<SurvivorData> survivorDataList = new List<SurvivorData>();

        public event Action<SurvivorBehavior> OnSurvivorSelected;
        public event Action<SurvivorBehavior> OnSurvivorDied;
        public event Action OnPopulationChanged;

        private readonly List<SurvivorBehavior> survivors = new List<SurvivorBehavior>();
        private SurvivorBehavior selectedSurvivor;

        public IReadOnlyList<SurvivorBehavior> Survivors => survivors;
        public SurvivorBehavior SelectedSurvivor => selectedSurvivor;
        public int AliveSurvivorCount => GetAliveSurvivors().Count;

        private void Start()
        {
            CollectSurvivors();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Advances all survivors by one day.</summary>
        public void TickDay(ShelterResources resources)
        {
            foreach (SurvivorBehavior survivor in survivors)
            {
                if (survivor != null && survivor.IsAlive)
                {
                    survivor.TickDay(resources);
                }
            }
        }

        /// <summary>Selects a survivor to receive orders.</summary>
        public void SelectSurvivor(SurvivorBehavior survivor)
        {
            selectedSurvivor = survivor;
            OnSurvivorSelected?.Invoke(survivor);
        }

        /// <summary>Deselects current survivor.</summary>
        public void DeselectSurvivor()
        {
            selectedSurvivor = null;
            OnSurvivorSelected?.Invoke(null);
        }

        /// <summary>Issues an order to the currently selected survivor.</summary>
        public bool IssueOrderToSelected(OrderType order, ShelterResources resources)
        {
            if (selectedSurvivor == null) return false;
            bool accepted = selectedSurvivor.IssueOrder(order, resources);
            if (accepted) DeselectSurvivor();
            return accepted;
        }

        /// <summary>Returns all alive survivors in a specific room.</summary>
        public List<SurvivorBehavior> GetSurvivorsInRoom(ShelterRoomType room)
        {
            List<SurvivorBehavior> result = new List<SurvivorBehavior>();
            foreach (SurvivorBehavior s in survivors)
            {
                if (s != null && s.IsAlive && s.CurrentRoom == room)
                {
                    result.Add(s);
                }
            }
            return result;
        }

        /// <summary>
        /// Registers a new survivor directly (from a mission rescue or scripted event).
        /// The caller is responsible for creating and positioning the GameObject beforehand.
        /// </summary>
        public void AddSurvivor(SurvivorBehavior sb)
        {
            if (sb == null) return;
            survivors.Add(sb);
            RegisterSurvivorEvents(sb);
            OnPopulationChanged?.Invoke();
        }

        public List<SurvivorBehavior> GetAliveSurvivors()
        {
            List<SurvivorBehavior> alive = new List<SurvivorBehavior>();
            foreach (SurvivorBehavior s in survivors)
            {
                if (s != null && s.IsAlive) alive.Add(s);
            }
            return alive;
        }

        // ── Private ─────────────────────────────────────────────────────────────

        private void CollectSurvivors()
        {
            GameObject survivorsRoot = GameObject.Find("Survivors");
            SurvivorBehavior[] found;

            if (survivorsRoot != null)
                found = survivorsRoot.GetComponentsInChildren<SurvivorBehavior>(includeInactive: true);
            else
                found = FindObjectsByType<SurvivorBehavior>(FindObjectsSortMode.None);

            survivors.Clear();
            for (int i = 0; i < found.Length; i++)
            {
                SurvivorBehavior sb = found[i];
                if (sb.Data == null && i < survivorDataList.Count && survivorDataList[i] != null)
                    sb.SetData(survivorDataList[i]);

                survivors.Add(sb);
                RegisterSurvivorEvents(sb);
            }

            Debug.Log($"[SurvivorManager] Collected {survivors.Count} survivors.");
        }

        private void RegisterSurvivorEvents(SurvivorBehavior sb)
        {
            sb.OnSurvivorDied += HandleSurvivorDeath;
        }

        private void HandleSurvivorDeath(SurvivorBehavior survivor)
        {
            if (selectedSurvivor == survivor) DeselectSurvivor();
            OnSurvivorDied?.Invoke(survivor);
            OnPopulationChanged?.Invoke();
        }
    }
}
