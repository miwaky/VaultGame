using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Registers world-space spawn positions per room into SurvivorBehavior at scene start,
    /// and moves each SurvivorBehavior to their starting room slot.
    /// Attach to the GameSystems GameObject and assign the slot arrays in the Inspector.
    /// </summary>
    public class SurvivorRoomRegistry : MonoBehaviour
    {
        [Serializable]
        public struct RoomSlots
        {
            public ShelterRoomType room;
            public Transform[] spawnPoints;
        }

        [Header("Room Spawn Slots")]
        [SerializeField] private RoomSlots[] roomSlots;

        private void Awake()
        {
            foreach (RoomSlots rs in roomSlots)
            {
                if (rs.spawnPoints == null || rs.spawnPoints.Length == 0) continue;
                Vector3[] positions = Array.ConvertAll(rs.spawnPoints, t => t.position);
                SurvivorBehavior.RegisterRoomSpawns(rs.room, positions);
            }
        }

        private void Start()
        {
            // Move all survivors to their starting rooms after spawn points are registered
            SurvivorBehavior[] allSurvivors = FindObjectsByType<SurvivorBehavior>(FindObjectsSortMode.None);
            foreach (SurvivorBehavior sb in allSurvivors)
                sb.SetRoom(sb.CurrentRoom);
        }
    }
}
