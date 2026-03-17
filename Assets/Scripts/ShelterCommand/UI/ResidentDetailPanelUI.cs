using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Drives the DetailsColumn Canvas UI for a single survivor.
    /// All serialized references are wired in the Inspector or injected via InjectColumn().
    /// Call Show(survivor) to populate every section, Hide() to clear.
    /// Needs bars (Faim, Fatigue, Stress, Moral) update every frame via Update().
    /// </summary>
    public class ResidentDetailPanelUI : MonoBehaviour
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI identityText;

        // ── Stat Bars ─────────────────────────────────────────────────────────────
        [Header("Stat Bars")]
        [SerializeField] private Image           forceFill;
        [SerializeField] private TextMeshProUGUI forceValue;
        [SerializeField] private Image           intelligenceFill;
        [SerializeField] private TextMeshProUGUI intelligenceValue;
        [SerializeField] private Image           techniqueFill;
        [SerializeField] private TextMeshProUGUI techniqueValue;
        [SerializeField] private Image           socialFill;
        [SerializeField] private TextMeshProUGUI socialValue;
        [SerializeField] private Image           enduranceFill;
        [SerializeField] private TextMeshProUGUI enduranceValue;
        [SerializeField] private TextMeshProUGUI totalText;

        // ── Talents ───────────────────────────────────────────────────────────────
        [Header("Talents")]
        [Tooltip("VerticalLayoutGroup container — talent rows are instantiated here at runtime.")]
        [SerializeField] private Transform  talentsContainer;
        [Tooltip("Prefab: HorizontalLayoutGroup with two TMP children: [0] name, [1] description.")]
        [SerializeField] private GameObject talentEntryPrefab;

        // ── Character Traits ──────────────────────────────────────────────────────
        [Header("Character Traits")]
        [SerializeField] private TextMeshProUGUI posTraitName;
        [SerializeField] private TextMeshProUGUI posTraitDesc;
        [SerializeField] private TextMeshProUGUI negTraitName;
        [SerializeField] private TextMeshProUGUI negTraitDesc;

        // ── Needs Bars (real-time) ────────────────────────────────────────────────
        [Header("Needs Bars (real-time)")]
        [SerializeField] private Image           faimFill;
        [SerializeField] private TextMeshProUGUI faimValue;
        [SerializeField] private Image           fatigueFill;
        [SerializeField] private TextMeshProUGUI fatigueValue;
        [SerializeField] private Image           stressFill;
        [SerializeField] private TextMeshProUGUI stressValue;
        [SerializeField] private Image           moralFill;
        [SerializeField] private TextMeshProUGUI moralValue;

        // ── Colors ────────────────────────────────────────────────────────────────
        private static readonly Color ColName     = new Color(0.55f, 1.00f, 0.55f);
        private static readonly Color ColPosTrait = new Color(0.45f, 1.00f, 0.65f);
        private static readonly Color ColNegTrait = new Color(1.00f, 0.35f, 0.35f);
        private static readonly Color ColTalent   = new Color(1.00f, 0.85f, 0.35f);
        private static readonly Color ColMuted    = new Color(0.50f, 0.50f, 0.50f);

        // ── Runtime ───────────────────────────────────────────────────────────────
        private SurvivorBehavior          currentSurvivor;
        private readonly List<GameObject> spawnedTalents = new List<GameObject>();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (currentSurvivor != null)
                UpdateNeedsBars();
        }

        // ── Setup API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Optional injection hook used by ResidentsPanelUI when the component is added at runtime.
        /// No-op here since all UI bindings are done via Inspector references.
        /// </summary>
        /// <summary>
        /// Appelé par ResidentsPanelUI après AddComponent.
        /// Navigue dans la hiérarchie réelle de DetailsColumn pour relier chaque champ.
        /// Structure attendue telle que construite dans la scène :
        ///   Content/NameRow/NameText, StatusText
        ///   Content/IdentityText
        ///   Content/StatForce/BarBg/BarFill  +  StatValue
        ///   Content/TotalRow (1er TMP de la rangée)
        ///   Content/TalentsContainer
        ///   Content/PosTraitRow/PosTraitName, PosTraitDesc
        ///   Content/NegTraitRow/NegTraitName, NegTraitDesc
        ///   Content/NeedFaim/BarBg/BarFill   +  NeedValue
        ///   (idem Fatigue, Stress, Moral)
        /// </summary>
        public void InjectColumn(RectTransform column)
        {
            if (column == null) return;

            // ── Racine du contenu défilant ─────────────────────────────────────────
            Transform root = column.Find("DetailScrollRect/Viewport/Content") ?? column;

            // ── Identité ──────────────────────────────────────────────────────────
            nameText     ??= FindTMP(root, "NameRow/NameText");
            statusText   ??= FindTMP(root, "NameRow/StatusText");
            identityText ??= FindTMP(root, "IdentityText");

            // ── Barres de stats ───────────────────────────────────────────────────
            forceFill         ??= FindImg(root, "StatForce/BarBg/BarFill");
            forceValue        ??= FindTMP(root, "StatForce/StatValue");
            intelligenceFill  ??= FindImg(root, "StatIntelligence/BarBg/BarFill");
            intelligenceValue ??= FindTMP(root, "StatIntelligence/StatValue");
            techniqueFill     ??= FindImg(root, "StatTechnique/BarBg/BarFill");
            techniqueValue    ??= FindTMP(root, "StatTechnique/StatValue");
            socialFill        ??= FindImg(root, "StatSocial/BarBg/BarFill");
            socialValue       ??= FindTMP(root, "StatSocial/StatValue");
            enduranceFill     ??= FindImg(root, "StatEndurance/BarBg/BarFill");
            enduranceValue    ??= FindTMP(root, "StatEndurance/StatValue");

            // TotalRow : le premier TMP de la rangée
            if (totalText == null)
            {
                Transform totalRow = root.Find("TotalRow");
                if (totalRow != null)
                    totalText = totalRow.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // ── Talents ───────────────────────────────────────────────────────────
            talentsContainer ??= root.Find("TalentsContainer");

            // ── Traits ────────────────────────────────────────────────────────────
            posTraitName ??= FindTMP(root, "PosTraitRow/PosTraitName");
            posTraitDesc ??= FindTMP(root, "PosTraitRow/PosTraitDesc");
            negTraitName ??= FindTMP(root, "NegTraitRow/NegTraitName");
            negTraitDesc ??= FindTMP(root, "NegTraitRow/NegTraitDesc");

            // ── Besoins en temps réel ─────────────────────────────────────────────
            faimFill     ??= FindImg(root, "NeedFaim/BarBg/BarFill");
            faimValue    ??= FindTMP(root, "NeedFaim/NeedValue");
            fatigueFill  ??= FindImg(root, "NeedFatigue/BarBg/BarFill");
            fatigueValue ??= FindTMP(root, "NeedFatigue/NeedValue");
            stressFill   ??= FindImg(root, "NeedStress/BarBg/BarFill");
            stressValue  ??= FindTMP(root, "NeedStress/NeedValue");
            moralFill    ??= FindImg(root, "NeedMoral/BarBg/BarFill");
            moralValue   ??= FindTMP(root, "NeedMoral/NeedValue");
        }

        // Cherche par chemin relatif (ex. "StatForce/BarBg/BarFill")
        private static TextMeshProUGUI FindTMP(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Image FindImg(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Transform FindTf(Transform root, string path)
            => root.Find(path);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Populates every UI section with the given survivor's data and shows the panel.</summary>
        public void Show(SurvivorBehavior survivor)
        {
            currentSurvivor = survivor;
            gameObject.SetActive(true);

            if (survivor == null) return;

            SurvivorGeneratedProfile p = survivor.GeneratedProfile;

            // Identity header
            Set(nameText, survivor.SurvivorName.ToUpper(), ColName);
            (string statusLabel, Color statusColor) = GetStatusInfo(survivor);
            Set(statusText, statusLabel, statusColor);

            if (p != null)
            {
                string genderStr = p.gender == SurvivorGender.Female ? "F" : "H";
                Set(identityText, $"{p.age} ans   {genderStr}   {ProfessionBonusTable.GetLabel(p.profession)}");

                // Stat bars
                ApplyBar(forceFill,        forceValue,        p.Force);
                ApplyBar(intelligenceFill, intelligenceValue, p.Intelligence);
                ApplyBar(techniqueFill,    techniqueValue,    p.Technique);
                ApplyBar(socialFill,       socialValue,       p.Social);
                ApplyBar(enduranceFill,    enduranceValue,    p.Endurance);

                // Total
                if (totalText != null)
                {
                    Color tc = p.TotalStats >= 130 ? new Color(0.40f, 1.00f, 0.80f) :
                               p.TotalStats >= 100 ? new Color(0.70f, 0.70f, 1.00f) :
                                                     new Color(0.60f, 0.60f, 0.60f);
                    totalText.text  = $"Total   {p.TotalStats} pts";
                    totalText.color = tc;
                }

                // Talents
                RebuildTalents(p);

                // Traits
                Set(posTraitName, TraitLabels.GetLabel(p.positiveTrait), ColPosTrait, bold: true);
                Set(posTraitDesc, GetPosDesc(p.positiveTrait), ColMuted);
                Set(negTraitName, TraitLabels.GetLabel(p.negativeTrait), ColNegTrait, bold: true);
                Set(negTraitDesc, GetNegDesc(p.negativeTrait), ColMuted);
            }

            // Needs bars — immediate snapshot on open
            UpdateNeedsBars();
        }

        /// <summary>Hides the panel and releases the survivor reference.</summary>
        public void Hide()
        {
            currentSurvivor = null;
            gameObject.SetActive(false);
        }

        // ── Barres ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applique la valeur sur une barre de stat (affichage immédiat).
        /// Utilise anchorMax.x pour que la fill s'étire proportionnellement dans son BarBg parent.
        /// La couleur varie : vert ≥70, jaune ≥40, rouge en dessous.
        /// </summary>
        private static void ApplyBar(Image fill, TextMeshProUGUI label, int value)
        {
            Color col = StatColor(value);
            if (fill != null)
            {
                SetBarFill(fill, Mathf.Clamp01(value / 100f), col);
            }
            if (label != null) { label.text = value.ToString(); label.color = col; }
        }

        // ── Besoins (temps réel, interpolé) ──────────────────────────────────────

        private void UpdateNeedsBars()
        {
            // Faim / Fatigue / Stress : valeur élevée = mauvais (rouge quand haut)
            SmoothBar(faimFill,    faimValue,    currentSurvivor.Hunger,  inverted: true);
            SmoothBar(fatigueFill, fatigueValue, currentSurvivor.Fatigue, inverted: true);
            SmoothBar(stressFill,  stressValue,  currentSurvivor.Stress,  inverted: true);
            // Moral : valeur élevée = bon (vert quand haut)
            SmoothBar(moralFill,   moralValue,   currentSurvivor.Morale,  inverted: false);
        }

        private static void SmoothBar(Image fill, TextMeshProUGUI label, int value, bool inverted)
        {
            if (fill == null) return;
            float target  = Mathf.Clamp01(value / 100f);
            float current = fill.rectTransform.anchorMax.x;
            float next    = Mathf.Lerp(current, target, Time.deltaTime * 10f);
            Color col     = inverted ? NeedsColor(value) : MoraleColor(value);
            SetBarFill(fill, next, col);
            if (label != null) { label.text = value.ToString(); label.color = col; }
        }

        /// <summary>
        /// Applique un ratio [0–1] sur l'Image fill en modifiant son anchorMax.x.
        /// BarFill doit être un enfant direct de BarBg avec anchorMin=(0,0) et anchorMax=(1,1) au départ.
        /// </summary>
        private static void SetBarFill(Image fill, float ratio, Color col)
        {
            RectTransform rt = fill.rectTransform;
            rt.anchorMin  = new Vector2(0f, 0f);
            rt.anchorMax  = new Vector2(ratio, 1f);
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
            fill.color    = col;
        }

        // ── Talents ───────────────────────────────────────────────────────────────

        private void RebuildTalents(SurvivorGeneratedProfile p)
        {
            foreach (GameObject go in spawnedTalents)
                if (go != null) Destroy(go);
            spawnedTalents.Clear();

            if (talentsContainer == null || talentEntryPrefab == null) return;

            bool hasTalents = p.Talents != null && p.Talents.Count > 0;
            if (!hasTalents)
            {
                GameObject empty = Instantiate(talentEntryPrefab, talentsContainer);
                spawnedTalents.Add(empty);
                TextMeshProUGUI[] lbls = empty.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (lbls.Length > 0) { lbls[0].text = "Aucun talent"; lbls[0].color = ColMuted; }
                if (lbls.Length > 1) lbls[1].text = string.Empty;
                return;
            }

            foreach (SurvivorTalent talent in p.Talents)
            {
                GameObject row = Instantiate(talentEntryPrefab, talentsContainer);
                spawnedTalents.Add(row);
                TextMeshProUGUI[] lbls = row.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (lbls.Length > 0) { lbls[0].text = TalentTable.GetLabel(talent);       lbls[0].color = ColTalent; }
                if (lbls.Length > 1) { lbls[1].text = TalentTable.GetDescription(talent); lbls[1].color = ColMuted;  }
            }
        }

        // ── Color helpers ─────────────────────────────────────────────────────────

        private static Color StatColor(int v) =>
            v >= 70 ? new Color(0.30f, 0.90f, 0.50f) :
            v >= 40 ? new Color(0.80f, 0.85f, 0.25f) :
                      new Color(0.90f, 0.30f, 0.20f);

        /// <summary>Inverted: high value (e.g. high hunger) = bad = red.</summary>
        private static Color NeedsColor(int v) =>
            v >= 70 ? new Color(0.90f, 0.25f, 0.20f) :
            v >= 40 ? new Color(0.90f, 0.70f, 0.15f) :
                      new Color(0.25f, 0.85f, 0.40f);

        /// <summary>Normal: high value (high morale) = good = green.</summary>
        private static Color MoraleColor(int v) =>
            v >= 60 ? new Color(0.30f, 0.85f, 0.55f) :
            v >= 30 ? new Color(0.85f, 0.70f, 0.15f) :
                      new Color(0.90f, 0.25f, 0.25f);

        private static (string label, Color color) GetStatusInfo(SurvivorBehavior sb)
        {
            if (!sb.IsAlive)    return ("DECEDE",     new Color(0.8f, 0.2f, 0.2f));
            if (sb.IsOnMission) return ("EN MISSION", new Color(1.0f, 0.8f, 0.2f));
            if (sb.IsSick)      return ("MALADE",     new Color(1.0f, 0.5f, 0.1f));
            if (sb.IsArrested)  return ("ARRETE",     new Color(1.0f, 0.3f, 0.3f));
            return ("ACTIF", new Color(0.4f, 1.0f, 0.5f));
        }

        // ── Trait descriptions ────────────────────────────────────────────────────

        private static string GetPosDesc(PositiveTrait t) => t switch
        {
            PositiveTrait.Travailleur => "Productivite +",
            PositiveTrait.Calme       => "Stress monte lentement",
            PositiveTrait.Courageux   => "Accepte missions risquees",
            PositiveTrait.Genereux    => "Reduit stress des voisins",
            PositiveTrait.Loyal       => "Refuse rarement un ordre",
            PositiveTrait.Createur    => "Bonus Technique au craft",
            PositiveTrait.Empathique  => "Reduit stress du groupe",
            PositiveTrait.Strategique => "Bonus Intel en mission",
            _                         => string.Empty,
        };

        private static string GetNegDesc(NegativeTrait t) => t switch
        {
            NegativeTrait.Peureux    => "Refuse missions dangereuses",
            NegativeTrait.Belliqueux => "Augmente stress des voisins",
            NegativeTrait.Egoiste    => "Consomme plus de nourriture",
            NegativeTrait.Paresseux  => "Fatigue monte plus vite",
            NegativeTrait.Impulsif   => "Incidents aleatoires",
            NegativeTrait.Pessimiste => "Moral descend plus vite",
            NegativeTrait.Kleptomann => "Peut voler des ressources",
            NegativeTrait.Fragile    => "Tombe malade facilement",
            _                        => string.Empty,
        };

        // ── Utility ───────────────────────────────────────────────────────────────

        private static void Set(TextMeshProUGUI t, string s, Color c = default, bool bold = false)
        {
            if (t == null) return;
            t.text      = s;
            if (c != default) t.color = c;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
