using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Attach to any GameObject with a Camera to register it as a security feed.
    /// The label (CAM-01, CAM-02…) is auto-assigned when the terminal discovers cameras —
    /// no configuration needed in the Inspector.
    /// If the Camera already has a RenderTexture set in the prefab, it is reused.
    /// Otherwise one is created automatically at runtime.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SecurityCamera : MonoBehaviour
    {
        [Header("Render Texture (optional — auto-created if not assigned)")]
        [SerializeField] private int autoTextureWidth  = 512;
        [SerializeField] private int autoTextureHeight = 288;

        /// <summary>Assigned automatically at discovery time — e.g. "CAM-01".</summary>
        public string        CameraLabel   { get; set; } = "CAM-XX";
        public RenderTexture RenderTexture { get; private set; }

        private Camera cam;
        private bool   createdOwnRT;

        private void Awake()
        {
            cam = GetComponent<Camera>();

            if (cam.targetTexture != null)
            {
                RenderTexture = cam.targetTexture;
                createdOwnRT  = false;
            }
            else
            {
                RenderTexture     = new RenderTexture(autoTextureWidth, autoTextureHeight, 24);
                cam.targetTexture = RenderTexture;
                createdOwnRT      = true;
            }
        }

        private void OnDestroy()
        {
            if (createdOwnRT && RenderTexture != null)
            {
                cam.targetTexture = null;
                RenderTexture.Release();
            }
        }
    }
}
