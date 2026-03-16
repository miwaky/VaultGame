using System;
using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Controls Steve the guard — a special NPC that receives direct orders from the player
    /// via the telephone. Unlike regular survivors, Steve does not follow the schedule system.
    ///
    /// Attach this component to the guard GameObject alongside a NavMeshAgent.
    /// Wire a reference to this controller in GuardOrdersUI.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardController : MonoBehaviour
    {
        // ── Constants ──────────────────────────────────────────────────────────────

        private const string GuardName            = "Steve";
        private const float  DestinationThreshold = 1.2f;
        private const float  ArrivalCheckInterval = 0.25f;

        // Paramètres du SurvivorAnimator.controller
        private const string AnimSpeed     = "Speed";
        private const string AnimIsWorking = "IsWorking";

        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private string guardDisplayName = GuardName;

        [Header("Skin & Animation")]
        [Tooltip("Animator sur le skin enfant (SteveSkin). Laissez vide pour auto-détection.")]
        [SerializeField] private Animator skinAnimator;

        [Header("Patrol")]
        [Tooltip("Secondes entre chaque changement de waypoint en mode Patrol.")]
        [SerializeField] private float patrolWaypointInterval = 5f;

        // ── Public read ────────────────────────────────────────────────────────────

        public GuardState     CurrentState { get; private set; } = GuardState.Idle;
        public GuardOrderType CurrentOrder { get; private set; } = GuardOrderType.None;
        public string         DisplayName  => guardDisplayName;

        /// <summary>Fired whenever the guard's state changes.</summary>
        public event Action<GuardState> OnStateChanged;

        // ── Private ────────────────────────────────────────────────────────────────

        private NavMeshAgent agent;
        private ShelterRoom  targetRoom;
        private float        arrivalTimer;
        private float        patrolTimer;

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (skinAnimator == null)
                skinAnimator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            UpdateAnimator();

            switch (CurrentState)
            {
                case GuardState.Moving:
                    CheckArrival();
                    break;

                case GuardState.Guarding:
                    if (CurrentOrder == GuardOrderType.PatrolRoom)
                        TickPatrol();
                    break;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Issues a MoveToRoom order. Cancels any current task and sends the guard to
        /// the specified room via NavMesh. The last order always takes priority.
        /// </summary>
        public void OrderMoveToRoom(ShelterRoom room)
        {
            if (room == null)
            {
                Debug.LogWarning("[GuardController] OrderMoveToRoom called with null room.");
                return;
            }

            CancelCurrentTask();
            targetRoom   = room;
            CurrentOrder = GuardOrderType.MoveToRoom;
            SetDestination(room.GetRandomSpawnPoint());
            SetState(GuardState.Moving);

            Debug.Log($"[GuardController] {DisplayName} → moving to '{room.RoomName}'.");
        }

        /// <summary>
        /// Issues a PatrolRoom order. The guard moves to the room first, then begins
        /// patrolling by visiting random spawn points inside it.
        /// </summary>
        public void OrderPatrolRoom(ShelterRoom room)
        {
            if (room == null)
            {
                Debug.LogWarning("[GuardController] OrderPatrolRoom called with null room.");
                return;
            }

            CancelCurrentTask();
            targetRoom   = room;
            CurrentOrder = GuardOrderType.PatrolRoom;
            SetDestination(room.GetRandomSpawnPoint());
            SetState(GuardState.Moving);
            patrolTimer = 0f;

            Debug.Log($"[GuardController] {DisplayName} → patrolling '{room.RoomName}'.");
        }

        /// <summary>
        /// Cancels the current task and returns the guard to Idle.
        /// </summary>
        public void OrderCancel()
        {
            CancelCurrentTask();
            Debug.Log($"[GuardController] {DisplayName} → order cancelled, returning to Idle.");
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private void CancelCurrentTask()
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;

            targetRoom   = null;
            CurrentOrder = GuardOrderType.None;
            patrolTimer  = 0f;
            SetState(GuardState.Idle);
        }

        private void SetDestination(Vector3 destination)
        {
            if (agent == null || !agent.isActiveAndEnabled) return;

            agent.isStopped = false;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
            }
            else if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agent.SetDestination(hit.position);
            }
            else
            {
                transform.position = destination;
                Debug.LogWarning($"[GuardController] {DisplayName} : NavMesh sample failed — teleporting.");
            }
        }

        private void CheckArrival()
        {
            arrivalTimer += Time.deltaTime;
            if (arrivalTimer < ArrivalCheckInterval) return;
            arrivalTimer = 0f;

            if (agent == null || !agent.isOnNavMesh) return;
            if (agent.pathPending) return;

            if (agent.remainingDistance <= DestinationThreshold)
            {
                agent.isStopped = true;
                OnArrivedAtRoom();
            }
        }

        private void OnArrivedAtRoom()
        {
            SetState(GuardState.Guarding);
            patrolTimer = 0f;
            Debug.Log($"[GuardController] {DisplayName} → arrived, now Guarding '{targetRoom?.RoomName}'.");
        }

        private void TickPatrol()
        {
            if (targetRoom == null) return;

            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaypointInterval)
            {
                patrolTimer = 0f;
                SetDestination(targetRoom.GetRandomSpawnPoint());
                agent.isStopped = false;
            }
        }

        private void SetState(GuardState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Drives the SurvivorAnimator parameters based on agent velocity.
        /// Speed > 0.05  → Movement animation
        /// Speed = 0     → Idle animation
        /// IsWorking     → toujours false pour le garde
        /// </summary>
        private void UpdateAnimator()
        {
            if (skinAnimator == null || agent == null) return;

            float speed = agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
            skinAnimator.SetFloat(AnimSpeed, speed);
            skinAnimator.SetBool(AnimIsWorking, false);
        }
    }
}
