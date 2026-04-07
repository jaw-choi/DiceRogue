#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DiceRogue
{
    public static class RunSeedDataCreator
    {
        private const string GeneratedRoot = "Assets/Game/Data/Generated";
        private const string SkillsRoot = GeneratedRoot + "/Skills";
        private const string CombatantsRoot = GeneratedRoot + "/Combatants";
        private const string ResourcesRoot = "Assets/Resources/DiceRogue";
        private const string ConfigPath = ResourcesRoot + "/RunConfig.asset";

        [MenuItem("Tools/DiceRogue/Generate Run Seed Data")]
        public static void Generate()
        {
            EnsureFolders();
            CleanupExistingAssets();
            AssetDatabase.Refresh();

            var config = RunContentFactory.CreateFallbackConfig();

            foreach (var skill in config.SkillLibrary.Where(skill => skill != null).Distinct())
            {
                AssetDatabase.CreateAsset(skill, $"{SkillsRoot}/{Sanitize(skill.DisplayName)}.asset");
            }

            foreach (var combatant in config.NormalEnemies
                         .Append(config.BossEnemy)
                         .Append(config.PlayerTemplate)
                         .Where(template => template != null)
                         .Distinct())
            {
                AssetDatabase.CreateAsset(combatant, $"{CombatantsRoot}/{Sanitize(combatant.DisplayName)}.asset");
            }

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Debug.Log("DiceRogue: Run seed data generated.");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Game/Data/Generated/Skills"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Game/Data/Generated/Combatants"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/DiceRogue"));
        }

        private static void CleanupExistingAssets()
        {
            if (AssetDatabase.IsValidFolder(GeneratedRoot))
            {
                AssetDatabase.DeleteAsset(GeneratedRoot);
            }

            if (AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath) != null)
            {
                AssetDatabase.DeleteAsset(ConfigPath);
            }

            AssetDatabase.Refresh();
            EnsureFolders();
            AssetDatabase.Refresh();
        }

        private static string Sanitize(string input)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            return new string(input.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        }
    }
}
#endif
