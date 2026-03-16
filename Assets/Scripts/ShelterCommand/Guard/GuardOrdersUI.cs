using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// UI controller for the Guard Orders panel shown inside the telephone.
    ///
    /// Structure expected in the scene (all wired via Inspector):
    ///
    ///   GuardOrdersPanel
    ///     ├─ ButtonMoveToRoom
    ///     ├─ ButtonPatrolRoom
    ///     ├─ ButtonCancelOrder
    ///     ├─ ButtonClose
    ///     ├─ RoomListPanel
    ///     │   └─ (RoomButton prefab instances — one per room entry)
    ///     └─ StatusLabel (TMP_Text)
    ///
    /// This panel is activated by TelephonePanelUI.OnCallGuardClicked().
    /// Wire GuardController and ShelterRoom[] references in the Inspector.
    /// </summary>
    public class GuardOrdersUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Guard")]
        [SerializeField] private GuardController guardController;

        [Header("Main Buttons")]
        [SerializeField] private Button buttonMoveToRoom;
        [SerializeField] private Button buttonPatrolRoom;
        [SerializeField] private Button buttonCancelOrder;
        [SerializeField] private Button buttonClose;

        [Header("Room List")]
        [Tooltip("Panel that hosts the dynamically generated room buttons.")]
        [SerializeField] private GameObject roomListPanel;
        [Tooltip("Prefab for a single room selection button. Must contain a Button and a TMP_Text child.")]
        [SerializeField] private GameObject roomButtonPrefab;
        [Tooltip("All shelter rooms available as destinations for the guard.")]
        [SerializeField] private ShelterRoom[] availableRooms;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusLabel;

        // ── Runtime ────────────────────────────────────────────────────────────────

        private GuardOrderType pendingOrderType = GuardOrderType.None;
        private readonly List<GameObject> roomButtonInstances = new List<GameObject>();

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            buttonMoveToRoom?.onClick.AddListener(OnMoveToRoomClicked);
            buttonPatrolRoom?.onClick.AddListener(OnPatrolRoomClicked);
            buttonCancelOrder?.onClick.AddListener(OnCancelOrderClicked);
            buttonClose?.onClick.AddListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            RefreshStatus();
            HideRoomList();

            if (guardController != null)
                guardController.OnStateChanged += OnGuardStateChanged;
        }

        private void OnDisable()
        {
            if (guardController != null)
                guardController.OnStateChanged -= OnGuardStateChanged;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Injects the guard reference at runtime (called by TelephonePanelUI if needed).
        /// </summary>
        public void Initialise(GuardController guard)
        {
            guardController = guard;
        }

        // ── Button handlers ────────────────────────────────────────────────────────

        private void OnMoveToRoomClicked()
        {
            pendingOrderType = GuardOrderType.MoveToRoom;
            ShowRoomList();
            SetStatus("Choisir une salle de destination…");
        }

        private void OnPatrolRoomClicked()
        {
            pendingOrderType = GuardOrderType.PatrolRoom;
            ShowRoomList();
            SetStatus("Choisir une salle à patrouiller…");
        }

        private void OnCancelOrderClicked()
        {
            guardController?.OrderCancel();
            HideRoomList();
            SetStatus("Ordre annulé.");
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        // ── Room list ──────────────────────────────────────────────────────────────

        private void ShowRoomList()
        {
            if (roomListPanel == null || roomButtonPrefab == null) return;

            ClearRoomButtons();
            roomListPanel.SetActive(true);

            foreach (ShelterRoom room in availableRooms)
            {
                if (room == null) continue;

                GameObject buttonGo = Instantiate(roomButtonPrefab, roomListPanel.transform);
                roomButtonInstances.Add(buttonGo);

                // Set label
                TextMeshProUGUI label = buttonGo.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = room.RoomName;

                // Wire click
                Button btn = buttonGo.GetComponent<Button>();
                ShelterRoom capturedRoom = room;
                if (btn != null)
                    btn.onClick.AddListener(() => OnRoomSelected(capturedRoom));
            }
        }

        private void HideRoomList()
        {
            ClearRoomButtons();
            if (roomListPanel != null)
                roomListPanel.SetActive(false);
        }

        private void ClearRoomButtons()
        {
            foreach (GameObject go in roomButtonInstances)
                if (go != null) Destroy(go);
            roomButtonInstances.Clear();
        }

        private void OnRoomSelected(ShelterRoom room)
        {
            if (guardController == null)
            {
                Debug.LogWarning("[GuardOrdersUI] GuardController not assigned.");
                return;
            }

            switch (pendingOrderType)
            {
                case GuardOrderType.MoveToRoom:
                    guardController.OrderMoveToRoom(room);
                    SetStatus($"Steve → {room.RoomName}");
                    break;

                case GuardOrderType.PatrolRoom:
                    guardController.OrderPatrolRoom(room);
                    SetStatus($"Steve surveille : {room.RoomName}");
                    break;
            }

            pendingOrderType = GuardOrderType.None;
            HideRoomList();
        }

        // ── Guard state callback ───────────────────────────────────────────────────

        private void OnGuardStateChanged(GuardState newState)
        {
            RefreshStatus();
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private void RefreshStatus()
        {
            if (guardController == null)
            {
                SetStatus("Garde non assigné.");
                return;
            }

            string stateText = guardController.CurrentState switch
            {
                GuardState.Idle        => "En attente",
                GuardState.Moving      => "En déplacement…",
                GuardState.Guarding    => "Surveillance en cours",
                GuardState.Intervening => "Intervention en cours",
                _                      => "Inconnu"
            };

            SetStatus($"Steve — {stateText}");
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
