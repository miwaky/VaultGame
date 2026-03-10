using UnityEngine;
using UnityEditor;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Builds the room-parameters panel: the future UI/room where the player configures
    /// shelter rooms (capacity, equipment, upgrades, etc.).
    /// Called by ShelterCommandSceneBuilder.BuildScene().
    /// — Placeholder for upcoming implementation —
    /// </summary>
    internal static class ShelterRoomParametersBuilder
    {
        // ── Entry point ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the RoomParameters GameObject under the given parent.
        /// Currently a visible placeholder — replace the interior with real UI/logic later.
        /// </summary>
        internal static GameObject Build(GameObject parent)
        {
            GameObject roomParamsRoot = new GameObject("RoomParameters");
            roomParamsRoot.transform.SetParent(parent.transform);
            roomParamsRoot.transform.localPosition = Vector3.zero;

            Debug.Log("[ShelterRoomParametersBuilder] Placeholder créé — à implémenter.");
            return roomParamsRoot;
        }
    }
}
