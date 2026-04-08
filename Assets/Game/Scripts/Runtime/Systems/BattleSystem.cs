using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    public class BattleSystem
    {
        private readonly DiceSystem diceSystem;

        public BattleSystem(DiceSystem diceSystem)
        {
            this.diceSystem = diceSystem;
        }

        public CombatantRuntimeState Player { get; private set; }
        public CombatantRuntimeState Enemy { get; private set; }
        public int CurrentTurn { get; private set; }
        public int MaxTurns { get; private set; }
        public BattleResultType BattleResult { get; private set; } = BattleResultType.Ongoing;
        public bool IsFinished => BattleResult != BattleResultType.Ongoing;

        public void BeginBattle(CombatantRuntimeState playerState, CombatantTemplate enemyTemplate, int maxTurns)
        {
            Player = playerState;
            Enemy = new CombatantRuntimeState(enemyTemplate);
            MaxTurns = 0;
            CurrentTurn = 0;
            BattleResult = BattleResultType.Ongoing;

            Player.ResetForBattle(false);
            Enemy.ResetForBattle(true);
        }

        public BattleTurnReport ResolveNextTurn()
        {
            var report = new BattleTurnReport();

            if (IsFinished || Player == null || Enemy == null)
            {
                report.BattleResult = BattleResult;
                report.LogLines.Add("전투가 이미 종료되었습니다.");
                return report;
            }

            CurrentTurn += 1;
            report.TurnNumber = CurrentTurn;

            Player.BeginTurn();
            Enemy.BeginTurn();

            report.LogLines.Add($"턴 {CurrentTurn}");
            report.LogLines.Add("턴 시작과 함께 방어도와 방어력이 초기화되었습니다.");

            if (Player.IsBerserkActive)
            {
                report.LogLines.Add($"{Player.DisplayName}이(가) 광분 상태입니다.");
            }

            if (Enemy.IsBerserkActive)
            {
                report.LogLines.Add($"{Enemy.DisplayName}이(가) 광분 상태입니다.");
            }

            var playerRemainingActions = Player.GetTurnDicePoints(Player.ConsumeTurnDicePointModifier());
            var enemyRemainingActions = Enemy.GetTurnDicePoints(Enemy.ConsumeTurnDicePointModifier());
            var playerSeenFaces = new HashSet<int>();
            var enemySeenFaces = new HashSet<int>();

            while ((playerRemainingActions > 0 || enemyRemainingActions > 0) && Player.IsAlive && Enemy.IsAlive)
            {
                if (playerRemainingActions > 0 && Player.IsAlive && Enemy.IsAlive)
                {
                    playerRemainingActions -= 1;
                    playerRemainingActions += ResolveAction(Player, Enemy, report, playerSeenFaces, true);
                }

                if (enemyRemainingActions > 0 && Player.IsAlive && Enemy.IsAlive)
                {
                    enemyRemainingActions -= 1;
                    enemyRemainingActions += ResolveAction(Enemy, Player, report, enemySeenFaces, false);
                }
            }

            Player.EndTurn();
            Enemy.EndTurn();

            if (!Enemy.IsAlive)
            {
                BattleResult = BattleResultType.Victory;
            }
            else if (!Player.IsAlive)
            {
                BattleResult = BattleResultType.Defeat;
            }

            report.BattleResult = BattleResult;
            return report;
        }

        private int ResolveAction(
            CombatantRuntimeState actor,
            CombatantRuntimeState target,
            BattleTurnReport report,
            HashSet<int> seenFaces,
            bool isPlayerActor)
        {
            var face = diceSystem.RollFace(actor);
            if (face?.Skill == null || !actor.IsAlive)
            {
                return 0;
            }

            if (isPlayerActor)
            {
                report.PlayerFace ??= face;
                report.PlayerFaces.Add(face);
            }
            else
            {
                report.EnemyFace ??= face;
                report.EnemyFaces.Add(face);
            }

            var faceIndex = FindFaceIndex(actor, face);
            var isFirstFaceThisTurn = seenFaces.Add(faceIndex);
            var actionResult = new BattleActionResult
            {
                Actor = actor,
                Face = face,
                SkillName = face.Skill.DisplayName,
                ActionType = face.Skill.ActionType,
                RepeatCount = face.RepeatCount
            };

            report.LogLines.Add($"{actor.DisplayName}이(가) {face.Skill.DisplayName}을(를) 사용했습니다.");

            switch (face.Skill.ActionType)
            {
                case SkillActionType.Attack:
                    ResolveAttackAction(actor, target, face, actionResult, report);
                    break;
                case SkillActionType.Defense:
                    ResolveDefenseAction(actor, face, actionResult, report);
                    break;
                case SkillActionType.Buff:
                    ResolveBuffAction(actor, face, actionResult, report);
                    break;
                case SkillActionType.Debuff:
                    ResolveDebuffAction(actor, target, face, actionResult, report);
                    break;
            }

            if (isFirstFaceThisTurn && face.BonusDicePointsOnFirstRoll > 0)
            {
                actionResult.BonusDicePointsGranted = face.BonusDicePointsOnFirstRoll;
                report.LogLines.Add($"{actor.DisplayName}이(가) 처음 나온 면 보너스로 DP {face.BonusDicePointsOnFirstRoll}을(를) 얻었습니다.");
            }

            actionResult.ActorWasDefeated = !actor.IsAlive;
            report.ActionResults.Add(actionResult);
            return isFirstFaceThisTurn ? face.BonusDicePointsOnFirstRoll : 0;
        }

        private void ResolveAttackAction(
            CombatantRuntimeState actor,
            CombatantRuntimeState target,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (target == null || !target.IsAlive)
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

            for (var repeatIndex = 0; repeatIndex < face.RepeatCount && actor.IsAlive && target.IsAlive; repeatIndex++)
            {
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
                    $"{actor.DisplayName}의 공격 {resolution.RawDamage} → 방어력 적용 후 {resolution.DamageAfterArmor}, 방어도 {resolution.ShieldBlocked}, HP {resolution.HpDamage}.");

                if (resolution.HpDamage > 0 && target.PassiveReflectPercent > 0)
                {
                    var reflectedDamage = Mathf.FloorToInt(resolution.HpDamage * (target.PassiveReflectPercent / 100f));
                    if (reflectedDamage > 0)
                    {
                        actor.LoseHpDirect(reflectedDamage);
                        totalReflectedDamage += reflectedDamage;
                        report.LogLines.Add($"{target.DisplayName}이(가) 피해 {reflectedDamage}를 반사했습니다.");
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
                    report.LogLines.Add($"{actor.DisplayName}이(가) 흡혈로 {healAmount} 회복했습니다.");
                }
            }

            actionResult.ReflectedDamageTaken = totalReflectedDamage;
            actionResult.ActivatedBerserk = activatedBerserk;
        }

        private void ResolveDefenseAction(
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

            report.LogLines.Add(
                $"{actor.DisplayName}이(가) 방어도 {face.ShieldValue}, 방어력 {face.ArmorValue}, 다음 턴 방어도 {face.NextTurnShieldValue}을(를) 얻었습니다.");
        }

        private void ResolveBuffAction(
            CombatantRuntimeState actor,
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

            actionResult.Targets.Add(new BattleTargetResult
            {
                Target = actor,
                TargetType = BattleActionTargetType.Self,
                RageGain = face.RageGainValue,
                AttackModifier = face.AttackModifierValue,
                DicePointModifier = face.DicePointModifierValue
            });

            report.LogLines.Add($"{actor.DisplayName}이(가) 분노 {face.RageGainValue}을(를) 얻었습니다.");
        }

        private void ResolveDebuffAction(
            CombatantRuntimeState actor,
            CombatantRuntimeState target,
            DiceFaceState face,
            BattleActionResult actionResult,
            BattleTurnReport report)
        {
            if (target == null || !target.IsAlive)
            {
                return;
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

            report.LogLines.Add(
                $"{actor.DisplayName}이(가) {target.DisplayName}에게 다음 턴 공격 {face.AttackModifierValue}, DP {face.DicePointModifierValue} 효과를 남겼습니다.");
        }

        private static int FindFaceIndex(CombatantRuntimeState actor, DiceFaceState face)
        {
            for (var index = 0; index < actor.DiceFaces.Count; index++)
            {
                if (ReferenceEquals(actor.DiceFaces[index], face))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
