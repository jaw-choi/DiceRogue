using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Dice Loadout", fileName = "DiceLoadout")]
    public class DiceLoadoutDefinition : ScriptableObject
    {
        public const int FaceCount = 6;

        [SerializeField] private string id = "dice_loadout";
        [SerializeField] private string displayName = "Dice Loadout";
        [SerializeField] private DiceBuildIdentity identity = DiceBuildIdentity.Balanced;
        [SerializeField] private List<SkillDefinition> faces = new List<SkillDefinition>(FaceCount);

        public string Id => id;
        public string DisplayName => displayName;
        public DiceBuildIdentity Identity => identity;
        public IReadOnlyList<SkillDefinition> Faces => faces;

        public void Configure(string loadoutId, string loadoutDisplayName, DiceBuildIdentity buildIdentity, IEnumerable<SkillDefinition> skills)
        {
            id = string.IsNullOrWhiteSpace(loadoutId) ? "dice_loadout" : loadoutId;
            displayName = string.IsNullOrWhiteSpace(loadoutDisplayName) ? "Dice Loadout" : loadoutDisplayName;
            identity = buildIdentity;
            faces = Normalize(skills);
        }

        private void OnValidate()
        {
            faces = Normalize(faces);
        }

        public static List<SkillDefinition> Normalize(IEnumerable<SkillDefinition> skills)
        {
            var normalized = skills?.Take(FaceCount).ToList() ?? new List<SkillDefinition>(FaceCount);
            while (normalized.Count < FaceCount)
            {
                normalized.Add(null);
            }

            return normalized;
        }
    }
}
