using System;
using System.Collections.Generic;

namespace DiceRogue
{
    public enum BattleActionTargetType
    {
        None = 0,
        Self = 1,
        Enemy = 2,
        Ally = 3
    }

    [Serializable]
    public class BattleTargetResult
    {
        public CombatantRuntimeState Target;
        public BattleActionTargetType TargetType;
        public int RawDamage;
        public int DamageAfterArmor;
        public int ShieldBlocked;
        public int HpDamage;
        public int HealAmount;
        public int ShieldGain;
        public int ArmorGain;
        public int NextTurnShieldGain;
        public int RageGain;
        public int RageSpent;
        public int ReflectDamage;
        public int DicePointModifier;
        public int AttackModifier;
        public bool WasDefeated;
    }

    [Serializable]
    public class BattleActionResult
    {
        public CombatantRuntimeState Actor;
        public DiceFaceState Face;
        public string SkillName;
        public SkillActionType ActionType;
        public int ShieldGain;
        public int ArmorGain;
        public int NextTurnShieldGain;
        public int HealAmount;
        public int RageGain;
        public int RageSpend;
        public int SelfDamage;
        public int ReflectedDamageTaken;
        public int DicePointModifier;
        public int AttackModifier;
        public int BonusDicePointsGranted;
        public int ShieldConsumed;
        public int RepeatCount;
        public int SummonedCount;
        public int SummonedAllyAttackBonusGranted;
        public bool ActivatedBerserk;
        public bool ActorWasDefeated;
        public readonly List<BattleTargetResult> Targets = new List<BattleTargetResult>();
    }
}
