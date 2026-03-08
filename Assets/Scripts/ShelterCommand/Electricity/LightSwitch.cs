using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Interrupteur direct : contrôle une liste de Light en les allumant/éteignant.
    /// Aucune dépendance à ElectricitySystem.
    ///
    /// Setup :
    ///   1. Pose ce composant sur un GameObject avec BoxCollider.
    ///   2. Assigne les lumières à contrôler dans la liste Controlled Lights.
    ///   3. Le joueur interagit avec E pour basculer l'état.
    /// </summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [Header("Lumières contrôlées")]
        [SerializeField] private List<Light> controlledLights = new List<Light>();

        [Header("État initial")]
        [SerializeField] private bool startOn = false;

        private bool isOn;

        public bool         IsInteractable => true;
        public string       PromptLabel    => isOn ? "Éteindre les lumières" : "Allumer les lumières";

        /// <summary>État courant de l'interrupteur.</summary>
        public bool IsOn => isOn;

        private void Start()
        {
            Debug.Log($"[LightSwitch] '{gameObject.name}' démarré | startOn = {startOn} | {controlledLights.Count} lumière(s) assignée(s) | layer = '{LayerMask.LayerToName(gameObject.layer)}'");

            if (controlledLights.Count == 0)
                Debug.LogWarning($"[LightSwitch] '{gameObject.name}' — la liste Controlled Lights est VIDE. Assigne au moins une Light dans l'Inspector.");

            bool hasCollider = GetComponent<Collider>() != null;
            if (!hasCollider)
                Debug.LogError($"[LightSwitch] '{gameObject.name}' — aucun Collider trouvé ! Le raycast ne peut pas le toucher.");

            SetLights(startOn);
        }

        /// <summary>Appelé par OfficeInteractionSystem quand le joueur appuie sur E.</summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            Debug.Log($"[LightSwitch] Interact() reçu → bascule de {isOn} à {!isOn}");
            SetLights(!isOn);
        }

        /// <summary>Allume ou éteint toutes les lumières de la liste.</summary>
        public void SetLights(bool on)
        {
            isOn = on;
            int toggled = 0;
            foreach (Light l in controlledLights)
            {
                if (l != null)
                {
                    l.enabled = on;
                    toggled++;
                }
                else
                {
                    Debug.LogWarning($"[LightSwitch] '{gameObject.name}' — une entrée de Controlled Lights est NULL (référence perdue).");
                }
            }
            Debug.Log($"[LightSwitch] '{gameObject.name}' → {toggled} lumière(s) {(on ? "ALLUMÉE(S)" : "ÉTEINTE(S)")}");
        }

        /// <summary>Ajoute une lumière à la liste contrôlée (utile depuis d'autres scripts).</summary>
        public void RegisterLight(Light light)
        {
            if (light != null && !controlledLights.Contains(light))
                controlledLights.Add(light);
        }
    }
}
