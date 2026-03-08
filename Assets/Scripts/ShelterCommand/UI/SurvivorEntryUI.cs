using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// UI entry in the survivor dossier list. Binds to a single SurvivorBehavior.
    /// </summary>
    public class SurvivorEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image statusIcon;
        [SerializeField] private Color aliveColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color deadColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color sickColor = new Color(0.9f, 0.7f, 0.1f);

        private SurvivorBehavior boundSurvivor;

        /// <summary>Binds this entry to a survivor and refreshes display.</summary>
        public void Bind(SurvivorBehavior survivor)
        {
            boundSurvivor = survivor;
            Refresh();
            survivor.OnNeedsChanged += OnNeedsChanged;
            survivor.OnSurvivorDied += OnDied;
        }

        private void OnDestroy()
        {
            if (boundSurvivor != null)
            {
                boundSurvivor.OnNeedsChanged -= OnNeedsChanged;
                boundSurvivor.OnSurvivorDied -= OnDied;
            }
        }

        private void OnNeedsChanged(SurvivorBehavior _) => Refresh();
        private void OnDied(SurvivorBehavior _) => Refresh();

        private void Refresh()
        {
            if (boundSurvivor == null) return;

            if (nameText != null)
                nameText.text = boundSurvivor.SurvivorName.ToUpper();

            if (statsText != null)
            {
                SurvivorData d = boundSurvivor.Data;
                if (d != null)
                {
                    statsText.text = $"FOR:{d.strength} INT:{d.intelligence} TEC:{d.technical} " +
                                     $"LOY:{d.loyalty} END:{d.endurance}";
                }
                statsText.text += $"\nFaim:{boundSurvivor.Hunger} Fatigue:{boundSurvivor.Fatigue} " +
                                  $"Stress:{boundSurvivor.Stress} Moral:{boundSurvivor.Morale}";
            }

            if (statusText != null)
            {
                if (!boundSurvivor.IsAlive)
                    statusText.text = "MORT";
                else if (boundSurvivor.IsArrested)
                    statusText.text = "ARRÊTÉ";
                else if (boundSurvivor.IsSick)
                    statusText.text = "MALADE";
                else
                    statusText.text = "OK";
            }

            if (statusIcon != null)
            {
                statusIcon.color = !boundSurvivor.IsAlive ? deadColor
                    : boundSurvivor.IsSick ? sickColor
                    : aliveColor;
            }
        }
    }
}
