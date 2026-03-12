using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace ShelterCommand
{
    /// <summary>
    /// Bouton cliquable sur la carte d'exploration.
    /// Notifie ExplorationPanelUI quand cliqué.
    ///
    /// Pas de labels enfants — le nom et la durée de la zone sont affichés
    /// dans le panel de droite via ExplorationPanelUI.RefreshSummary().
    ///
    /// Attacher directement sur l'Image de la zone.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class MapZoneButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ExplorationZone zone;

        // ── Couleurs ──────────────────────────────────────────────────────────────
        private static readonly Color ColorSelected = new Color(0.95f, 0.85f, 0.1f, 1f);
        private static readonly Color ColorHover    = new Color(1f,    1f,    1f,   0.25f);

        private Image image;
        private Color colorDefault;
        private bool  isSelected;

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<MapZoneButton> OnZoneSelected;

        // ── Properties ────────────────────────────────────────────────────────────
        public ExplorationZone Zone => zone;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake() => EnsureImage();

        // ── Public API ────────────────────────────────────────────────────────────
        public void SetSelected(bool selected)
        {
            EnsureImage();
            isSelected  = selected;
            image.color = isSelected ? ColorSelected : colorDefault;
        }

        // ── Pointer handlers ──────────────────────────────────────────────────────
        public void OnPointerClick(PointerEventData eventData) => OnZoneSelected?.Invoke(this);

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureImage();
            if (!isSelected)
                image.color = Color.Lerp(colorDefault, Color.white, 0.35f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EnsureImage();
            if (!isSelected)
                image.color = colorDefault;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialise image et colorDefault de manière idempotente.
        /// Sûr à appeler avant Awake (ex. SetSelected depuis OnEnable d'un parent).
        /// </summary>
        private void EnsureImage()
        {
            if (image != null) return;
            image        = GetComponent<Image>();
            colorDefault = zone != null ? zone.zoneColor : image.color;
            image.color  = colorDefault;
        }
    }
}
