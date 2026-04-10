using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceRogue
{
    public class SkillEffectExecutor
    {
        public void Execute(
            BattleSystem battleSystem,
            CombatantRuntimeState actor,
            IReadOnlyList<CombatantRuntimeState> allies,
            IReadOnlyList<CombatantRuntimeState> opponents,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (battleSystem == null || actor == null || face?.Skill == null || actionResult == null || report == null)
            {
                return;
            }

            switch (face.Skill.ActionType)
            {
                case SkillActionType.Attack:
                    ResolveAttackAction(actor, SelectOpponentTargets(battleSystem, opponents, face.Skill.TargetType), face, actionResult, report);
                    break;
                case SkillActionType.Defense:
                    ResolveDefenseAction(actor, face, actionResult, report);
                    break;
                case SkillActionType.Buff:
                    ResolveBuffAction(battleSystem, actor, allies, face, actionResult, report);
                    break;
                case SkillActionType.Debuff:
                    ResolveDebuffAction(actor, SelectOpponentTargets(battleSystem, opponents, face.Skill.TargetType), face, actionResult, report);
                    break;
            }
        }

        private static List<CombatantRuntimeState> SelectOpponentTargets(
            BattleSystem battleSystem,
            IReadOnlyList<CombatantRuntimeState> opponents,
            SkillTargetType targetType)
        {
            var aliveOpponents = opponents?
                .Where(target => target != null && target.IsAlive)
                .ToList() ?? new List<CombatantRuntimeState>();

            switch (targetType)
            {
                case SkillTargetType.AllEnemies:
                    return aliveOpponents;
                case SkillTargetType.HighHpEnemy:
                    return aliveOpponents.Count == 0
                        ? new List<CombatantRuntimeState>()
                        : new List<CombatantRuntimeState> { aliveOpponents.OrderByDescending(target => target.CurrentHp).First() };
                case SkillTargetType.RandomEnemy:
                default:
                    if (aliveOpponents.Count == 0)
                    {
                        return new List<CombatantRuntimeState>();
                    }

                    var randomIndex = battleSystem != null ? battleSystem.NextRandomIndex(aliveOpponents.Count) : 0;
                    return new List<CombatantRuntimeState> { aliveOpponents[randomIndex] };
            }
        }

        private static void ResolveAttackAction(
            CombatantRuntimeState actor,
            IReadOnlyList<CombatantRuntimeState> targets,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (targets == null || targets.Count == 0 || !actor.IsAlive)
            {
                return;
            }

            var activatedBerserk = false;
            var totalHpDamage = 0;
            var totalReflectedDamage = 0;

            if (face.RageCostValue > 0)
            {
                actor.SpendRage(face.RageCostValue);
                actionResult.RageSpend = face.RageCostValue;
            }

            if (face.SelfDamageValue > 0)
            {
                actor.LoseHpDirect(face.SelfDamageValue);
                actionResult.SelfDamage = face.SelfDamageValue;
                report.LogLines.Add($"{actor.DisplayName} suffers {face.SelfDamageValue} self damage.");
            }

            if (!actor.IsAlive)
            {
                return;
            }

            var attackDamage = face.AttackValue;
            if (face.ShieldDamagePercent > 0)
            {
                attackDamage += Mathf.FloorToInt(actor.GetShieldValue() * (face.ShieldDamagePercent / 100f));
            }

            attackDamage += actor.GetAttackBonus();

            if (face.Skill.ConsumeAllShield)
            {
                actionResult.ShieldConsumed = actor.GetShieldValue();
                actor.ConsumeAllShield();
            }

            for (var repeatIndex = 0; repeatIndex < face.RepeatCount && actor.IsAlive; repeatIndex++)
            {
                foreach (var target in targets)
                {
                    if (target == null || !target.IsAlive)
                    {
                        continue;
                    }

                    var resolution = target.TakeAttack(Mathf.Max(0, attackDamage));
                    totalHpDamage += resolution.HpDamage;

                    actionResult.Targets.Add(new BattleTargetResult
                    {
                        Target = target,
                        TargetType = BattleActionTargetType.Enemy,
                        RawDamage = resolution.RawDamage,
                        DamageAfterArmor = resolution.DamageAfterArmor,
                        ShieldBlocked = resolution.ShieldBlocked,
                        HpDamage = resolution.HpDamage,
                        WasDefeated = !target.IsAlive
                    });

                    report.LogLines.Add(
                        $"{actor.DisplayName} hits {target.DisplayName} for {resolution.RawDamage} -> armor {resolution.DamageAfterArmor}, shield {resolution.ShieldBlocked}, HP {resolution.HpDamage}.");

                    if (resolution.HpDamage > 0 && target.PassiveReflectPercent > 0)
                    {
                        var reflectedDamage = Mathf.FloorToInt(resolution.HpDamage * (target.PassiveReflectPercent / 100f));
                        if (reflectedDamage > 0)
                        {
                            actor.LoseHpDirect(reflectedDamage);
                            totalReflectedDamage += reflectedDamage;
                            report.LogLines.Add($"{target.DisplayName} reflects {reflectedDamage} damage to {actor.DisplayName}.");
                        }
                    }
                }
            }

            if (face.RageGainValue > 0)
            {
                activatedBerserk = actor.GainRage(face.RageGainValue);
                actionResult.RageGain = face.RageGainValue;
            }

            var totalLifestealPercent = face.LifestealPercent + actor.BerserkLifestealPercent;
            if (totalLifestealPercent > 0 && totalHpDamage > 0)
            {
                var healAmount = Mathf.FloorToInt(totalHpDamage * (totalLifestealPercent / 100f));
                if (healAmount > 0)
                {
                    actor.Heal(healAmount);
                    actionResult.HealAmount = healAmount;
                    report.LogLines.Add($"{actor.DisplayName} heals {healAmount} from lifesteal.");
                }
            }

            actionResult.ReflectedDamageTaken = totalReflectedDamage;
            actionResult.ActivatedBerserk = activatedBerserk;
        }

        private static void ResolveDefenseAction(
            CombatantRuntimeState actor,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (face.ShieldValue > 0)
            {
                actor.GainShield(face.ShieldValue);
                actionResult.ShieldGain = face.ShieldValue;
            }

            if (face.ArmorValue > 0)
            {
                actor.GainArmor(face.ArmorValue);
                actionResult.ArmorGain = face.ArmorValue;
            }

            if (face.NextTurnShieldValue > 0)
            {
                actor.QueueNextTurnShield(face.NextTurnShieldValue);
                actionResult.NextTurnShieldGain = face.NextTurnShieldValue;
            }

            actionResult.Targets.Add(new BattleTargetResult
            {
                Target = actor,
                TargetType = BattleActionTargetType.Self,
                ShieldGain = face.ShieldValue,
                ArmorGain = face.ArmorValue,
                NextTurnShieldGain = face.NextTurnShieldValue
            });

            report.LogLines.Add($"{actor.DisplayName} gains shield {face.ShieldValue}, armor {face.ArmorValue}, next-turn shield {face.NextTurnShieldValue}.");
        }

        private static void ResolveBuffAction(
            BattleSystem battleSystem,
            CombatantRuntimeState actor,
            IReadOnlyList<CombatantRuntimeState> allies,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (face.RageGainValue > 0)
            {
                actionResult.RageGain = face.RageGainValue;
                actionResult.ActivatedBerserk = actor.GainRage(face.RageGainValue);
            }

            if (face.AttackModifierValue != 0)
            {
                actor.ApplyNextTurnAttackModifier(face.AttackModifierValue);
                actionResult.AttackModifier = face.AttackModifierValue;
            }

            if (face.DicePointModifierValue != 0)
            {
                actor.ApplyNextTurnDicePointModifier(face.DicePointModifierValue);
                actionResult.DicePointModifier = face.DicePointModifierValue;
            }

            if (face.Skill.SummonTemplate != null && face.Skill.SummonCount > 0)
            {
                actionResult.SummonedCount = battleSystem.SummonEnemies(actor, face.Skill.SummonTemplate, face.Skill.SummonCount, face.Skill.MaxSummonedAllies, report);
            }

            if (face.Skill.SummonedAllyAttackBonusAmount != 0)
            {
                var buffedAllies = 0;
                foreach (var ally in allies)
                {
                    if (ally == null || !ally.IsAlive || !ally.IsSummoned)
                    {
                        continue;
                    }

                    ally.AddPersistentAttackBonus(face.Skill.SummonedAllyAttackBonusAmount);
                    buffedAllies += 1;

                    actionResult.Targets.Add(new BattleTargetResult
                    {
                        Target = ally,
                        TargetType = BattleActionTargetType.Ally,
                        AttackModifier = face.Skill.SummonedAllyAttackBonusAmount
                    });
                }

                if (buffedAllies > 0)
                {
                    actionResult.SummonedAllyAttackBonusGranted = face.Skill.SummonedAllyAttackBonusAmount;
                    report.LogLines.Add($"{actor.DisplayName} grants +{face.Skill.SummonedAllyAttackBonusAmount} attack to {buffedAllies} summoned allies.");
                }
            }

            actionResult.Targets.Add(new BattleTargetResult
            {
                Target = actor,
                TargetType = BattleActionTargetType.Self,
                RageGain = face.RageGainValue,
                AttackModifier = face.AttackModifierValue,
                DicePointModifier = face.DicePointModifierValue
            });

            if (face.RageGainValue > 0)
            {
                report.LogLines.Add($"{actor.DisplayName} gains rage {face.RageGainValue}.");
            }
        }

        private static void ResolveDebuffAction(
            CombatantRuntimeState actor,
            IReadOnlyList<CombatantRuntimeState> targets,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                if (face.AttackModifierValue != 0)
                {
                    target.ApplyNextTurnAttackModifier(face.AttackModifierValue);
                    actionResult.AttackModifier = face.AttackModifierValue;
                }

                if (face.DicePointModifierValue != 0)
                {
                    target.ApplyNextTurnDicePointModifier(face.DicePointModifierValue);
                    actionResult.DicePointModifier = face.DicePointModifierValue;
                }

                actionResult.Targets.Add(new BattleTargetResult
                {
                    Target = target,
                    TargetType = BattleActionTargetType.Enemy,
                    AttackModifier = face.AttackModifierValue,
                    DicePointModifier = face.DicePointModifierValue
                });

                report.LogLines.Add($"{actor.DisplayName} applies next-turn attack {face.AttackModifierValue} and DP {face.DicePointModifierValue} to {target.DisplayName}.");
            }
        }
    }
}
