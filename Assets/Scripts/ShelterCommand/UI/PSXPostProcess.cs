using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Applies PSX-style downscaling via a low-resolution RenderTexture.
    /// PixelScale controls the intensity of the effect:
    ///   1.0 = full PSX (320×240), 0.0 = native resolution (no pixelation).
    /// The slider can be changed at runtime and the RT is rebuilt automatically.
    /// </summary>
    public class PSXPostProcess : MonoBehaviour
    {
        [Header("PSX Resolution")]
        [Tooltip("Base resolution width at full PSX effect (pixelScale = 1).")]
        [SerializeField] private int baseWidth  = 320;
        [Tooltip("Base resolution height at full PSX effect (pixelScale = 1).")]
        [SerializeField] private int baseHeight = 240;
        [Tooltip("0 = native resolution (no pixelation)  |  1 = full PSX effect.")]
        [SerializeField, Range(0f, 1f)] private float pixelScale = 1f;

        [Header("Flickering Light")]
        [SerializeField] private Light[] unstableLights;
        [SerializeField, Range(0f, 0.3f)] private float flickerAmplitude = 0.1f;
        [SerializeField, Range(1f, 20f)]  private float flickerFrequency = 8f;

        // ── Private ───────────────────────────────────────────────────────────────

        private RenderTexture lowResRT;
        private Camera        cam;
        private float[]       lightBaseIntensities;
        private float         lastPixelScale = -1f;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            cam = GetComponent<Camera>();
            CacheLightIntensities();
        }

        private void OnEnable()  => CreateLowResRT();
        private void OnDisable() => CleanupRT();

        private void Update()
        {
            FlickerLights();

            // Rebuild RT when the slider changes at runtime
            if (!Mathf.Approximately(pixelScale, lastPixelScale))
                CreateLowResRT();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // When pixelScale is effectively 0, skip the downscale entirely
            if (lowResRT == null || pixelScale < 0.01f)
            {
                Graphics.Blit(src, dest);
                return;
            }

            Graphics.Blit(src, lowResRT);
            Graphics.Blit(lowResRT, dest);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void CreateLowResRT()
        {
            lastPixelScale = pixelScale;
            CleanupRT();

            if (pixelScale < 0.01f) return;     // no pixelation needed

            // Lerp between native screen resolution (full quality) and base PSX size
            int w = Mathf.RoundToInt(Mathf.Lerp(Screen.width,  baseWidth,  pixelScale));
            int h = Mathf.RoundToInt(Mathf.Lerp(Screen.height, baseHeight, pixelScale));
            w = Mathf.Max(w, 1);
            h = Mathf.Max(h, 1);

            lowResRT            = new RenderTexture(w, h, 16);
            lowResRT.filterMode = FilterMode.Point;
            lowResRT.Create();
        }

        private void CleanupRT()
        {
            if (lowResRT == null) return;
            lowResRT.Release();
            Destroy(lowResRT);
            lowResRT = null;
        }

        private void CacheLightIntensities()
        {
            if (unstableLights == null) return;
            lightBaseIntensities = new float[unstableLights.Length];
            for (int i = 0; i < unstableLights.Length; i++)
                if (unstableLights[i] != null)
                    lightBaseIntensities[i] = unstableLights[i].intensity;
        }

        private void FlickerLights()
        {
            if (unstableLights == null || lightBaseIntensities == null) return;
            for (int i = 0; i < unstableLights.Length; i++)
            {
                if (unstableLights[i] == null) continue;
                float noise = Mathf.PerlinNoise(Time.time * flickerFrequency + i * 73.1f, 0f);
                unstableLights[i].intensity = lightBaseIntensities[i] + (noise - 0.5f) * 2f * flickerAmplitude;
            }
        }
    }
}
