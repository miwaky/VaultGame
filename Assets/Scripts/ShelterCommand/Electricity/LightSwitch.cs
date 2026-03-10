using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Interrupteur : contrôle une liste de PoweredLight en appelant SetLight().
    /// Chaque PoweredLight gère elle-même son enfant LightSource via SetActive.
    ///
    /// Setup :
    ///   1. Pose ce composant sur un GO avec BoxCollider (le boîtier mural).
    ///   2. Dans Controlled Lights, assigne les composants PoweredLight à contrôler.
    ///      (glisse le GO racine de chaque lampe — Unity prendra le composant PoweredLight)
    ///   3. startOn = état initial de toutes les lampes liées.
    /// </summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [Header("Lampes contrôlées")]
        [SerializeField] private List<PoweredLight> controlledLights = new List<PoweredLight>();

        [Header("État initial")]
        [SerializeField] private bool startOn = false;

        private bool isOn;

        public bool   IsInteractable => true;
        public string PromptLabel    => isOn ? "Éteindre les lumières" : "Allumer les lumières";

        public bool IsOn => isOn;

        private void Start()
        {
            if (GetComponent<Collider>() == null)
                Debug.LogError($"[LightSwitch] '{gameObject.name}' — aucun Collider, le raycast ne peut pas le toucher.");

            if (controlledLights.Count == 0)
                Debug.LogWarning($"[LightSwitch] '{gameObject.name}' — aucune PoweredLight assignée dans Controlled Lights.");

            SetLights(startOn);
        }

        /// <summary>Appelé par OfficeInteractionSystem quand le joueur appuie sur E.</summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            SetLights(!isOn);
        }

        /// <summary>Allume ou éteint toutes les PoweredLight de la liste.</summary>
        public void SetLights(bool on)
        {
            isOn = on;
            foreach (PoweredLight light in controlledLights)
            {
                if (light != null)
                    light.SetLight(on);
                else
                    Debug.LogWarning($"[LightSwitch] '{gameObject.name}' — entrée NULL dans Controlled Lights.");
            }
        }

        /// <summary>Enregistre une PoweredLight depuis un autre script.</summary>
        public void RegisterLight(PoweredLight light)
        {
            if (light != null && !controlledLights.Contains(light))
                controlledLights.Add(light);
        }
    }
}
