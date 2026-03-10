using UnityEngine;
using UnityEngine.Rendering;

namespace ShelterCommand
{
    /// <summary>
    /// Configure l'éclairage ambiant de la scène pour obtenir une obscurité profonde.
    /// Pose ce composant sur n'importe quel GameObject persistant (ex: GameManager).
    ///
    /// Ces valeurs surchargent l'ambient color définie dans Window → Rendering → Lighting.
    /// </summary>
    public class AmbientSettings : MonoBehaviour
    {
        [Header("Lumière ambiante")]
        [SerializeField] private AmbientMode ambientMode       = AmbientMode.Flat;
        [SerializeField] private Color       ambientColor      = new Color(0.01f, 0.01f, 0.02f); // quasi noir, légère teinte bleue froide
        [SerializeField] private float       ambientIntensity  = 0f;  // 0 = aucune contribution du skybox

        [Header("Brume (optionnel)")]
        [SerializeField] private bool  enableFog    = true;
        [SerializeField] private Color fogColor     = new Color(0.02f, 0.02f, 0.04f);
        [SerializeField] private float fogStartDist = 8f;
        [SerializeField] private float fogEndDist   = 25f;

        private void Awake()
        {
            ApplySettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplySettings();
        }
#endif

        /// <summary>Applique les réglages d'ambiance à la scène courante.</summary>
        private void ApplySettings()
        {
            RenderSettings.ambientMode      = ambientMode;
            RenderSettings.ambientLight     = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;

            RenderSettings.fog        = enableFog;
            RenderSettings.fogColor   = fogColor;
            RenderSettings.fogMode    = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStartDist;
            RenderSettings.fogEndDistance   = fogEndDist;
        }
    }
}
