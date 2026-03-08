using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Flashlight FPS attachée à la caméra du joueur.
    /// Toggle ON/OFF avec la touche F.
    /// Fonctionne indépendamment du système électrique du shelter.
    ///
    /// Setup :
    ///   1. Ajoute ce composant au GameObject Player ou caméra FPS.
    ///   2. Crée un GameObject enfant de la caméra nommé "FlashlightRoot",
    ///      positionné à (0, 0, 0.15) — légèrement devant la caméra.
    ///   3. Assigne ce Transform dans le champ FlashlightRoot.
    ///      Si aucun Light enfant n'est trouvé, un Spot est créé automatiquement.
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform flashlightRoot;

        [Header("Light Settings")]
        [SerializeField] private float intensity      = 8f;
        [SerializeField] private float range          = 20f;
        [SerializeField] private float spotAngle      = 60f;
        [SerializeField] private float innerSpotAngle = 30f;
        [SerializeField] private Color lightColor     = new Color(1f, 0.97f, 0.88f);
        [SerializeField] private bool  startEnabled   = false;

        [Header("Shadow Settings")]
        [SerializeField] private bool  enableShadows  = false;   // OFF par défaut : évite le clipping près des murs
        [SerializeField] private float shadowStrength  = 1f;
        [SerializeField] private float shadowNearPlane = 0.01f;

        [Header("Battery (optional)")]
        [SerializeField] private bool  useBattery     = false;
        [SerializeField] private float batterySeconds = 120f;

        private Light  flashlight;
        private bool   isOn;
        private float  batteryRemaining;

        public bool  IsOn           => isOn;
        public float BatteryPercent => useBattery ? batteryRemaining / batterySeconds : 1f;

        private void Awake()
        {
            flashlight = ResolveLight();
            ApplyLightSettings();
            batteryRemaining = batterySeconds;
            SetFlashlight(startEnabled);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                Toggle();

            if (isOn && useBattery)
                DrainBattery();
        }

        /// <summary>Applique les paramètres Inspector à la Light.
        /// Appelé en Awake et à chaque modification dans l'Inspector (OnValidate).</summary>
        private void ApplyLightSettings()
        {
            if (flashlight == null) return;
            flashlight.type            = LightType.Spot;
            flashlight.intensity       = intensity;
            flashlight.range           = range;
            flashlight.spotAngle       = spotAngle;
            flashlight.innerSpotAngle  = innerSpotAngle;
            flashlight.color           = lightColor;
            flashlight.shadows         = enableShadows ? LightShadows.Hard : LightShadows.None;
            flashlight.shadowStrength  = shadowStrength;
            flashlight.shadowNearPlane = shadowNearPlane;
            flashlight.renderMode      = LightRenderMode.ForcePixel;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (flashlight != null) ApplyLightSettings();
        }
#endif

        /// <summary>Trouve ou crée le composant Light sur flashlightRoot.</summary>
        private Light ResolveLight()
        {
            Transform root = flashlightRoot != null ? flashlightRoot : transform;
            Light found = root.GetComponentInChildren<Light>(includeInactive: true);
            if (found != null) return found;

            GameObject go = new GameObject("FlashlightPoint");
            go.transform.SetParent(root, false);
            // Légèrement devant la caméra — évite que la géométrie proche clip l'illumination
            go.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            return go.AddComponent<Light>();
        }

        /// <summary>Bascule la lampe ON/OFF.</summary>
        public void Toggle()
        {
            if (!isOn && useBattery && batteryRemaining <= 0f)
            {
                Debug.Log("[PlayerFlashlight] Batterie vide.");
                return;
            }
            SetFlashlight(!isOn);
        }

        private void SetFlashlight(bool on)
        {
            isOn               = on;
            flashlight.enabled = on;
        }

        private void DrainBattery()
        {
            batteryRemaining -= Time.deltaTime;
            if (batteryRemaining <= 0f)
            {
                batteryRemaining = 0f;
                SetFlashlight(false);
                Debug.Log("[PlayerFlashlight] Batterie épuisée.");
            }
        }
    }
}
