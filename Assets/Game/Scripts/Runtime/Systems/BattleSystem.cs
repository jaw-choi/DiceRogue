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
            MaxTurns = Mathf.Max(1, maxTurns);
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
                report.LogLines.Add("Battle is already finished.");
                return report;
            }

            CurrentTurn += 1;
            report.TurnNumber = CurrentTurn;

            var playerFace = diceSystem.RollFace(Player);
            var enemyFace = diceSystem.RollFace(Enemy);
            report.PlayerFace = playerFace;
            report.EnemyFace = enemyFace;

            report.LogLines.Add($"Turn {CurrentTurn}");
            report.LogLines.Add($"{Player.DisplayName} rolled {playerFace.Skill.DisplayName}");
            report.LogLines.Add($"{Enemy.DisplayName} rolled {enemyFace.Skill.DisplayName}");

            ResolveFace(Player, Enemy, playerFace, report);
            if (Player.IsAlive && Enemy.IsAlive)
            {
                ResolveFace(Enemy, Player, enemyFace, report);
            }

            if (!Enemy.IsAlive)
            {
                BattleResult = BattleResultType.Victory;
            }
            else if (!Player.IsAlive)
            {
                BattleResult = BattleResultType.Defeat;
            }
            else if (CurrentTurn >= MaxTurns)
            {
                BattleResult = ResolveTimeout(report);
            }

            report.BattleResult = BattleResult;
            return report;
        }

        private void ResolveFace(CombatantRuntimeState actor, CombatantRuntimeState target, DiceFaceState face, BattleTurnReport report)
        {
            if (face?.Skill == null || !actor.IsAlive)
            {
                return;
            }

            switch (face.Skill.ActionType)
            {
                case SkillActionType.Attack:
                {
                    var rawDamage = face.AttackValue + actor.Rage;
                    var resolution = target.TakeAttack(rawDamage);
                    report.LogLines.Add(
                        $"{actor.DisplayName} attack {resolution.RawDamage} -> armor {target.Armor}, shield block {resolution.ShieldBlocked}, hp damage {resolution.HpDamage}");
                    break;
                }
                case SkillActionType.Guard:
                {
                    actor.GainShield(face.ShieldValue);
                    report.LogLines.Add($"{actor.DisplayName} gains {face.ShieldValue} shield.");
                    break;
                }
                case SkillActionType.Heal:
                {
                    actor.Heal(face.HealValue);
                    report.LogLines.Add($"{actor.DisplayName} heals {face.HealValue} hp.");
                    break;
                }
                case SkillActionType.Berserk:
                {
                    actor.LoseHpDirect(face.SelfDamageValue);
                    actor.GainRage(face.RageGainValue);
                    report.LogLines.Add($"{actor.DisplayName} loses {face.SelfDamageValue} hp and gains {face.RageGainValue} rage.");
                    break;
                }
            }
        }

        private BattleResultType ResolveTimeout(BattleTurnReport report)
        {
            var playerScore = Player.CurrentHp + Player.Shield + Player.Armor + Player.Rage;
            var enemyScore = Enemy.CurrentHp + Enemy.Shield + Enemy.Armor + Enemy.Rage;

            if (playerScore >= enemyScore)
            {
                Enemy.LoseHpDirect(Enemy.CurrentHp);
                report.LogLines.Add("Timeout: player wins by total combat score.");
                return BattleResultType.Victory;
            }

            Player.LoseHpDirect(Player.CurrentHp);
            report.LogLines.Add("Timeout: enemy wins by total combat score.");
            return BattleResultType.Defeat;
        }
    }
}
