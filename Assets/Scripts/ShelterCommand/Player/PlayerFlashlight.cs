using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Torche FPS. Pose ce composant directement sur le GameObject Camera du joueur.
    /// Appuie sur F pour allumer / éteindre.
    /// Pas d'ombres : évite les conflits avec les shaders PSX et le clipping près des murs.
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        [Header("Spot Light")]
        [SerializeField] private float intensity  = 5f;
        [SerializeField] private float range      = 15f;
        [SerializeField] private float spotAngle  = 55f;
        [SerializeField] private float innerAngle = 25f;
        [SerializeField] private Color lightColor = new Color(1f, 0.96f, 0.84f);

        [Header("État initial")]
        [SerializeField] private bool startEnabled = false;

        private Light spotLight;

        private void Awake()
        {
            spotLight = CreateSpotLight();
            spotLight.enabled = startEnabled;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                spotLight.enabled = !spotLight.enabled;
        }

        /// <summary>Crée le Spot Light comme enfant direct de ce GameObject (la caméra).</summary>
        private Light CreateSpotLight()
        {
            GameObject go = new GameObject("FlashlightSpot");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            Light l = go.AddComponent<Light>();
            l.type            = LightType.Spot;
            l.intensity       = intensity;
            l.range           = range;
            l.spotAngle       = spotAngle;
            l.innerSpotAngle  = innerAngle;
            l.color           = lightColor;

            // Pas d'ombres — évite tout clipping près des murs et les conflits avec les shaders PSX
            l.shadows    = LightShadows.None;

            // ForcePixel : calcul per-pixel garanti, fonctionne à 30cm comme à 10m
            l.renderMode = LightRenderMode.ForcePixel;

            return l;
        }

#if UNITY_EDITOR
        /// <summary>Applique les modifications Inspector immédiatement en Play Mode.</summary>
        private void OnValidate()
        {
            if (spotLight == null) return;
            spotLight.intensity      = intensity;
            spotLight.range          = range;
            spotLight.spotAngle      = spotAngle;
            spotLight.innerSpotAngle = innerAngle;
            spotLight.color          = lightColor;
        }
#endif
    }
}
