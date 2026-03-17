using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Panel modal "Préparer la mission".
    ///
    /// Nourriture et Eau se saisissent via boutons −/+ ou champ libre.
    /// Le bouton "Recommandé" calcule : personnes × (jours²).
    /// À la confirmation, retire les objets physiques du stockage.
    /// </summary>
    public class MissionEquipmentUI : MonoBehaviour
    {
        private static readonly Color ColOk      = new Color(0.40f, 1.00f, 0.50f);
        private static readonly Color ColWarning = new Color(1.00f, 0.80f, 0.20f);
        private static readonly Color ColZero    = new Color(0.50f, 0.50f, 0.50f);
        private static readonly Color ColOver    = new Color(1.00f, 0.30f, 0.30f);

        [Header("Résumé")]
        [SerializeField] private TextMeshProUGUI missionSummaryLabel;

        [Header("Nourriture")]
        [SerializeField] private Button          foodMinus;
        [SerializeField] private Button          foodPlus;
        [SerializeField] private TMP_InputField  foodInput;
        [SerializeField] private TextMeshProUGUI foodStockLabel;    // "X disponibles"

        [Header("Eau")]
        [SerializeField] private Button          waterMinus;
        [SerializeField] private Button          waterPlus;
        [SerializeField] private TMP_InputField  waterInput;
        [SerializeField] private TextMeshProUGUI waterStockLabel;

        [Header("Boutons")]
        [SerializeField] private Button          recommendButton;
        [SerializeField] private TextMeshProUGUI recommendLabel;    // affiche la formule
        [SerializeField] private Button          confirmButton;
        [SerializeField] private TextMeshProUGUI confirmLabel;
        [SerializeField] private Button          cancelButton;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private System.Action<int, int> onConfirmed;
        private int maxFood;
        private int maxWater;
        private int survivorCount;
        private int days;

        private int FoodQty  => ParseInput(foodInput);
        private int WaterQty => ParseInput(waterInput);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            foodMinus?.onClick.AddListener(() => Step(foodInput,  -1, () => maxFood));
            foodPlus?.onClick.AddListener(()  => Step(foodInput,  +1, () => maxFood));
            waterMinus?.onClick.AddListener(() => Step(waterInput, -1, () => maxWater));
            waterPlus?.onClick.AddListener(()  => Step(waterInput, +1, () => maxWater));

            foodInput?.onEndEdit.AddListener(_ => ClampAndRefresh());
            waterInput?.onEndEdit.AddListener(_ => ClampAndRefresh());

            recommendButton?.onClick.AddListener(OnRecommend);
            confirmButton?.onClick.AddListener(OnConfirm);
            cancelButton?.onClick.AddListener(OnCancel);

            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Ouvre le panel.
        /// </summary>
        /// <param name="zoneName">Nom affiché dans le résumé.</param>
        /// <param name="daysCount">Durée en jours.</param>
        /// <param name="survivors">Nombre de survivants dans l'équipe.</param>
        /// <param name="confirmed">Callback (food, water) si confirmé.</param>
        public void Open(string zoneName, int daysCount, int survivors,
                         System.Action<int, int> confirmed)
        {
            onConfirmed   = confirmed;
            days          = daysCount;
            survivorCount = survivors;

            maxFood  = StorageRegistry.CountItems(ResourceType.Food);
            maxWater = StorageRegistry.CountItems(ResourceType.Water);

            if (missionSummaryLabel != null)
                missionSummaryLabel.text =
                    $"→  {zoneName.ToUpper()}   —   {daysCount} jour{(daysCount > 1 ? "s" : "")}   —   {survivors} survivant{(survivors > 1 ? "s" : "")}";

            SetInput(foodInput,  0);
            SetInput(waterInput, 0);

            // Formule recommandée visible dès l'ouverture
            int rec = Recommended();
            if (recommendLabel != null)
                recommendLabel.text = $"Recommandé  ({survivorCount} × {days}² = {rec})";

            RefreshAll();
            gameObject.SetActive(true);
        }

        // ── Handlers ─────────────────────────────────────────────────────────────

        private void Step(TMP_InputField input, int delta, System.Func<int> getMax)
        {
            if (input == null) return;
            int v = Mathf.Clamp(ParseInput(input) + delta, 0, getMax());
            SetInput(input, v);
            RefreshAll();
        }

        private void ClampAndRefresh()
        {
            SetInput(foodInput,  Mathf.Clamp(FoodQty,  0, maxFood));
            SetInput(waterInput, Mathf.Clamp(WaterQty, 0, maxWater));
            RefreshAll();
        }

        private void OnRecommend()
        {
            int rec = Recommended();
            SetInput(foodInput,  Mathf.Clamp(rec, 0, maxFood));
            SetInput(waterInput, Mathf.Clamp(rec, 0, maxWater));
            RefreshAll();
        }

        private void OnConfirm()
        {
            int food  = FoodQty;
            int water = WaterQty;

            StorageSpawner spawner = StorageSpawner.Instance;
            if (spawner != null)
            {
                for (int i = 0; i < food;  i++) StorageRegistry.ConsumeItem(ResourceType.Food);
                for (int i = 0; i < water; i++) StorageRegistry.ConsumeItem(ResourceType.Water);
            }

            gameObject.SetActive(false);
            onConfirmed?.Invoke(food, water);
        }

        private void OnCancel() => gameObject.SetActive(false);

        // ── Refresh ───────────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshStock(foodStockLabel,  FoodQty,  maxFood);
            RefreshStock(waterStockLabel, WaterQty, maxWater);

            if (foodMinus  != null) foodMinus.interactable  = FoodQty  > 0;
            if (foodPlus   != null) foodPlus.interactable   = FoodQty  < maxFood;
            if (waterMinus != null) waterMinus.interactable = WaterQty > 0;
            if (waterPlus  != null) waterPlus.interactable  = WaterQty < maxWater;

            if (confirmLabel != null)
            {
                int f = FoodQty, w = WaterQty;
                confirmLabel.text = (f > 0 || w > 0)
                    ? $"Confirmer  ({f}🍗  {w}💧)"
                    : "Confirmer sans provisions";
            }
        }

        private static void RefreshStock(TextMeshProUGUI label, int qty, int max)
        {
            if (label == null) return;
            label.text  = max > 0 ? $"{qty}  /  {max} disponibles" : "Aucun en stock";
            label.color = qty > max ? ColOver : qty > 0 ? ColOk : max > 0 ? ColWarning : ColZero;
        }

        // ── Formule recommandée ───────────────────────────────────────────────────

        /// <summary>personnes × (jours²)</summary>
        private int Recommended() => survivorCount * (days * days);

        // ── Utilitaires ───────────────────────────────────────────────────────────

        private static int ParseInput(TMP_InputField f)
            => f != null && int.TryParse(f.text, out int v) ? Mathf.Max(0, v) : 0;

        private static void SetInput(TMP_InputField f, int v)
        {
            if (f != null) f.text = v.ToString();
        }
    }
}
