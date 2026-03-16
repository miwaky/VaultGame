using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Moves a survivor to their assigned idle point after spawning.
    /// Requires a NavMeshAgent on the same GameObject.
    /// Once the destination is reached the agent stops and the component disables itself.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class SurvivorIdleMovement : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Vector3      targetPosition;
        private bool         hasTarget;
        private bool         destinationSet;

        private const float ArrivalThreshold = 0.5f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        /// <summary>Sets the idle destination. Call this before the component's first Update.</summary>
        public void SetTarget(Vector3 position)
        {
            targetPosition = position;
            hasTarget      = true;
            destinationSet = false;
        }

        private void Update()
        {
            if (!hasTarget || agent == null || !agent.isOnNavMesh) return;

            // Issue destination once on the first valid frame — regardless of current path state
            if (!destinationSet)
            {
                agent.isStopped = false;
                agent.SetDestination(targetPosition);
                destinationSet = true;
                return;
            }

            // Check arrival
            if (!agent.pathPending && agent.remainingDistance <= ArrivalThreshold)
            {
                agent.isStopped = true;
                enabled = false;
            }
        }
    }
}
