using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Carries runtime-resolved values for token substitution in dialogue text.
    ///
    /// Supported tokens:
    ///   {amount}    → total number of items resolved by the node event
    ///   {resource}  → localised display name of the first resource type
    ///   {survivors} → comma-separated survivor names for the current mission
    ///
    /// Usage: build via <see cref="DialogueContext.Build"/> then call <see cref="Apply"/>.
    /// </summary>
    public class DialogueContext
    {
        private readonly Dictionary<string, string> tokens = new Dictionary<string, string>();

        private DialogueContext() { }

        // ── Factory ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the node event (if any), spawns resources, and builds the context.
        /// </summary>
        /// <param name="node">The dialogue node being displayed.</param>
        /// <param name="mission">Current active mission (can be null).</param>
        /// <returns>A ready-to-use context. Never null.</returns>
        public static DialogueContext Build(ExplorationDialogue node, ActiveMission mission)
        {
            var ctx = new DialogueContext();

            // ── Survivors token ───────────────────────────────────────────────────
            if (mission != null && mission.Survivors.Count > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < mission.Survivors.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(mission.Survivors[i].SurvivorName);
                }
                ctx.tokens["{survivors}"] = sb.ToString();
            }
            else
            {
                ctx.tokens["{survivors}"] = "les explorateurs";
            }

            // ── Node event resolution ─────────────────────────────────────────────
            DialogueEventData evt = node.nodeEvent;
            if (evt == null || evt.eventType == DialogueEventType.None ||
                (evt.eventType != DialogueEventType.AddResource &&
                 evt.eventType != DialogueEventType.LoseResource))
            {
                ctx.tokens["{amount}"]   = "0";
                ctx.tokens["{resource}"] = "";
                return ctx;
            }

            ResourceEntry[] entries = evt.resources;
            if (entries == null || entries.Length == 0)
            {
                ctx.tokens["{amount}"]   = "0";
                ctx.tokens["{resource}"] = "";
                return ctx;
            }

            // Resolve which entries actually fire
            ResourceEntry[] resolved;
            if (evt.selectionMode == ResourceSelectionMode.RandomOne)
                resolved = new[] { entries[Random.Range(0, entries.Length)] };
            else
                resolved = entries;

            // Compute totals per type (for {amount} and {resource})
            int totalAmount = 0;
            ResourceType firstType = resolved[0].resourceType;

            StorageSpawner spawner = StorageSpawner.Instance;
            bool isAdd = evt.eventType == DialogueEventType.AddResource;

            foreach (ResourceEntry e in resolved)
            {
                int amount = e.ResolveAmount();
                totalAmount += amount;

                if (isAdd && spawner != null)
                {
                    spawner.SpawnItems(e.resourceType, amount);
                    Debug.Log($"[DialogueContext] Node event: +{amount} {e.resourceType}");
                }
                else if (!isAdd && spawner != null)
                {
                    spawner.RemoveItems(e.resourceType, amount);
                    Debug.Log($"[DialogueContext] Node event: -{amount} {e.resourceType}");
                }
            }

            ctx.tokens["{amount}"]   = totalAmount.ToString();
            ctx.tokens["{resource}"] = LocaliseResource(firstType);
            return ctx;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Substitutes all tokens in <paramref name="text"/> and returns the result.</summary>
        public string Apply(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (KeyValuePair<string, string> kv in tokens)
                text = text.Replace(kv.Key, kv.Value);
            return text;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string LocaliseResource(ResourceType type) => type switch
        {
            ResourceType.Food      => "nourriture",
            ResourceType.Water     => "eau",
            ResourceType.Medicine  => "médicaments",
            ResourceType.Materials => "matériaux",
            ResourceType.Energy    => "énergie",
            _                      => type.ToString()
        };
    }
}
