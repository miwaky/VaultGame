using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class MaterialCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Default Level Materials")]
    public static void CreateDefaultMaterials()
    {
        string materialsPath = "Assets/Materials";
        
        if (!AssetDatabase.IsValidFolder(materialsPath))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        CreateCeilingShader();
        
        CreateMaterial(materialsPath + "/Wall_Material.mat", new Color(0.7f, 0.7f, 0.7f), false);
        CreateMaterial(materialsPath + "/Floor_Material.mat", new Color(0.3f, 0.3f, 0.35f), false);
        CreateMaterial(materialsPath + "/Ceiling_Material.mat", new Color(0.9f, 0.9f, 0.9f), true);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Matériaux créés dans /Assets/Materials/");
        Debug.Log("- Wall_Material.mat (Gris)");
        Debug.Log("- Floor_Material.mat (Gris foncé)");
        Debug.Log("- Ceiling_Material.mat (Blanc cassé - transparent du dessus)");
    }
    
    private static void CreateMaterial(string path, Color color, bool isCeiling)
    {
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(path);
        
        Shader shader;
        if (isCeiling)
        {
            shader = Shader.Find("Custom/TransparentCeiling");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                Debug.LogWarning("Shader Custom/TransparentCeiling introuvable, utilisation du shader Lit standard");
            }
        }
        else
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        
        if (existingMat != null)
        {
            existingMat.shader = shader;
            existingMat.color = color;
            EditorUtility.SetDirty(existingMat);
            Debug.Log($"Matériau mis à jour : {path}");
        }
        else
        {
            Material mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"Matériau créé : {path}");
        }
    }
    
    private static void CreateCeilingShader()
    {
        string shaderPath = "Assets/Materials/TransparentCeiling.shader";
        
        if (AssetDatabase.LoadAssetAtPath<Shader>(shaderPath) != null)
        {
            return;
        }
        
        string shaderCode = "Shader \"Custom/TransparentCeiling\"\n{\n    Properties\n    {\n        _Color (\"Color\", Color) = (1,1,1,1)\n        _MainTex (\"Albedo (RGB)\", 2D) = \"white\" {}\n        _Glossiness (\"Smoothness\", Range(0,1)) = 0.5\n        _Metallic (\"Metallic\", Range(0,1)) = 0.0\n    }\n    SubShader\n    {\n        Tags { \"RenderType\"=\"Transparent\" \"Queue\"=\"Transparent\" \"RenderPipeline\"=\"UniversalPipeline\" }\n        LOD 200\n        \n        Cull Back\n        ZWrite Off\n        Blend SrcAlpha OneMinusSrcAlpha\n\n        Pass\n        {\n            Name \"ForwardLit\"\n            Tags { \"LightMode\"=\"UniversalForward\" }\n\n            HLSLPROGRAM\n            #pragma vertex vert\n            #pragma fragment frag\n            #pragma multi_compile_fog\n            \n            #include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n            #include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl\"\n\n            struct Attributes\n            {\n                float4 positionOS : POSITION;\n                float3 normalOS : NORMAL;\n                float2 uv : TEXCOORD0;\n            };\n\n            struct Varyings\n            {\n                float4 positionCS : SV_POSITION;\n                float3 normalWS : TEXCOORD0;\n                float3 viewDirWS : TEXCOORD1;\n                float2 uv : TEXCOORD2;\n                float fogFactor : TEXCOORD3;\n            };\n\n            TEXTURE2D(_MainTex);\n            SAMPLER(sampler_MainTex);\n\n            CBUFFER_START(UnityPerMaterial)\n                float4 _Color;\n                float4 _MainTex_ST;\n                float _Glossiness;\n                float _Metallic;\n            CBUFFER_END\n\n            Varyings vert(Attributes input)\n            {\n                Varyings output;\n                \n                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);\n                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);\n                \n                output.positionCS = vertexInput.positionCS;\n                output.normalWS = normalInput.normalWS;\n                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);\n                output.uv = TRANSFORM_TEX(input.uv, _MainTex);\n                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);\n                \n                return output;\n            }\n\n            half4 frag(Varyings input) : SV_Target\n            {\n                float3 normalWS = normalize(input.normalWS);\n                float3 viewDirWS = normalize(input.viewDirWS);\n                \n                float viewDot = dot(normalWS, viewDirWS);\n                float alpha = saturate(viewDot * 2.0);\n                \n                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);\n                half4 color = texColor * _Color;\n                \n                InputData inputData = (InputData)0;\n                inputData.normalWS = normalWS;\n                inputData.viewDirectionWS = viewDirWS;\n                inputData.shadowCoord = 0;\n                inputData.fogCoord = input.fogFactor;\n                inputData.vertexLighting = half3(0, 0, 0);\n                inputData.bakedGI = half3(0, 0, 0);\n                \n                SurfaceData surfaceData = (SurfaceData)0;\n                surfaceData.albedo = color.rgb;\n                surfaceData.metallic = _Metallic;\n                surfaceData.smoothness = _Glossiness;\n                surfaceData.normalTS = half3(0, 0, 1);\n                surfaceData.emission = 0;\n                surfaceData.occlusion = 1;\n                surfaceData.alpha = alpha;\n                \n                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);\n                finalColor.a = alpha;\n                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);\n                \n                return finalColor;\n            }\n            ENDHLSL\n        }\n    }\n    FallBack \"Universal Render Pipeline/Lit\"\n}\n";
        
        System.IO.File.WriteAllText(shaderPath, shaderCode);
        AssetDatabase.Refresh();
        Debug.Log("Shader Custom/TransparentCeiling créé");
    }
}
#endif

