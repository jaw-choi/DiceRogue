using System.Text;
using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Skill Definition", fileName = "SkillDefinition")]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string id = "skill_id";
        [SerializeField] private string displayName = "Basic Attack";
        [SerializeField] private SkillActionType actionType = SkillActionType.Attack;
        [SerializeField] private SkillTargetType targetType = SkillTargetType.RandomEnemy;
        [SerializeField] private int attackAmount;
        [SerializeField] private int attackUpgradeAmount;
        [SerializeField] private int shieldAmount;
        [SerializeField] private int shieldUpgradeAmount;
        [SerializeField] private int armorAmount;
        [SerializeField] private int armorUpgradeAmount;
        [SerializeField] private int nextTurnShieldAmount;
        [SerializeField] private int nextTurnShieldUpgradeAmount;
        [SerializeField] private int selfDamageAmount;
        [SerializeField] private int selfDamageUpgradeAmount;
        [SerializeField] private int rageGainAmount;
        [SerializeField] private int rageGainUpgradeAmount;
        [SerializeField] private int rageCostAmount;
        [SerializeField] private int rageCostReductionPerUpgrade;
        [SerializeField] private int lifestealPercent;
        [SerializeField] private int lifestealUpgradePercent;
        [SerializeField] private int shieldDamagePercent;
        [SerializeField] private int shieldDamageUpgradePercent;
        [SerializeField] private int attackModifierAmount;
        [SerializeField] private int attackModifierUpgradeAmount;
        [SerializeField] private int dicePointModifierAmount;
        [SerializeField] private int dicePointModifierUpgradeAmount;
        [SerializeField] private int repeatCount = 1;
        [SerializeField] private int repeatCountUpgradeAmount;
        [SerializeField] private int bonusDicePointsOnFirstRoll;
        [SerializeField] private int bonusDicePointsUpgradeAmount;
        [SerializeField] private CombatantTemplate summonTemplate;
        [SerializeField] private int summonCount;
        [SerializeField] private int maxSummonedAllies;
        [SerializeField] private int summonedAllyAttackBonusAmount;
        [SerializeField] private bool consumeAllShield;
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] [TextArea(2, 4)] private string description = "Deal damage.";

        public string Id => id;
        public string DisplayName => displayName;
        public SkillActionType ActionType => actionType;
        public SkillTargetType TargetType => targetType;
        public int AttackAmount => attackAmount;
        public int ShieldAmount => shieldAmount;
        public int ArmorAmount => armorAmount;
        public int NextTurnShieldAmount => nextTurnShieldAmount;
        public int SelfDamageAmount => selfDamageAmount;
        public int RageGainAmount => rageGainAmount;
        public int RageCostAmount => rageCostAmount;
        public int LifestealPercent => lifestealPercent;
        public int ShieldDamagePercent => shieldDamagePercent;
        public int AttackModifierAmount => attackModifierAmount;
        public int DicePointModifierAmount => dicePointModifierAmount;
        public int RepeatCount => repeatCount;
        public int BonusDicePointsOnFirstRoll => bonusDicePointsOnFirstRoll;
        public CombatantTemplate SummonTemplate => summonTemplate;
        public int SummonCount => summonCount;
        public int MaxSummonedAllies => maxSummonedAllies;
        public int SummonedAllyAttackBonusAmount => summonedAllyAttackBonusAmount;
        public bool ConsumeAllShield => consumeAllShield;
        public Color AccentColor => accentColor;
        public string Description => description;

        public int GetAttackAmount(int upgradeLevel) => attackAmount + (attackUpgradeAmount * upgradeLevel);
        public int GetShieldAmount(int upgradeLevel) => shieldAmount + (shieldUpgradeAmount * upgradeLevel);
        public int GetArmorAmount(int upgradeLevel) => armorAmount + (armorUpgradeAmount * upgradeLevel);
        public int GetNextTurnShieldAmount(int upgradeLevel) => nextTurnShieldAmount + (nextTurnShieldUpgradeAmount * upgradeLevel);
        public int GetSelfDamageAmount(int upgradeLevel) => selfDamageAmount + (selfDamageUpgradeAmount * upgradeLevel);
        public int GetRageGainAmount(int upgradeLevel) => rageGainAmount + (rageGainUpgradeAmount * upgradeLevel);
        public int GetRageCostAmount(int upgradeLevel) => Mathf.Max(0, rageCostAmount - (rageCostReductionPerUpgrade * upgradeLevel));
        public int GetLifestealPercent(int upgradeLevel) => lifestealPercent + (lifestealUpgradePercent * upgradeLevel);
        public int GetShieldDamagePercent(int upgradeLevel) => shieldDamagePercent + (shieldDamageUpgradePercent * upgradeLevel);
        public int GetAttackModifierAmount(int upgradeLevel) => attackModifierAmount + (attackModifierUpgradeAmount * upgradeLevel);
        public int GetDicePointModifierAmount(int upgradeLevel) => dicePointModifierAmount + (dicePointModifierUpgradeAmount * upgradeLevel);
        public int GetRepeatCount(int upgradeLevel) => Mathf.Max(1, repeatCount + (repeatCountUpgradeAmount * upgradeLevel));
        public int GetBonusDicePointsOnFirstRoll(int upgradeLevel) => bonusDicePointsOnFirstRoll + (bonusDicePointsUpgradeAmount * upgradeLevel);

        public string GetSummary(int upgradeLevel = 0)
        {
            var builder = new StringBuilder();

            if (GetAttackAmount(upgradeLevel) > 0)
            {
                builder.Append($"Damage {GetAttackAmount(upgradeLevel)}");
            }

            if (GetShieldDamagePercent(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Shield ratio {GetShieldDamagePercent(upgradeLevel)}%");
            }

            if (GetShieldAmount(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Shield +{GetShieldAmount(upgradeLevel)}");
            }

            if (GetArmorAmount(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Armor +{GetArmorAmount(upgradeLevel)}");
            }

            if (GetNextTurnShieldAmount(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Next turn Shield +{GetNextTurnShieldAmount(upgradeLevel)}");
            }

            if (GetRageGainAmount(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Rage +{GetRageGainAmount(upgradeLevel)}");
            }

            if (GetRageCostAmount(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Spend Rage {GetRageCostAmount(upgradeLevel)}");
            }

            if (GetSelfDamageAmount(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Self HP -{GetSelfDamageAmount(upgradeLevel)}");
            }

            if (GetLifestealPercent(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"Lifesteal {GetLifestealPercent(upgradeLevel)}%");
            }

            if (GetAttackModifierAmount(upgradeLevel) != 0)
            {
                AppendWithSeparator(builder, $"Attack {(GetAttackModifierAmount(upgradeLevel) > 0 ? "+" : string.Empty)}{GetAttackModifierAmount(upgradeLevel)}");
            }

            if (GetDicePointModifierAmount(upgradeLevel) != 0)
            {
                AppendWithSeparator(builder, $"DP {(GetDicePointModifierAmount(upgradeLevel) > 0 ? "+" : string.Empty)}{GetDicePointModifierAmount(upgradeLevel)}");
            }

            if (GetRepeatCount(upgradeLevel) > 1)
            {
                AppendWithSeparator(builder, $"Repeat x{GetRepeatCount(upgradeLevel)}");
            }

            if (GetBonusDicePointsOnFirstRoll(upgradeLevel) > 0)
            {
                AppendWithSeparator(builder, $"First roll DP +{GetBonusDicePointsOnFirstRoll(upgradeLevel)}");
            }

            if (summonTemplate != null)
            {
                AppendWithSeparator(builder, $"Summon {summonTemplate.DisplayName} x{Mathf.Max(1, summonCount)}");
            }

            if (summonedAllyAttackBonusAmount != 0)
            {
                AppendWithSeparator(builder, $"Summoned ally Attack {(summonedAllyAttackBonusAmount > 0 ? "+" : string.Empty)}{summonedAllyAttackBonusAmount}");
            }

            if (consumeAllShield)
            {
                AppendWithSeparator(builder, "Consume all Shield");
            }

            return builder.Length > 0 ? builder.ToString() : description;
        }

        private static void AppendWithSeparator(StringBuilder builder, string text)
        {
            if (builder.Length > 0)
            {
                builder.Append(" / ");
            }

            builder.Append(text);
        }
    }
}
