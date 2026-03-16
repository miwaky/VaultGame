using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Reads the NavMeshAgent speed and the SurvivorBehavior work state each frame
    /// and drives the Animator accordingly.
    ///
    /// Required Animator parameters:
    ///   - Speed  (Float)  — mirrors NavMeshAgent.velocity.magnitude
    ///   - IsWorking (Bool) — true when the survivor is at a work location
    ///
    /// Expected Animator Controller states:
    ///   Idle      ← base state (Speed == 0, IsWorking == false)
    ///   Movement  ← triggered by Speed > MovingThreshold
    ///   Work      ← triggered by IsWorking == true
    /// </summary>
    [RequireComponent(typeof(SurvivorBehavior))]
    public class SurvivorAnimatorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Tooltip("Animator on the visual model child. Auto-resolved if left empty.")]
        [SerializeField] private Animator animator;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const string SpeedParam     = "Speed";
        private const string IsWorkingParam = "IsWorking";
        private const float  MovingThreshold = 0.05f;

        // ── State ─────────────────────────────────────────────────────────────────

        private NavMeshAgent    agent;
        private SurvivorBehavior behavior;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            behavior = GetComponent<SurvivorBehavior>();
            agent    = GetComponent<NavMeshAgent>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null) return;

            float speed = agent != null ? agent.velocity.magnitude : 0f;
            animator.SetFloat(SpeedParam, speed);
            animator.SetBool(IsWorkingParam, behavior != null && behavior.IsWorking);
        }

        /// <summary>
        /// Assigns a new Animator when the visual model is swapped at runtime.
        /// Called by SurvivorInitializer after instantiating the visual prefab.
        /// </summary>
        public void SetAnimator(Animator newAnimator)
        {
            animator = newAnimator;
        }
    }
}
