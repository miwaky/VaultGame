using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Attach to any GameObject containing a Light component.
    /// The light switches on/off with the shelter's ElectricitySystem.
    ///
    /// baseLightIntensity is the intensity used at full power (Power = 1).
    /// Set flickerOnRestore to true for a brief flicker effect when power comes back.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PoweredLight : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float baseLightIntensity = 1f;
        [SerializeField] private bool  flickerOnRestore   = true;

        [Header("Flicker")]
        [SerializeField] private float flickerDuration     = 0.6f;
        [SerializeField] private float flickerIntervalMin  = 0.04f;
        [SerializeField] private float flickerIntervalMax  = 0.15f;

        private Light       managedLight;
        private bool        isFlickering;
        private float       flickerTimer;
        private float       flickerEndTime;
        private float       nextFlickerToggle;

        private bool initialized;

        private void Awake()
        {
            managedLight = GetComponent<Light>();
            managedLight.enabled = false; // éteint par défaut, sera allumé par Start()
        }

        private void OnEnable()
        {
            if (ElectricitySystem.Instance != null)
                ElectricitySystem.Instance.OnPowerChanged += ApplyPower;
        }

        private void OnDisable()
        {
            if (ElectricitySystem.Instance != null)
                ElectricitySystem.Instance.OnPowerChanged -= ApplyPower;
        }

        private void Start()
        {
            // Sync direct sans flicker au démarrage, quelle que soit l'ordre d'init
            if (ElectricitySystem.Instance != null)
                SetLightEnabled(ElectricitySystem.Instance.Power > 0f);
            initialized = true;
        }

        private void Update()
        {
            if (!isFlickering) return;

            if (Time.time >= flickerEndTime)
            {
                isFlickering = false;
                managedLight.enabled   = true;
                managedLight.intensity = baseLightIntensity;
                return;
            }

            if (Time.time >= nextFlickerToggle)
            {
                managedLight.enabled = !managedLight.enabled;
                nextFlickerToggle    = Time.time + Random.Range(flickerIntervalMin, flickerIntervalMax);
            }
        }

        private void ApplyPower(float power)
        {
            if (power > 0f)
            {
                // Flicker uniquement après le démarrage, pas lors de l'init initiale
                if (flickerOnRestore && initialized)
                    StartFlicker();
                else
                    SetLightEnabled(true);
            }
            else
            {
                isFlickering = false;
                SetLightEnabled(false);
            }
        }

        private void SetLightEnabled(bool on)
        {
            managedLight.enabled   = on;
            managedLight.intensity = on ? baseLightIntensity : 0f;
        }

        private void StartFlicker()
        {
            isFlickering     = true;
            flickerEndTime   = Time.time + flickerDuration;
            nextFlickerToggle = Time.time;
        }
    }
}
