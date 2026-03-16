using UnityEngine;
using UnityEditor;
using ShelterCommand;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Restructures the CameraSurveillance prefab to match the target hierarchy:
    ///
    ///   CameraSurveillance  (CameraRoot — SecurityCameraController)
    ///     CameraPivot
    ///       Camera          (Camera + SecurityCamera)
    ///     CamMarker         (visual mesh — stays on root)
    ///     Bloqueur          (collision blocker — stays on root)
    ///
    /// Menu: Window → ShelterCommand → Restructure CameraSurveillance Prefab
    /// </summary>
    internal static class CameraSurveillancePrefabRebuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Camera/CameraSurveillance.prefab";

        [MenuItem("Window/ShelterCommand/Restructure CameraSurveillance Prefab")]
        public static void Restructure()
        {
            // ── Load prefab ─────────────────────────────────────────────────────────
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[CameraSurveillancePrefabRebuilder] Prefab introuvable : {PrefabPath}");
                return;
            }

            // Open in edit mode for safe structural changes
            using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;

                // ── Detect current state ─────────────────────────────────────────────
                Camera rootCamera       = root.GetComponent<Camera>();
                SecurityCamera rootSecCam  = root.GetComponent<SecurityCamera>();
                SecurityCameraController rootCtrl = root.GetComponent<SecurityCameraController>();

                // Find or create CameraPivot
                Transform pivot = root.transform.Find("CameraPivot");
                if (pivot == null)
                {
                    GameObject pivotGo = new GameObject("CameraPivot");
                    pivotGo.transform.SetParent(root.transform, false);
                    pivotGo.transform.SetSiblingIndex(0);
                    pivot = pivotGo.transform;
                }

                // Find or create Camera child under CameraPivot
                Transform cameraChild = pivot.Find("Camera");
                if (cameraChild == null)
                {
                    GameObject cameraGo = new GameObject("Camera");
                    cameraGo.transform.SetParent(pivot, false);
                    cameraChild = cameraGo.transform;
                }

                // Copy Camera component to child if root still has it
                if (rootCamera != null)
                {
                    Camera newCam = cameraChild.GetComponent<Camera>();
                    if (newCam == null) newCam = cameraChild.gameObject.AddComponent<Camera>();
                    EditorUtility.CopySerialized(rootCamera, newCam);
                }

                // Copy SecurityCamera component to child if root still has it
                if (rootSecCam != null)
                {
                    SecurityCamera newSecCam = cameraChild.GetComponent<SecurityCamera>();
                    if (newSecCam == null) newSecCam = cameraChild.gameObject.AddComponent<SecurityCamera>();
                    EditorUtility.CopySerialized(rootSecCam, newSecCam);
                }

                // ── Remove SecurityCamera FIRST (it has RequireComponent on Camera) ──
                // Then remove Camera. Order matters.
                if (rootSecCam != null)
                    Object.DestroyImmediate(rootSecCam);
                if (rootCamera != null)
                    Object.DestroyImmediate(rootCamera);

                // ── Add SecurityCameraController to root if not already there ────────
                SecurityCameraController ctrl = rootCtrl ?? root.AddComponent<SecurityCameraController>();

                // Wire pivot reference
                SerializedObject so = new SerializedObject(ctrl);
                SerializedProperty pivotProp = so.FindProperty("cameraPivot");
                if (pivotProp != null)
                {
                    pivotProp.objectReferenceValue = pivot;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning("[CameraSurveillancePrefabRebuilder] Propriété 'cameraPivot' introuvable — assigne-la manuellement dans l'Inspector.");
                }

                Debug.Log("[CameraSurveillancePrefabRebuilder] ✔ Prefab restructuré avec succès :\n" +
                          "  CameraSurveillance (SecurityCameraController)\n" +
                          "    CameraPivot\n" +
                          "      Camera (Camera + SecurityCamera)\n" +
                          "    CamMarker\n" +
                          "    Bloqueur");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
