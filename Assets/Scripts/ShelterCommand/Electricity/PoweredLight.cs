using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Lampe contrôlée par LightSwitch.
    /// Le GO racine reste toujours actif.
    /// Seul l'enfant LightSource est activé/désactivé.
    ///
    /// Setup :
    ///   1. GO racine PoweredLight : actif, porte ce script + Collider (si interactable directement).
    ///   2. GO enfant LightSource : contient la Light. Laisse-le inactif dans l'éditeur ou actif, peu importe.
    ///   3. Assigne cet enfant dans le champ Light Source, ou nomme-le exactement "LightSource".
    /// </summary>
    public class PoweredLight : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private GameObject lightSource;

        [Header("État initial")]
        [SerializeField] private bool startOn = false;

        /// <summary>Vrai si le LightSource est actif.</summary>
        public bool IsOn => lightSource != null && lightSource.activeSelf;

        private void Awake()
        {
            if (lightSource == null)
            {
                Transform found = transform.Find("LightSource");
                if (found != null)
                    lightSource = found.gameObject;
                else
                    Debug.LogError($"[PoweredLight] '{gameObject.name}' — enfant 'LightSource' introuvable.");
            }
        }

        private void Start()
        {
            // Force l'état initial sans dépendre de l'état éditeur du GO enfant
            SetLight(startOn);
        }

        /// <summary>Active ou désactive le LightSource.</summary>
        public void SetLight(bool on)
        {
            if (lightSource != null)
                lightSource.SetActive(on);
        }

        /// <summary>Inverse l'état courant du LightSource.</summary>
        public void Toggle() => SetLight(!IsOn);
    }
}
