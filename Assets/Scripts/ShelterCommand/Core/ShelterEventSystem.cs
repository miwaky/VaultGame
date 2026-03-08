using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Generates and resolves random shelter events each day.
    /// Fires OnEventTriggered so the HUD can display a popup.
    /// </summary>
    public class ShelterEventSystem : MonoBehaviour
    {
        [Header("Event Settings")]
        [SerializeField, Range(0f, 1f)] private float dailyEventChance = 0.35f;

        public event Action<ShelterEvent> OnEventTriggered;

        // ── Event definitions ────────────────────────────────────────────────────

        private static readonly List<ShelterEvent> EventPool = new List<ShelterEvent>
        {
            new ShelterEvent(
                id: "food_theft",
                title: "Vol de nourriture",
                description: "Un survivant a été surpris à voler des rations. Que faire ?",
                EventCategory.Crisis,
                new EventChoice("Sanctionner",   "Arrêter le coupable.  ► Moral -15 / Ordre +"),
                new EventChoice("Ignorer",        "Fermer les yeux.      ► Nourriture -15 / Tension +")),

            new ShelterEvent(
                id: "sickness_outbreak",
                title: "Épidémie",
                description: "Plusieurs survivants montrent des signes de maladie.",
                EventCategory.Medical,
                new EventChoice("Soigner",        "Traiter les malades.  ► Médicaments -15"),
                new EventChoice("Isoler",          "Mise en quarantaine.  ► Moral -20 / Stress +")),

            new ShelterEvent(
                id: "survivor_dispute",
                title: "Dispute violente",
                description: "Une altercation éclate entre deux survivants.",
                EventCategory.Social,
                new EventChoice("Médiation",       "Résoudre pacifiquement. ► Stress -10 / Temps perdu"),
                new EventChoice("Forcer l'ordre", "Arrêter les deux.       ► Moral -15 / Ordre +")),

            new ShelterEvent(
                id: "generator_failure",
                title: "Panne du générateur",
                description: "Le générateur tombe en panne. L'énergie s'effondre.",
                EventCategory.Technical,
                new EventChoice("Réparer",          "Mobiliser des techniciens. ► Matériaux -20 / Énergie +50"),
                new EventChoice("Rationnement",     "Réduire la consommation.   ► Énergie -30 / Moral -10")),

            new ShelterEvent(
                id: "outsiders_knocking",
                title: "Inconnus à l'entrée",
                description: "Trois étrangers demandent à entrer. Armés mais épuisés.",
                EventCategory.External,
                new EventChoice("Laisser entrer", "Accueillir les inconnus.  ► Nourriture -20 / Eau -10"),
                new EventChoice("Refuser",         "Sécuriser l'accès.        ► Moral -5")),

            new ShelterEvent(
                id: "water_contamination",
                title: "Eau contaminée",
                description: "La réserve d'eau montre des signes de contamination.",
                EventCategory.Technical,
                new EventChoice("Traiter l'eau",   "Purification d'urgence.  ► Médicaments -10"),
                new EventChoice("Rationner",        "Réduire la distribution. ► Eau -30 / Santé -")),

            new ShelterEvent(
                id: "morale_boost",
                title: "Bonne nouvelle radio",
                description: "Un signal radio capte des nouvelles encourageantes du monde extérieur.",
                EventCategory.Social,
                new EventChoice("Diffuser à l'abri", "Partager la nouvelle.  ► Moral +15 pour tous"),
                new EventChoice("Garder le secret",  "Éviter les faux espoirs. ► Aucun effet")),

            new ShelterEvent(
                id: "medical_supplies",
                title: "Médicaments trouvés",
                description: "Un survivant découvre une cache médicale dans l'abri.",
                EventCategory.Medical,
                new EventChoice("Distribuer à tous",  "Soins généraux.       ► Médicaments +20 / Stress -"),
                new EventChoice("Réserver aux blessés","Soins ciblés.        ► Médicaments +10 / Malades guéris")),
        };

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Rolls for a random event. Returns null if no event fires.</summary>
        public ShelterEvent TryTriggerRandomEvent()
        {
            if (UnityEngine.Random.value > dailyEventChance) return null;
            if (EventPool.Count == 0) return null;

            int index = UnityEngine.Random.Range(0, EventPool.Count);
            ShelterEvent ev = EventPool[index];
            OnEventTriggered?.Invoke(ev);
            Debug.Log($"[ShelterEventSystem] Event triggered: {ev.Title}");
            return ev;
        }

        /// <summary>Resolves the chosen option of an event and applies consequences.</summary>
        public void ResolveEvent(ShelterEvent ev, int choiceIndex,
            SurvivorManager survivorManager, ShelterResourceManager resourceManager)
        {
            if (ev == null) return;

            switch (ev.Id)
            {
                case "food_theft":
                    if (choiceIndex == 0)
                    {
                        // Arrest the culprit
                        List<SurvivorBehavior> alive = survivorManager.GetAliveSurvivors();
                        if (alive.Count > 0)
                        {
                            alive[UnityEngine.Random.Range(0, alive.Count)].Arrest();
                        }
                    }
                    else
                    {
                        resourceManager.ConsumeResource(ResourceType.Food, 15);
                    }
                    break;

                case "sickness_outbreak":
                    if (choiceIndex == 0)
                    {
                        resourceManager.ConsumeResource(ResourceType.Medicine, 15);
                        foreach (SurvivorBehavior s in survivorManager.GetAliveSurvivors())
                        {
                            if (s.IsSick) s.Heal();
                        }
                    }
                    else
                    {
                        // Quarantine: stress up for all
                        foreach (SurvivorBehavior s in survivorManager.GetAliveSurvivors())
                        {
                            s.IssueOrder(OrderType.GoToInfirmary, resourceManager.Resources);
                        }
                    }
                    break;

                case "generator_failure":
                    if (choiceIndex == 0)
                    {
                        resourceManager.ConsumeResource(ResourceType.Materials, 20);
                        resourceManager.AddResources(energy: 50);
                    }
                    else
                    {
                        resourceManager.ConsumeResource(ResourceType.Energy, 30);
                    }
                    break;

                case "outsiders_knocking":
                    if (choiceIndex == 0)
                    {
                        resourceManager.ConsumeResource(ResourceType.Food, 20);
                        resourceManager.ConsumeResource(ResourceType.Water, 10);
                    }
                    break;

                case "water_contamination":
                    if (choiceIndex == 0)
                    {
                        resourceManager.ConsumeResource(ResourceType.Medicine, 10);
                    }
                    else
                    {
                        resourceManager.ConsumeResource(ResourceType.Water, 30);
                    }
                    break;

                case "morale_boost":
                    if (choiceIndex == 0)
                    {
                        foreach (SurvivorBehavior s in survivorManager.GetAliveSurvivors())
                            s.IssueOrder(OrderType.GoSleep, resourceManager.Resources); // proxy for morale boost
                    }
                    break;

                case "medical_supplies":
                    if (choiceIndex == 0)
                    {
                        resourceManager.AddResources(medicine: 20);
                    }
                    else
                    {
                        resourceManager.AddResources(medicine: 10);
                        foreach (SurvivorBehavior s in survivorManager.GetAliveSurvivors())
                            if (s.IsSick) s.Heal();
                    }
                    break;
            }

            Debug.Log($"[ShelterEventSystem] Event '{ev.Title}' resolved with choice {choiceIndex}.");
        }
    }

    // ── Supporting data types ────────────────────────────────────────────────────

    public enum EventCategory { Crisis, Medical, Social, Technical, External }

    public class EventChoice
    {
        public string Label;
        public string Tooltip;

        public EventChoice(string label, string tooltip)
        {
            Label = label;
            Tooltip = tooltip;
        }
    }

    public class ShelterEvent
    {
        public string Id;
        public string Title;
        public string Description;
        public EventCategory Category;
        public EventChoice[] Choices;

        public ShelterEvent(string id, string title, string description,
            EventCategory category, params EventChoice[] choices)
        {
            Id = id;
            Title = title;
            Description = description;
            Category = category;
            Choices = choices;
        }
    }
}
