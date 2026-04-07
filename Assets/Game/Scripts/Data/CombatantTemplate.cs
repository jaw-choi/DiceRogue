using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Combatant Template", fileName = "CombatantTemplate")]
    public class CombatantTemplate : ScriptableObject
    {
        [SerializeField] private string id = "combatant_id";
        [SerializeField] private string displayName = "Hero";
        [SerializeField] private int maxHp = 30;
        [SerializeField] private int startingArmor;
        [SerializeField] private int startingRage;
        [SerializeField] private bool isBoss;
        [SerializeField] private List<SkillDefinition> diceSkills = new List<SkillDefinition>(6);

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public int StartingArmor => startingArmor;
        public int StartingRage => startingRage;
        public bool IsBoss => isBoss;
        public IReadOnlyList<SkillDefinition> DiceSkills => diceSkills;
    }
}
