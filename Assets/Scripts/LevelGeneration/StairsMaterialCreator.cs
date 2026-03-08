using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class StairsMaterialCreator : MonoBehaviour
{
    [MenuItem("Tools/Level Generator/Create Stairs Material")]
    public static void CreateStairsMaterial()
    {
        string materialPath = "Assets/Materials/Stairs_Material.mat";
        
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMaterial != null)
        {
            Debug.Log("Le matériau Stairs_Material existe déjà !");
            EditorGUIUtility.PingObject(existingMaterial);
            return;
        }
        
        Material stairsMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        stairsMaterial.name = "Stairs_Material";
        
        stairsMaterial.color = new Color(0.565f, 0.38f, 0.235f, 1f);
        stairsMaterial.SetFloat("_Smoothness", 0.2f);
        stairsMaterial.SetFloat("_Metallic", 0f);
        
        AssetDatabase.CreateAsset(stairsMaterial, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✓ Matériau créé : {materialPath}");
        EditorGUIUtility.PingObject(stairsMaterial);
    }
}
#endif
