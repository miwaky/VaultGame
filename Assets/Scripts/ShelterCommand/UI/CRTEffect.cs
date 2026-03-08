using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Post-processing CRT / PSX effect applied via OnRenderImage.
    /// Requires a material using the CRTScreen shader.
    /// Attach to the main camera (or a dedicated post-processing camera).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class CRTEffect : MonoBehaviour
    {
        [Header("CRT Material")]
        [SerializeField] private Material crtMaterial;

        [Header("CRT Parameters")]
        [SerializeField, Range(0f, 1f)] private float scanlineIntensity = 0.15f;
        [SerializeField, Range(0f, 0.05f)] private float vignetteStrength = 0.02f;
        [SerializeField, Range(0f, 0.01f)] private float noiseIntensity = 0.005f;
        [SerializeField, Range(0f, 0.005f)] private float analogShift = 0.001f;
        [SerializeField, Range(0f, 2f)] private float flickerSpeed = 1.2f;
        [SerializeField] private Color tintColor = new Color(0.9f, 1f, 0.85f, 1f);

        private static readonly int ScanlineIntensityID = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int VignetteStrengthID = Shader.PropertyToID("_VignetteStrength");
        private static readonly int NoiseIntensityID = Shader.PropertyToID("_NoiseIntensity");
        private static readonly int AnalogShiftID = Shader.PropertyToID("_AnalogShift");
        private static readonly int TimeID = Shader.PropertyToID("_CRTTime");
        private static readonly int TintColorID = Shader.PropertyToID("_TintColor");

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (crtMaterial == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed * 13.7f) * 0.008f;

            crtMaterial.SetFloat(ScanlineIntensityID, scanlineIntensity);
            crtMaterial.SetFloat(VignetteStrengthID, vignetteStrength);
            crtMaterial.SetFloat(NoiseIntensityID, noiseIntensity * flicker);
            crtMaterial.SetFloat(AnalogShiftID, analogShift * flicker);
            crtMaterial.SetFloat(TimeID, Time.time);
            crtMaterial.SetColor(TintColorID, tintColor);

            Graphics.Blit(src, dest, crtMaterial);
        }
    }
}
