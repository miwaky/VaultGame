using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Trigger volume that marks an area as a work location.
    /// When a survivor enters it, SurvivorBehavior.IsWorking is set to true.
    /// When the survivor leaves, it is set to false.
    ///
    /// Setup: add this component to a GameObject with a Collider set to Is Trigger.
    /// The GameObject should be placed inside a work room (farm, water room, storage, infirmary…).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SurvivorWorkZone : MonoBehaviour
    {
        private void Awake()
        {
            // Ensure the collider is a trigger at runtime regardless of Inspector setting
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            SurvivorBehavior survivor = other.GetComponentInParent<SurvivorBehavior>();
            if (survivor != null && survivor.IsAlive)
                survivor.SetWorking(true);
        }

        private void OnTriggerExit(Collider other)
        {
            SurvivorBehavior survivor = other.GetComponentInParent<SurvivorBehavior>();
            if (survivor != null)
                survivor.SetWorking(false);
        }
    }
}
