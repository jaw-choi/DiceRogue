using System.Text;
using TMPro;
using UnityEngine;

namespace DiceRogue
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text turnInfoText;
        [SerializeField] private TMP_Text actingUnitText;
        [SerializeField] private TMP_Text turnQueueText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text enemyStatsText;
        [SerializeField] private TMP_Text currentDiceResultText;
        [SerializeField] private TMP_Text summaryText;

        public void Configure(
            TMP_Text runtimeTurnInfoText,
            TMP_Text runtimeActingUnitText,
            TMP_Text runtimeTurnQueueText,
            TMP_Text runtimePlayerStatsText,
            TMP_Text runtimeEnemyStatsText,
            TMP_Text runtimeCurrentDiceResultText,
            TMP_Text runtimeSummaryText)
        {
            turnInfoText = runtimeTurnInfoText;
            actingUnitText = runtimeActingUnitText;
            turnQueueText = runtimeTurnQueueText;
            playerStatsText = runtimePlayerStatsText;
            enemyStatsText = runtimeEnemyStatsText;
            currentDiceResultText = runtimeCurrentDiceResultText;
            summaryText = runtimeSummaryText;
        }

        public void Refresh(BattleSystem battleSystem, BattleTurnReport latestReport = null, string currentActorName = null)
        {
            if (battleSystem == null)
            {
                return;
            }

            if (turnInfoText != null)
            {
                turnInfoText.text = battleSystem.MaxTurns > 0
                    ? $"Turn {battleSystem.CurrentTurn}/{battleSystem.MaxTurns}"
                    : $"Turn {battleSystem.CurrentTurn}";
            }

            if (actingUnitText != null)
            {
                var actorLabel = !string.IsNullOrWhiteSpace(currentActorName)
                    ? currentActorName
                    : !string.IsNullOrWhiteSpace(latestReport?.CurrentActingUnitName)
                        ? latestReport.CurrentActingUnitName
                        : "Waiting";
                actingUnitText.text = $"Acting: {actorLabel}";
            }

            if (turnQueueText != null)
            {
                turnQueueText.text = BuildTurnQueueText(battleSystem, latestReport);
            }

            if (playerStatsText != null && battleSystem.Player != null)
            {
                playerStatsText.text = BuildPlayerStatsText(battleSystem.Player, latestReport);
            }

            if (enemyStatsText != null)
            {
                enemyStatsText.text = BuildEnemyStatsText(battleSystem);
            }

            if (currentDiceResultText != null)
            {
                currentDiceResultText.text = BuildDiceResultText(latestReport);
            }

            if (summaryText != null && latestReport != null)
            {
                summaryText.text = latestReport.GetJoinedLog();
            }
        }

        public void SetSummary(string text)
        {
            if (summaryText != null)
            {
                summaryText.text = text;
            }
        }

        private static string BuildPlayerStatsText(CombatantRuntimeState player, BattleTurnReport latestReport)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Player");
            builder.AppendLine($"HP {player.CurrentHp}/{player.MaxHp}");
            builder.AppendLine($"Shield {player.Shield}");
            builder.AppendLine($"Armor {player.Armor}");
            builder.AppendLine($"Rage {player.Rage}");
            builder.AppendLine(player.IsBerserkActive ? $"Berserk Active ({player.BerserkTurnsRemaining}t)" : "Berserk Inactive");
            builder.Append($"Current DP {Mathf.Max(0, latestReport?.PlayerTurnDicePoints ?? player.BaseDicePoints)}");
            return builder.ToString();
        }

        private static string BuildTurnQueueText(BattleSystem battleSystem, BattleTurnReport latestReport)
        {
            if (latestReport != null && latestReport.TurnOrderEntries.Count > 0)
            {
                return $"Turn Queue\n{string.Join(" -> ", latestReport.TurnOrderEntries)}";
            }

            if (battleSystem == null)
            {
                return "Turn Queue\nNo units.";
            }

            var builder = new StringBuilder();
            builder.Append("Turn Queue\n");
            builder.Append("Player");
            for (var index = 0; index < battleSystem.Enemies.Count; index++)
            {
                if (battleSystem.Enemies[index] != null && battleSystem.Enemies[index].IsAlive)
                {
                    builder.Append(" -> ");
                    builder.Append(battleSystem.Enemies[index].DisplayName);
                }
            }

            return builder.ToString();
        }

        private static string BuildDiceResultText(BattleTurnReport latestReport)
        {
            if (latestReport == null)
            {
                return "Roll Log\nNo actions resolved yet.";
            }

            var builder = new StringBuilder();
            builder.AppendLine("Roll Log");

            if (latestReport.PlayerFaces.Count > 0)
            {
                builder.Append("Player: ");
                AppendFaces(builder, latestReport.PlayerFaces);
                builder.AppendLine();
            }

            if (latestReport.EnemyFaces.Count > 0)
            {
                builder.Append("Enemies: ");
                AppendFaces(builder, latestReport.EnemyFaces);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static void AppendFaces(StringBuilder builder, System.Collections.Generic.IReadOnlyList<DiceFaceState> faces)
        {
            for (var index = 0; index < faces.Count; index++)
            {
                if (faces[index]?.Skill == null)
                {
                    continue;
                }

                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(faces[index].Skill.DisplayName);
            }
        }

        private static string BuildEnemyStatsText(BattleSystem battleSystem)
        {
            if (battleSystem == null || battleSystem.Enemies == null || battleSystem.Enemies.Count == 0)
            {
                return "Enemies\nNo enemies";
            }

            var builder = new StringBuilder();
            builder.AppendLine("Enemies");

            for (var index = 0; index < battleSystem.Enemies.Count; index++)
            {
                var enemy = battleSystem.Enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                builder.AppendLine($"{enemy.DisplayName}  HP {enemy.CurrentHp}/{enemy.MaxHp}");
                builder.AppendLine($"Shield {enemy.Shield} | Armor {enemy.Armor} | Rage {enemy.Rage}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}
