using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Skill Database", fileName = "SkillDatabase")]
    public class SkillDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillDefinition> playerPrototypeSkills = new List<SkillDefinition>();
        [SerializeField] private List<SkillDefinition> enemyPrototypeSkills = new List<SkillDefinition>();

        public IReadOnlyList<SkillDefinition> PlayerPrototypeSkills => playerPrototypeSkills;
        public IReadOnlyList<SkillDefinition> EnemyPrototypeSkills => enemyPrototypeSkills;
        public IReadOnlyList<SkillDefinition> AllSkills => playerPrototypeSkills.Concat(enemyPrototypeSkills).Distinct().ToList();

        public void Configure(IEnumerable<SkillDefinition> playerSkills, IEnumerable<SkillDefinition> enemySkills)
        {
            playerPrototypeSkills = playerSkills?.Where(skill => skill != null).Distinct().ToList() ?? new List<SkillDefinition>();
            enemyPrototypeSkills = enemySkills?.Where(skill => skill != null).Distinct().ToList() ?? new List<SkillDefinition>();
        }
    }
}
