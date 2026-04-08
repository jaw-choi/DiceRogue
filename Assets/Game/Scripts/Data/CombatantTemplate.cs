using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Combatant Template", fileName = "CombatantTemplate")]
    public class CombatantTemplate : ScriptableObject
    {
        [SerializeField] private string id = "combatant_id";
        [SerializeField] private string displayName = "Hero";
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int baseDicePoints = 1;
        [SerializeField] private int passiveShieldPerTurn;
        [SerializeField] private int passiveReflectPercent;
        [SerializeField] private bool isBoss;
        [SerializeField] private Sprite battleSprite;
        [SerializeField] private Color battleTint = Color.white;
        [SerializeField] private List<SkillDefinition> diceSkills = new List<SkillDefinition>(6);

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public int BaseDicePoints => baseDicePoints;
        public int PassiveShieldPerTurn => passiveShieldPerTurn;
        public int PassiveReflectPercent => passiveReflectPercent;
        public int StartingArmor => 0;
        public int StartingRage => 0;
        public bool IsBoss => isBoss;
        public Sprite BattleSprite => battleSprite;
        public Color BattleTint => battleTint;
        public IReadOnlyList<SkillDefinition> DiceSkills => diceSkills;
    }
}
