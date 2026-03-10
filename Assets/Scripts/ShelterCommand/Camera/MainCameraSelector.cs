using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Manages which Camera is tagged MainCamera at runtime.
    /// Attach to the GameSystems GameObject alongside ShelterGameManager.
    /// Cameras registered here get their tag toggled — only the active one
    /// carries the "MainCamera" tag; all others use "Untagged".
    /// </summary>
    public class MainCameraSelector : MonoBehaviour
    {
        [System.Serializable]
        public class NamedCamera
        {
            public string label;
            public Camera camera;
        }

        [Header("Available Cameras")]
        [SerializeField] private List<NamedCamera> cameras = new List<NamedCamera>();

        [Header("Startup")]
        [SerializeField] private int defaultCameraIndex = 0;

        private int activeIndex = -1;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Currently active camera, or null if none.</summary>
        public Camera ActiveCamera => IsValidIndex(activeIndex) ? cameras[activeIndex].camera : null;

        /// <summary>
        /// Activates the camera at the given index and tags it as MainCamera.
        /// All other registered cameras lose the tag.
        /// </summary>
        public void SelectCamera(int index)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"[MainCameraSelector] Index {index} hors limites (count={cameras.Count}).");
                return;
            }

            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i].camera == null) continue;
                cameras[i].camera.tag = (i == index) ? "MainCamera" : "Untagged";
            }

            activeIndex = index;
            Debug.Log($"[MainCameraSelector] MainCamera → '{cameras[index].label}'");
        }

        /// <summary>Activates the camera whose label matches (case-insensitive).</summary>
        public void SelectCamera(string label)
        {
            int idx = cameras.FindIndex(c =>
                string.Equals(c.label, label, System.StringComparison.OrdinalIgnoreCase));

            if (idx < 0)
            {
                Debug.LogWarning($"[MainCameraSelector] Caméra '{label}' introuvable.");
                return;
            }

            SelectCamera(idx);
        }

        /// <summary>Activates the camera by direct Camera reference.</summary>
        public void SelectCamera(Camera cam)
        {
            int idx = cameras.FindIndex(c => c.camera == cam);
            if (idx < 0)
            {
                Debug.LogWarning($"[MainCameraSelector] Caméra '{cam.name}' non enregistrée.");
                return;
            }

            SelectCamera(idx);
        }

        /// <summary>Cycles to the next registered camera.</summary>
        public void SelectNextCamera()
        {
            if (cameras.Count == 0) return;
            SelectCamera((activeIndex + 1) % cameras.Count);
        }

        /// <summary>Cycles to the previous registered camera.</summary>
        public void SelectPreviousCamera()
        {
            if (cameras.Count == 0) return;
            int prev = (activeIndex - 1 + cameras.Count) % cameras.Count;
            SelectCamera(prev);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            if (cameras.Count == 0)
            {
                Debug.LogWarning("[MainCameraSelector] Aucune caméra enregistrée.");
                return;
            }

            SelectCamera(Mathf.Clamp(defaultCameraIndex, 0, cameras.Count - 1));
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private bool IsValidIndex(int index) => index >= 0 && index < cameras.Count;
    }
}
