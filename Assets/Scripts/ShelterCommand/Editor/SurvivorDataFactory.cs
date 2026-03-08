using UnityEngine;
using UnityEditor;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Creates the 10 default SurvivorData ScriptableObject assets for the prototype.
    /// Menu: Tools > Shelter Command > Create Default Survivor Data
    /// </summary>
    public static class SurvivorDataFactory
    {
        private static readonly SurvivorProfile[] Profiles = new[]
        {
            new SurvivorProfile("Aria",   32, str:70, intel:60, tech:50, loy:80, end:65),
            new SurvivorProfile("Borek",  45, str:85, intel:40, tech:55, loy:70, end:80),
            new SurvivorProfile("Chloé",  28, str:40, intel:90, tech:70, loy:85, end:50),
            new SurvivorProfile("Daan",   38, str:60, intel:55, tech:90, loy:75, end:60),
            new SurvivorProfile("Elsa",   24, str:45, intel:80, tech:60, loy:90, end:55),
            new SurvivorProfile("Farid",  50, str:75, intel:65, tech:45, loy:60, end:85),
            new SurvivorProfile("Gwen",   35, str:55, intel:75, tech:65, loy:70, end:65),
            new SurvivorProfile("Henk",   42, str:80, intel:45, tech:70, loy:65, end:75),
            new SurvivorProfile("Iris",   29, str:50, intel:85, tech:55, loy:80, end:60),
            new SurvivorProfile("Joël",   33, str:65, intel:70, tech:80, loy:75, end:70),
        };

        [MenuItem("Tools/Shelter Command/Create Default Survivor Data")]
        public static void CreateAllSurvivorData()
        {
            string folder = "Assets/Scripts/ShelterCommand";
            string dataFolder = folder + "/Data/SurvivorAssets";

            if (!AssetDatabase.IsValidFolder(dataFolder))
            {
                AssetDatabase.CreateFolder(folder + "/Data", "SurvivorAssets");
            }

            foreach (SurvivorProfile profile in Profiles)
            {
                string path = $"{dataFolder}/{profile.Name}.asset";

                SurvivorData existing = AssetDatabase.LoadAssetAtPath<SurvivorData>(path);
                if (existing != null)
                {
                    Debug.Log($"[SurvivorDataFactory] {profile.Name}.asset already exists — skipping.");
                    continue;
                }

                SurvivorData data = ScriptableObject.CreateInstance<SurvivorData>();
                data.survivorName = profile.Name;
                data.age = profile.Age;
                data.strength = profile.Strength;
                data.intelligence = profile.Intelligence;
                data.technical = profile.Technical;
                data.loyalty = profile.Loyalty;
                data.endurance = profile.Endurance;

                AssetDatabase.CreateAsset(data, path);
                Debug.Log($"[SurvivorDataFactory] Created {profile.Name}.asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SurvivorDataFactory] All 10 SurvivorData assets created.");
        }

        private readonly struct SurvivorProfile
        {
            public readonly string Name;
            public readonly int Age;
            public readonly int Strength;
            public readonly int Intelligence;
            public readonly int Technical;
            public readonly int Loyalty;
            public readonly int Endurance;

            public SurvivorProfile(string name, int age,
                int str, int intel, int tech, int loy, int end)
            {
                Name = name; Age = age; Strength = str; Intelligence = intel;
                Technical = tech; Loyalty = loy; Endurance = end;
            }
        }
    }
}
