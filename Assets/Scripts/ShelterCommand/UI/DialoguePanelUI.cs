using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Panneau de dialogue partagé — radio ET PNJ.
    ///
    /// Deux zones distinctes :
    ///   • NpcZone   (haut)  — badge speaker + boîte texte, pagination via Entrée/Espace.
    ///   • ChoicesZone (bas) — badge "Vous" + liste verticale de boutons transparents.
    ///
    /// Pagination : TextOverflowModes.Page sur dialogueTextLabel.
    ///   Pages 1..n-1 → indicateur "▼  Entrée" visible, ChoicesZone cachée.
    ///   Page n (dernière) → indicateur caché, ChoicesZone affichée (choix ou OK).
    ///
    /// Fermeture : E, Échap, Entrée (dernière page), ou clic OK.
    /// </summary>
    public class DialoguePanelUI : MonoBehaviour
    {
        // ── Inspector — NPC Zone ──────────────────────────────────────────────────
        [Header("NPC Zone (haut)")]
        [SerializeField] private TextMeshProUGUI speakerNameLabel;
        [SerializeField] private TextMeshProUGUI dialogueTextLabel;
        [Tooltip("Label '▼  Entrée' affiché quand il reste des pages à lire.")]
        [SerializeField] private TextMeshProUGUI moreTextIndicator;
        [Tooltip("Root de la zone NPC (contient badge + boîte texte).")]
        [SerializeField] private GameObject      npcZone;

        // ── Inspector — Choices Zone ──────────────────────────────────────────────
        [Header("Choices Zone (bas)")]
        [Tooltip("Root de la zone choix — affichée uniquement sur la dernière page.")]
        [SerializeField] private GameObject      choicesZone;
        [Tooltip("Parent des boutons de choix (VerticalLayoutGroup recommandé).")]
        [SerializeField] private Transform       choicesContainer;
        [Tooltip("Prefab bouton : Button + TextMeshProUGUI, fond transparent.")]
        [SerializeField] private GameObject      choiceButtonPrefab;
        [Tooltip("Bouton OK transparent — visible quand aucun choix, texte uniquement.")]
        [SerializeField] private Button          okButton;

        // ── Inspector — Timer ─────────────────────────────────────────────────────
        [Header("Timer")]
        [SerializeField] private GameObject      timerRoot;
        [SerializeField] private Image           timerFill;
        [SerializeField] private TextMeshProUGUI timerLabel;

        // ── Pagination ────────────────────────────────────────────────────────────
        private int  currentPage      = 1;
        private bool isLastPage       = true;
        private bool awaitingPageTurn = false;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private DialogueManager           manager;
        private ExplorationDialogue       currentNode;
        private DialogueContext           currentCtx;
        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Affiche le panneau pour un noeud de dialogue.</summary>
        public void Show(ExplorationDialogue node, DialogueContext ctx, DialogueManager dialogueManager)
        {
            manager     = dialogueManager;
            currentNode = node;
            currentCtx  = ctx;
            currentPage = 1;

            gameObject.SetActive(true);

            if (speakerNameLabel != null)
                speakerNameLabel.text = ctx != null ? ctx.Apply(node.speakerName) : node.speakerName;

            HideTimer();
            HideChoicesZone();

            string text = ctx != null ? ctx.Apply(node.dialogueText) : node.dialogueText;
            SetupPagination(text);
        }

        /// <summary>Ferme et réinitialise le panneau.</summary>
        public void Hide()
        {
            ClearChoices();
            HideTimer();
            awaitingPageTurn = false;
            gameObject.SetActive(false);
        }

        /// <summary>Met à jour la barre de décompte. Appelé chaque frame par DialogueManager.</summary>
        public void UpdateTimer(float remaining, float total)
        {
            if (timerRoot != null) timerRoot.SetActive(true);
            if (timerFill != null) timerFill.fillAmount = total > 0f ? remaining / total : 0f;
            if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(remaining) + "s";
        }

        /// <summary>Cache la barre de décompte.</summary>
        public void HideTimer()
        {
            if (timerRoot != null) timerRoot.SetActive(false);
        }

        // ── Input ─────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            if (Keyboard.current == null) return;

            bool confirmPressed = Keyboard.current.enterKey.wasPressedThisFrame ||
                                  Keyboard.current.spaceKey.wasPressedThisFrame;
            bool closePressed   = Keyboard.current.eKey.wasPressedThisFrame     ||
                                  Keyboard.current.escapeKey.wasPressedThisFrame;

            if (closePressed)
            {
                manager?.EndDialogue();
                return;
            }

            if (awaitingPageTurn && confirmPressed)
                AdvancePage();
        }

        // ── Pagination ────────────────────────────────────────────────────────────

        private void SetupPagination(string fullText)
        {
            if (dialogueTextLabel == null)
            {
                OnReachedLastPage();
                return;
            }

            dialogueTextLabel.overflowMode  = TextOverflowModes.Page;
            dialogueTextLabel.pageToDisplay = currentPage;
            dialogueTextLabel.text          = fullText;
            dialogueTextLabel.ForceMeshUpdate();

            int total  = Mathf.Max(1, dialogueTextLabel.textInfo.pageCount);
            isLastPage = currentPage >= total;
            ShowMoreIndicator(!isLastPage);

            if (isLastPage)
                OnReachedLastPage();
            else
                awaitingPageTurn = true;
        }

        private void AdvancePage()
        {
            awaitingPageTurn = false;
            currentPage++;

            if (dialogueTextLabel != null)
            {
                dialogueTextLabel.pageToDisplay = currentPage;
                dialogueTextLabel.ForceMeshUpdate();

                int total  = Mathf.Max(1, dialogueTextLabel.textInfo.pageCount);
                isLastPage = currentPage >= total;
                ShowMoreIndicator(!isLastPage);
            }
            else
            {
                isLastPage = true;
            }

            if (isLastPage)
                OnReachedLastPage();
            else
                awaitingPageTurn = true;
        }

        // ── Choices ───────────────────────────────────────────────────────────────

        private void OnReachedLastPage()
        {
            if (choicesZone != null) choicesZone.SetActive(true);
            BuildChoices(currentNode, currentCtx);
        }

        private void HideChoicesZone()
        {
            if (choicesZone != null) choicesZone.SetActive(false);
            if (okButton != null)    okButton.gameObject.SetActive(false);
            ClearChoices();
        }

        private void BuildChoices(ExplorationDialogue node, DialogueContext ctx)
        {
            if (node == null) return;

            ClearChoices();
            bool hasChoices = node.choices != null && node.choices.Length > 0;

            if (okButton != null)
            {
                okButton.gameObject.SetActive(!hasChoices);
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(() => manager?.EndDialogue());
            }

            if (!hasChoices || choicesContainer == null || choiceButtonPrefab == null) return;

            if (okButton != null) okButton.gameObject.SetActive(false);

            // Gather survivor profiles from the current mission for condition evaluation
            IReadOnlyList<SurvivorGeneratedProfile> profiles = GatherMissionProfiles();

            foreach (DialogueChoice choice in node.choices)
            {
                if (choice == null) continue;

                // Evaluate conditions
                bool conditionMet = true;
                string failHint   = string.Empty;
                ConditionFailBehaviour failBehaviour = ConditionFailBehaviour.Disable;

                if (choice.condition != null && !choice.condition.IsEmpty)
                {
                    conditionMet = choice.condition.Evaluate(profiles, out failHint);
                    failBehaviour = choice.condition.failBehaviour;

                    // Skip hidden choices entirely
                    if (!conditionMet && failBehaviour == ConditionFailBehaviour.Hide)
                        continue;
                }

                GameObject btnGo = Instantiate(choiceButtonPrefab, choicesContainer);
                spawnedButtons.Add(btnGo);

                // Build label — append hint when conditions are not met
                string rawText  = ctx != null ? ctx.Apply(choice.choiceText) : choice.choiceText;
                string fullText = conditionMet || string.IsNullOrEmpty(failHint)
                    ? rawText
                    : $"{rawText} <color=#FF6B6B><size=85%>{failHint}</size></color>";

                TextMeshProUGUI lbl = btnGo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (lbl != null)
                    lbl.text = fullText;

                Button btn = btnGo.GetComponent<Button>();
                if (btn != null)
                {
                    if (conditionMet)
                    {
                        DialogueChoice captured = choice;
                        btn.onClick.AddListener(() => manager?.OnChoiceSelected(captured));
                    }
                    else
                    {
                        // Greyed out — interactable false and dimmed color
                        btn.interactable = false;
                        if (lbl != null)
                            lbl.color = new Color(lbl.color.r, lbl.color.g, lbl.color.b, 0.45f);
                    }
                }
            }
        }

        /// <summary>
        /// Collects SurvivorGeneratedProfile from the current active mission via DialogueManager.
        /// Falls back to an empty list if no mission is active.
        /// </summary>
        private IReadOnlyList<SurvivorGeneratedProfile> GatherMissionProfiles()
        {
            ActiveMission mission = DialogueManager.Instance?.CurrentMission;
            if (mission == null || mission.Survivors.Count == 0)
                return System.Array.Empty<SurvivorGeneratedProfile>();

            var profiles = new List<SurvivorGeneratedProfile>(mission.Survivors.Count);
            foreach (SurvivorBehavior s in mission.Survivors)
            {
                if (s != null && s.GeneratedProfile != null)
                    profiles.Add(s.GeneratedProfile);
            }
            return profiles;
        }

        private void ClearChoices()
        {
            foreach (GameObject go in spawnedButtons)
                if (go != null) Destroy(go);
            spawnedButtons.Clear();
        }

        private void ShowMoreIndicator(bool show)
        {
            if (moreTextIndicator != null)
                moreTextIndicator.gameObject.SetActive(show);
        }
    }
}
