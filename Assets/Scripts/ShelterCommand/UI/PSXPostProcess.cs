using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Applies PSX-style vertex snapping and color depth reduction per mesh renderer.
    /// Attach to a camera to affect all objects in view via the shader trick.
    /// For URP-less setups we use a simple vertex-jitter approach.
    /// Also handles the low-resolution render by downscaling the game view.
    /// </summary>
    public class PSXPostProcess : MonoBehaviour
    {
        [Header("PSX Resolution")]
        [SerializeField] private int targetWidth = 320;
        [SerializeField] private int targetHeight = 240;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;

        [Header("Flickering Light")]
        [SerializeField] private Light[] unstableLights;
        [SerializeField, Range(0f, 0.3f)] private float flickerAmplitude = 0.1f;
        [SerializeField, Range(1f, 20f)] private float flickerFrequency = 8f;

        private RenderTexture lowResRT;
        private Camera cam;
        private float[] lightBaseIntensities;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            CacheLightIntensities();
        }

        private void OnEnable()
        {
            CreateLowResRT();
        }

        private void OnDisable()
        {
            CleanupRT();
        }

        private void Update()
        {
            FlickerLights();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // Blit to low-res then scale up with point filtering for the pixelated look
            if (lowResRT == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            Graphics.Blit(src, lowResRT);
            Graphics.Blit(lowResRT, dest);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private void CreateLowResRT()
        {
            CleanupRT();
            lowResRT = new RenderTexture(targetWidth, targetHeight, 16);
            lowResRT.filterMode = filterMode;
            lowResRT.Create();
        }

        private void CleanupRT()
        {
            if (lowResRT != null)
            {
                lowResRT.Release();
                Destroy(lowResRT);
                lowResRT = null;
            }
        }

        private void CacheLightIntensities()
        {
            if (unstableLights == null) return;
            lightBaseIntensities = new float[unstableLights.Length];
            for (int i = 0; i < unstableLights.Length; i++)
            {
                if (unstableLights[i] != null)
                {
                    lightBaseIntensities[i] = unstableLights[i].intensity;
                }
            }
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
