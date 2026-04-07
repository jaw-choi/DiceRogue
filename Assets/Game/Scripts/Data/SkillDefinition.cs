using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Skill Definition", fileName = "SkillDefinition")]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string id = "skill_id";
        [SerializeField] private string displayName = "Basic Attack";
        [SerializeField] private SkillActionType actionType = SkillActionType.Attack;
        [SerializeField] private int attackAmount = 2;
        [SerializeField] private int shieldAmount;
        [SerializeField] private int healAmount;
        [SerializeField] private int selfDamageAmount;
        [SerializeField] private int rageGainAmount;
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] [TextArea(2, 4)] private string description = "Deal damage.";

        public string Id => id;
        public string DisplayName => displayName;
        public SkillActionType ActionType => actionType;
        public int AttackAmount => attackAmount;
        public int ShieldAmount => shieldAmount;
        public int HealAmount => healAmount;
        public int SelfDamageAmount => selfDamageAmount;
        public int RageGainAmount => rageGainAmount;
        public Color AccentColor => accentColor;
        public string Description => description;

        public string GetSummary(int upgradeLevel = 0)
        {
            return actionType switch
            {
                SkillActionType.Attack => $"Damage {attackAmount + upgradeLevel}",
                SkillActionType.Guard => $"Shield {shieldAmount + upgradeLevel}",
                SkillActionType.Heal => $"Heal {healAmount + upgradeLevel}",
                SkillActionType.Berserk => $"Lose {selfDamageAmount}, Rage +{rageGainAmount + upgradeLevel}",
                _ => description
            };
        }
    }
}
