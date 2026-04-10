using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceRogue
{
    public class BattleSystem
    {
        private readonly DiceSystem diceSystem;
        private readonly SkillEffectExecutor skillEffectExecutor;
        private readonly List<CombatantRuntimeState> enemies = new List<CombatantRuntimeState>(3);

        public BattleSystem(DiceSystem diceSystem)
        {
            this.diceSystem = diceSystem;
            skillEffectExecutor = new SkillEffectExecutor();
        }

        public CombatantRuntimeState Player { get; private set; }
        public IReadOnlyList<CombatantRuntimeState> Enemies => enemies;
        public CombatantRuntimeState Enemy => GetPrimaryEnemy();
        public int CurrentTurn { get; private set; }
        public int MaxTurns { get; private set; }
        public BattleResultType BattleResult { get; private set; } = BattleResultType.Ongoing;
        public bool IsFinished => BattleResult != BattleResultType.Ongoing;
        public bool EnableDebugLogging { get; set; } = true;

        public void BeginBattle(CombatantRuntimeState playerState, CombatantTemplate enemyTemplate, int maxTurns)
        {
            BeginBattle(playerState, enemyTemplate != null ? new[] { enemyTemplate } : System.Array.Empty<CombatantTemplate>(), maxTurns);
        }

        public void BeginBattle(CombatantRuntimeState playerState, EncounterDefinition encounterDefinition, int maxTurns)
        {
            BeginBattle(playerState, encounterDefinition?.EnemyTemplates ?? System.Array.Empty<CombatantTemplate>(), maxTurns);
        }

        public void BeginBattle(CombatantRuntimeState playerState, IReadOnlyList<CombatantTemplate> enemyTemplates, int maxTurns)
        {
            Player = playerState;
            enemies.Clear();
            MaxTurns = Mathf.Max(0, maxTurns);
            CurrentTurn = 0;
            BattleResult = BattleResultType.Ongoing;

            if (Player == null)
            {
                BattleResult = BattleResultType.Defeat;
                return;
            }

            Player.ResetForBattle(false);

            foreach (var enemyTemplate in enemyTemplates ?? System.Array.Empty<CombatantTemplate>())
            {
                if (enemyTemplate == null)
                {
                    continue;
                }

                var enemyState = new CombatantRuntimeState(enemyTemplate);
                enemyState.ResetForBattle(true);
                enemies.Add(enemyState);
            }

            if (enemies.Count == 0)
            {
                BattleResult = BattleResultType.Victory;
                LogLine("[Battle] No enemies were configured. Battle ends immediately.");
                return;
            }

            LogLine($"[Battle] {Player.DisplayName} vs {string.Join(", ", enemies.Select(enemy => enemy.DisplayName))} started.");
        }

        public BattleTurnReport ResolveNextTurn()
        {
            var report = new BattleTurnReport();

            if (IsFinished || Player == null)
            {
                report.BattleResult = BattleResult;
                report.LogLines.Add("Battle is already finished.");
                return report;
            }

            CurrentTurn += 1;
            report.TurnNumber = CurrentTurn;

            var turnOrder = BuildTurnOrder();
            var remainingActions = new Dictionary<CombatantRuntimeState, int>();
            var seenFaces = new Dictionary<CombatantRuntimeState, HashSet<int>>();

            Player.BeginTurn();
            foreach (var enemy in GetActiveEnemies())
            {
                enemy.BeginTurn();
            }

            report.LogLines.Add($"Turn {CurrentTurn}");
            report.LogLines.Add("Shield and Armor are reset at the start of the turn.");

            foreach (var combatant in turnOrder)
            {
                remainingActions[combatant] = combatant.GetTurnDicePoints(combatant.ConsumeTurnDicePointModifier());
                seenFaces[combatant] = new HashSet<int>();
                report.LogLines.Add($"{combatant.DisplayName} DP {remainingActions[combatant]}");
                report.TurnOrderEntries.Add($"{combatant.DisplayName} ({remainingActions[combatant]} DP)");

                if (ReferenceEquals(combatant, Player))
                {
                    report.PlayerTurnDicePoints = remainingActions[combatant];
                }

                if (combatant.IsBerserkActive)
                {
                    report.LogLines.Add($"{combatant.DisplayName} is Berserk.");
                }
            }

            while (Player.IsAlive && AreAnyEnemiesAlive() && HasRemainingActions(remainingActions))
            {
                foreach (var combatant in turnOrder)
                {
                    if (!Player.IsAlive || !AreAnyEnemiesAlive())
                    {
                        break;
                    }

                    if (!remainingActions.TryGetValue(combatant, out var actionsLeft) || actionsLeft <= 0 || !combatant.IsAlive)
                    {
                        continue;
                    }

                    remainingActions[combatant] = actionsLeft - 1;
                    remainingActions[combatant] += ResolveAction(combatant, report, seenFaces[combatant], ReferenceEquals(combatant, Player));
                }
            }

            Player.EndTurn();
            foreach (var enemy in enemies.Where(enemy => enemy != null && enemy.IsAlive))
            {
                enemy.EndTurn();
            }

            if (!Player.IsAlive)
            {
                BattleResult = BattleResultType.Defeat;
            }
            else if (!AreAnyEnemiesAlive())
            {
                BattleResult = BattleResultType.Victory;
            }

            if (IsFinished)
            {
                Player.ResetTemporaryCombatState();
                foreach (var enemy in enemies)
                {
                    enemy?.ResetTemporaryCombatState();
                }

                report.LogLines.Add(BattleResult == BattleResultType.Victory
                    ? $"{Player.DisplayName} wins the battle."
                    : $"{Player.DisplayName} is defeated.");
            }

            report.BattleResult = BattleResult;
            LogReport(report);
            return report;
        }

        public int SummonEnemies(
            CombatantRuntimeState summoner,
            CombatantTemplate summonTemplate,
            int summonCount,
            int maxSummonedAllies,
            BattleTurnReport report)
        {
            if (summoner == null || summonTemplate == null || summonCount <= 0 || ReferenceEquals(summoner, Player))
            {
                return 0;
            }

            var currentSummonCount = enemies.Count(enemy =>
                enemy != null &&
                enemy.IsAlive &&
                enemy.IsSummoned &&
                enemy.SummonerId == summoner.Template.Id);

            var allowedSummons = Mathf.Max(0, maxSummonedAllies - currentSummonCount);
            var actualSummons = Mathf.Min(Mathf.Max(1, summonCount), allowedSummons);

            for (var index = 0; index < actualSummons; index++)
            {
                var summonedEnemy = new CombatantRuntimeState(summonTemplate);
                summonedEnemy.MarkAsSummoned(summoner.Template.Id);
                summonedEnemy.ResetForBattle(true);
                summonedEnemy.MarkAsSummoned(summoner.Template.Id);
                enemies.Add(summonedEnemy);
            }

            if (actualSummons > 0)
            {
                report?.LogLines.Add($"{summoner.DisplayName} summons {summonTemplate.DisplayName} x{actualSummons}.");
            }

            return actualSummons;
        }

        public int NextRandomIndex(int count)
        {
            return diceSystem.NextIndex(count);
        }

        public BattleTurnReport ForceVictoryForDebug()
        {
            var report = new BattleTurnReport
            {
                TurnNumber = CurrentTurn,
                CurrentActingUnitName = Player != null ? Player.DisplayName : string.Empty,
                BattleResult = BattleResult
            };

            if (Player == null)
            {
                report.LogLines.Add("No active battle.");
                return report;
            }

            if (IsFinished)
            {
                report.LogLines.Add("Battle is already finished.");
                return report;
            }

            report.LogLines.Add("[Debug] Instant kill activated.");

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.LoseHpDirect(enemy.CurrentHp);
                report.LogLines.Add($"{enemy.DisplayName} was defeated instantly.");
            }

            if (!AreAnyEnemiesAlive())
            {
                BattleResult = BattleResultType.Victory;
                Player.ResetTemporaryCombatState();
                foreach (var enemy in enemies)
                {
                    enemy?.ResetTemporaryCombatState();
                }

                report.LogLines.Add($"{Player.DisplayName} wins the battle.");
            }

            report.BattleResult = BattleResult;
            LogReport(report);
            return report;
        }

        private int ResolveAction(
            CombatantRuntimeState actor,
            BattleTurnReport report,
            HashSet<int> seenFaces,
            bool isPlayerActor)
        {
            report.CurrentActingUnitName = actor != null ? actor.DisplayName : string.Empty;
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

            report.LogLines.Add($"{actor.DisplayName} uses {face.Skill.DisplayName}.");

            skillEffectExecutor.Execute(
                this,
                actor,
                GetAllies(actor),
                GetOpponents(actor),
                face,
                actionResult,
                report);

            if (isFirstFaceThisTurn && face.BonusDicePointsOnFirstRoll > 0)
            {
                actionResult.BonusDicePointsGranted = face.BonusDicePointsOnFirstRoll;
                report.LogLines.Add($"{actor.DisplayName} gains {face.BonusDicePointsOnFirstRoll} bonus DP from the first roll of this face.");
            }

            actionResult.ActorWasDefeated = !actor.IsAlive;
            report.ActionResults.Add(actionResult);
            return isFirstFaceThisTurn ? face.BonusDicePointsOnFirstRoll : 0;
        }

        private List<CombatantRuntimeState> BuildTurnOrder()
        {
            var turnOrder = new List<CombatantRuntimeState> { Player };
            turnOrder.AddRange(GetActiveEnemies());
            return turnOrder;
        }

        private IReadOnlyList<CombatantRuntimeState> GetAllies(CombatantRuntimeState actor)
        {
            if (ReferenceEquals(actor, Player))
            {
                return new[] { Player };
            }

            return enemies.Where(enemy => enemy != null && enemy.IsAlive && !ReferenceEquals(enemy, actor)).ToList();
        }

        private IReadOnlyList<CombatantRuntimeState> GetOpponents(CombatantRuntimeState actor)
        {
            if (ReferenceEquals(actor, Player))
            {
                return GetActiveEnemies().ToList();
            }

            return Player != null && Player.IsAlive
                ? new[] { Player }
                : System.Array.Empty<CombatantRuntimeState>();
        }

        private IEnumerable<CombatantRuntimeState> GetActiveEnemies()
        {
            return enemies.Where(enemy => enemy != null && enemy.IsAlive);
        }

        private bool AreAnyEnemiesAlive()
        {
            return enemies.Any(enemy => enemy != null && enemy.IsAlive);
        }

        private CombatantRuntimeState GetPrimaryEnemy()
        {
            return enemies.FirstOrDefault(enemy => enemy != null && enemy.IsAlive) ?? enemies.FirstOrDefault(enemy => enemy != null);
        }

        private static bool HasRemainingActions(Dictionary<CombatantRuntimeState, int> remainingActions)
        {
            foreach (var pair in remainingActions)
            {
                if (pair.Key != null && pair.Key.IsAlive && pair.Value > 0)
                {
                    return true;
                }
            }

            return false;
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

        private void LogReport(BattleTurnReport report)
        {
            if (!EnableDebugLogging || report == null || report.LogLines.Count == 0)
            {
                return;
            }

            Debug.Log($"[Battle Turn {report.TurnNumber}]\n{report.GetJoinedLog()}");
        }

        private void LogLine(string message)
        {
            if (!EnableDebugLogging || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Debug.Log(message);
        }
    }
}
