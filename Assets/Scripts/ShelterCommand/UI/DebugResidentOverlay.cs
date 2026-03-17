using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Debug-only overlay that displays the full generated profile of every survivor.
    /// Toggled with F1. Only active in the Unity Editor (or development builds).
    ///
    /// Add this component to any persistent GameObject in the scene.
    /// No wiring required — it resolves SurvivorManager automatically.
    /// </summary>
    public class DebugResidentOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        // ── Constants ─────────────────────────────────────────────────────────────
        private const float PanelWidth      = 340f;
        private const float PanelHeight     = 620f;
        private const float HeaderHeight    = 28f;
        private const float RowHeight       = 18f;
        private const float Padding         = 10f;
        private const float ScrollbarWidth  = 12f;
        private const int   FontSizeNormal  = 12;
        private const int   FontSizeHeader  = 14;

        // ── Colors ────────────────────────────────────────────────────────────────
        private static readonly Color BgColor         = new Color(0.04f, 0.04f, 0.04f, 0.92f);
        private static readonly Color HeaderBg        = new Color(0.10f, 0.22f, 0.10f, 1.00f);
        private static readonly Color SeparatorColor  = new Color(0.20f, 0.40f, 0.20f, 0.80f);
        private static readonly Color ColorName       = new Color(0.55f, 1.00f, 0.55f);
        private static readonly Color ColorStat       = new Color(0.70f, 0.90f, 1.00f);
        private static readonly Color ColorTalent     = new Color(1.00f, 0.85f, 0.35f);
        private static readonly Color ColorPosTrait   = new Color(0.55f, 1.00f, 0.75f);
        private static readonly Color ColorNegTrait   = new Color(1.00f, 0.45f, 0.45f);
        private static readonly Color ColorStatus     = new Color(0.80f, 0.80f, 0.80f);
        private static readonly Color ColorMuted      = new Color(0.50f, 0.50f, 0.50f);
        private static readonly Color ColorLabel      = new Color(0.65f, 0.65f, 0.65f);

        // ── State ─────────────────────────────────────────────────────────────────
        private bool              isVisible      = false;
        private Vector2           scrollPos      = Vector2.zero;
        private SurvivorManager   survivorManager;

        // ── Cached styles ─────────────────────────────────────────────────────────
        private GUIStyle styleBg;
        private GUIStyle styleHeader;
        private GUIStyle styleNormal;
        private GUIStyle styleBold;
        private GUIStyle styleTitle;
        private bool     stylesBuilt = false;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                isVisible = !isVisible;
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            BuildStyles();
            ResolveManager();

            float x = Screen.width  - PanelWidth - 12f;
            float y = 12f;

            // Outer panel
            GUI.Box(new Rect(x - Padding, y - Padding,
                             PanelWidth + Padding * 2f, PanelHeight + Padding * 2f), GUIContent.none, styleBg);

            // Header bar
            GUI.Box(new Rect(x - Padding, y - Padding,
                             PanelWidth + Padding * 2f, HeaderHeight + Padding), GUIContent.none, styleHeader);

            GUI.Label(new Rect(x, y, PanelWidth, HeaderHeight),
                "  ⚙  DEBUG — PROFILS RÉSIDENTS   [F1]", styleTitle);

            y += HeaderHeight + Padding + 4f;

            if (survivorManager == null)
            {
                GUI.Label(new Rect(x, y, PanelWidth, RowHeight),
                    "<color=#FF6666>SurvivorManager introuvable.</color>", styleNormal);
                return;
            }

            IReadOnlyList<SurvivorBehavior> survivors = survivorManager.Survivors;
            if (survivors == null || survivors.Count == 0)
            {
                GUI.Label(new Rect(x, y, PanelWidth, RowHeight),
                    "<color=#AAAAAA>Aucun résident initialisé.</color>", styleNormal);
                return;
            }

            // ── Scrollable content ────────────────────────────────────────────────
            float scrollAreaHeight = PanelHeight - (y - 12f) - Padding;
            float contentHeight    = EstimateContentHeight(survivors.Count);

            Rect scrollView  = new Rect(x - Padding, y, PanelWidth + Padding * 2f, scrollAreaHeight);
            Rect scrollContent = new Rect(0f, 0f, PanelWidth - ScrollbarWidth, contentHeight);

            scrollPos = GUI.BeginScrollView(scrollView, scrollPos, scrollContent, false, true);

            float cy = 4f;

            foreach (SurvivorBehavior sb in survivors)
            {
                if (sb == null) continue;
                cy = DrawSurvivorBlock(sb, Padding, cy);
            }

            GUI.EndScrollView();
        }

        // ── Drawing ───────────────────────────────────────────────────────────────

        private float DrawSurvivorBlock(SurvivorBehavior sb, float bx, float by)
        {
            SurvivorGeneratedProfile p = sb.GeneratedProfile;
            float w = PanelWidth - ScrollbarWidth - Padding;

            // ── Name + status ─────────────────────────────────────────────────────
            string status = !sb.IsAlive    ? "<color=#FF4444>✝ DÉCÉDÉ</color>"    :
                             sb.IsOnMission ? "<color=#FFCC44>✈ EN MISSION</color>" :
                             sb.IsSick      ? "<color=#FF8844>✚ MALADE</color>"     :
                             sb.IsArrested  ? "<color=#FF6666>⚑ ARRÊTÉ</color>"     :
                                              "<color=#66FF66>● Actif</color>";

            DrawColoredLabel(bx, by, w * 0.62f, sb.SurvivorName.ToUpper(), ColorName, true);
            DrawColoredLabel(bx + w * 0.63f, by, w * 0.37f, status, ColorStatus);
            by += RowHeight;

            if (p == null)
            {
                DrawColoredLabel(bx, by, w, "— profil non généré —", ColorMuted);
                by += RowHeight + 4f;
                DrawSeparator(bx, by, w); by += 6f;
                return by;
            }

            // ── Identity ──────────────────────────────────────────────────────────
            string genderLabel = p.gender == SurvivorGender.Female ? "Femme" : "Homme";
            string profLabel   = ProfessionBonusTable.GetLabel(p.profession);
            DrawLabelPair(bx, by, w, "Identité",
                $"{p.age} ans  •  {genderLabel}  •  {profLabel}");
            by += RowHeight;

            // ── Stats ─────────────────────────────────────────────────────────────
            DrawSectionTitle(bx, by, w, "STATS"); by += RowHeight;
            DrawStatBar(bx, by, w, "Force",        p.Force,        ColorStat); by += RowHeight;
            DrawStatBar(bx, by, w, "Intelligence", p.Intelligence, ColorStat); by += RowHeight;
            DrawStatBar(bx, by, w, "Technique",    p.Technique,    ColorStat); by += RowHeight;
            DrawStatBar(bx, by, w, "Social",       p.Social,       ColorStat); by += RowHeight;
            DrawStatBar(bx, by, w, "Endurance",    p.Endurance,    ColorStat); by += RowHeight;

            string totalColor = p.TotalStats >= 130 ? "#66FFCC" :
                                p.TotalStats >= 100 ? "#AAAAFF" : "#AAAAAA";
            DrawLabelPair(bx, by, w, "Total",
                $"<color={totalColor}>{p.TotalStats} pts</color>");
            by += RowHeight;

            // ── Talents ───────────────────────────────────────────────────────────
            DrawSectionTitle(bx, by, w, "TALENTS"); by += RowHeight;
            if (p.Talents == null || p.Talents.Count == 0)
            {
                DrawColoredLabel(bx + 8f, by, w, "— aucun talent —", ColorMuted);
                by += RowHeight;
            }
            else
            {
                foreach (SurvivorTalent talent in p.Talents)
                {
                    string desc = TalentTable.GetDescription(talent);
                    DrawColoredLabel(bx + 8f, by, w * 0.40f,
                        TalentTable.GetLabel(talent), ColorTalent, true);
                    DrawColoredLabel(bx + w * 0.42f, by, w * 0.58f,
                        desc, ColorMuted);
                    by += RowHeight;
                }
            }

            // ── Traits ────────────────────────────────────────────────────────────
            DrawSectionTitle(bx, by, w, "CARACTÈRE"); by += RowHeight;

            string posLabel = TraitLabels.GetLabel(p.positiveTrait);
            string posDesc  = GetPositiveTraitDescription(p.positiveTrait);
            DrawColoredLabel(bx + 8f, by, w * 0.40f,
                $"+ {posLabel}", ColorPosTrait, true);
            DrawColoredLabel(bx + w * 0.42f, by, w * 0.58f, posDesc, ColorMuted);
            by += RowHeight;

            string negLabel = TraitLabels.GetLabel(p.negativeTrait);
            string negDesc  = GetNegativeTraitDescription(p.negativeTrait);
            DrawColoredLabel(bx + 8f, by, w * 0.40f,
                $"— {negLabel}", ColorNegTrait, true);
            DrawColoredLabel(bx + w * 0.42f, by, w * 0.58f, negDesc, ColorMuted);
            by += RowHeight + 2f;

            // ── Needs (runtime) ───────────────────────────────────────────────────
            DrawSectionTitle(bx, by, w, "ÉTAT TEMPS RÉEL"); by += RowHeight;
            DrawNeedsBar(bx, by, w, "Faim",    sb.Hunger);  by += RowHeight;
            DrawNeedsBar(bx, by, w, "Fatigue", sb.Fatigue); by += RowHeight;
            DrawNeedsBar(bx, by, w, "Stress",  sb.Stress);  by += RowHeight;
            DrawMoraleBar(bx, by, w, sb.Morale);             by += RowHeight + 4f;

            DrawSeparator(bx, by, w); by += 8f;
            return by;
        }

        // ── Primitive draw helpers ────────────────────────────────────────────────

        private void DrawSectionTitle(float x, float y, float w, string title)
        {
            GUI.Label(new Rect(x, y, w, RowHeight),
                $"<color=#44AAFF><b>▸ {title}</b></color>", styleNormal);
        }

        private void DrawColoredLabel(float x, float y, float w, string text, Color c, bool bold = false)
        {
            string hex = ColorUtility.ToHtmlStringRGB(c);
            string content = bold ? $"<b><color=#{hex}>{text}</color></b>"
                                  : $"<color=#{hex}>{text}</color>";
            GUI.Label(new Rect(x, y, w, RowHeight), content, styleNormal);
        }

        private void DrawLabelPair(float x, float y, float w, string label, string value)
        {
            string lHex = ColorUtility.ToHtmlStringRGB(ColorLabel);
            GUI.Label(new Rect(x, y, w * 0.32f, RowHeight),
                $"<color=#{lHex}>{label}</color>", styleNormal);
            GUI.Label(new Rect(x + w * 0.33f, y, w * 0.67f, RowHeight),
                value, styleNormal);
        }

        private void DrawStatBar(float x, float y, float w, string statName, int value, Color barColor)
        {
            float labelW = 90f;
            float numW   = 28f;
            float barW   = w - labelW - numW - 6f;
            float barX   = x + labelW;

            string lHex = ColorUtility.ToHtmlStringRGB(ColorLabel);
            GUI.Label(new Rect(x, y, labelW, RowHeight),
                $"<color=#{lHex}>{statName}</color>", styleNormal);

            // Bar background
            GUI.Box(new Rect(barX, y + 4f, barW, RowHeight - 8f), GUIContent.none);

            // Bar fill
            float fill = Mathf.Clamp01(value / 100f);
            Color fillColor = value >= 70 ? new Color(0.3f, 0.9f, 0.5f) :
                              value >= 40 ? new Color(0.7f, 0.85f, 0.4f) :
                                            new Color(0.9f, 0.4f, 0.3f);
            Color old = GUI.color;
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(barX + 1f, y + 5f, (barW - 2f) * fill, RowHeight - 10f),
                Texture2D.whiteTexture);
            GUI.color = old;

            // Numeric value
            string vHex = ColorUtility.ToHtmlStringRGB(barColor);
            GUI.Label(new Rect(barX + barW + 4f, y, numW, RowHeight),
                $"<color=#{vHex}><b>{value}</b></color>", styleNormal);
        }

        private void DrawNeedsBar(float x, float y, float w, string label, int value)
        {
            // Needs are bad when high — invert color logic
            Color barColor = value >= 70 ? new Color(0.9f, 0.35f, 0.25f) :
                             value >= 40 ? new Color(0.9f, 0.75f, 0.3f)  :
                                           new Color(0.4f, 0.85f, 0.4f);
            DrawStatBar(x, y, w, label, value, barColor);
        }

        private void DrawMoraleBar(float x, float y, float w, int morale)
        {
            float labelW = 90f;
            float numW   = 28f;
            float barW   = w - labelW - numW - 6f;
            float barX   = x + labelW;

            string lHex = ColorUtility.ToHtmlStringRGB(ColorLabel);
            GUI.Label(new Rect(x, y, labelW, RowHeight),
                $"<color=#{lHex}>Moral</color>", styleNormal);

            GUI.Box(new Rect(barX, y + 4f, barW, RowHeight - 8f), GUIContent.none);

            float fill = Mathf.Clamp01(morale / 100f);
            Color fillColor = morale >= 60 ? new Color(0.3f, 0.85f, 0.5f) :
                              morale >= 30 ? new Color(0.8f, 0.7f, 0.2f)  :
                                             new Color(0.9f, 0.25f, 0.25f);
            Color old = GUI.color;
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(barX + 1f, y + 5f, (barW - 2f) * fill, RowHeight - 10f),
                Texture2D.whiteTexture);
            GUI.color = old;

            string colHex = ColorUtility.ToHtmlStringRGB(fillColor);
            GUI.Label(new Rect(barX + barW + 4f, y, numW, RowHeight),
                $"<color=#{colHex}><b>{morale}</b></color>", styleNormal);
        }

        private void DrawSeparator(float x, float y, float w)
        {
            Color old = GUI.color;
            GUI.color = SeparatorColor;
            GUI.DrawTexture(new Rect(x, y, w, 1f), Texture2D.whiteTexture);
            GUI.color = old;
        }

        // ── Style & utility ───────────────────────────────────────────────────────

        private void BuildStyles()
        {
            if (stylesBuilt) return;
            stylesBuilt = true;

            Texture2D bgTex     = MakeTex(BgColor);
            Texture2D headerTex = MakeTex(HeaderBg);

            styleBg = new GUIStyle(GUI.skin.box)
            {
                normal    = { background = bgTex },
                border    = new RectOffset(4, 4, 4, 4),
                padding   = new RectOffset(0, 0, 0, 0),
            };

            styleHeader = new GUIStyle(GUI.skin.box)
            {
                normal  = { background = headerTex },
                border  = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };

            styleNormal = new GUIStyle(GUI.skin.label)
            {
                fontSize  = FontSizeNormal,
                richText  = true,
                wordWrap  = false,
                alignment = TextAnchor.MiddleLeft,
            };

            styleBold = new GUIStyle(styleNormal)
            {
                fontStyle = FontStyle.Bold,
            };

            styleTitle = new GUIStyle(styleBold)
            {
                fontSize  = FontSizeHeader,
                normal    = { textColor = new Color(0.55f, 1f, 0.55f) },
                alignment = TextAnchor.MiddleLeft,
            };
        }

        private void ResolveManager()
        {
            if (survivorManager == null)
                survivorManager = FindFirstObjectByType<SurvivorManager>();
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private static float EstimateContentHeight(int count)
        {
            // ~15 rows per survivor block × RowHeight + separators
            return count * (15 * RowHeight + 32f) + 16f;
        }

        // ── Trait descriptions ────────────────────────────────────────────────────

        private static string GetPositiveTraitDescription(PositiveTrait t) => t switch
        {
            PositiveTrait.Travailleur => "Productivité +",
            PositiveTrait.Calme       => "Stress monte lentement",
            PositiveTrait.Courageux   => "Accepte ordres risqués",
            PositiveTrait.Genereux    => "Réduit stress voisins",
            PositiveTrait.Loyal       => "Rarement en refus",
            PositiveTrait.Createur    => "Bonus Technique craft",
            PositiveTrait.Empathique  => "Réduit stress du groupe",
            PositiveTrait.Strategique => "Bonus Intel missions",
            _                         => "—",
        };

        private static string GetNegativeTraitDescription(NegativeTrait t) => t switch
        {
            NegativeTrait.Peureux    => "Refuse missions risquées",
            NegativeTrait.Belliqueux => "Augmente stress voisins",
            NegativeTrait.Egoiste    => "Consomme plus de nourriture",
            NegativeTrait.Paresseux  => "Fatigue monte plus vite",
            NegativeTrait.Impulsif   => "Incidents aléatoires",
            NegativeTrait.Pessimiste => "Moral descend plus vite",
            NegativeTrait.Kleptomann => "Vole des ressources",
            NegativeTrait.Fragile    => "Tombe malade facilement",
            _                        => "—",
        };

#endif
    }
}
