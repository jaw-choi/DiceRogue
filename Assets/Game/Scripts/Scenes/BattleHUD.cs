using System.Text;
using TMPro;
using UnityEngine;

namespace DiceRogue
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text turnInfoText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text enemyStatsText;
        [SerializeField] private TMP_Text currentDiceResultText;
        [SerializeField] private TMP_Text summaryText;

        public void Configure(
            TMP_Text runtimeTurnInfoText,
            TMP_Text runtimePlayerStatsText,
            TMP_Text runtimeEnemyStatsText,
            TMP_Text runtimeCurrentDiceResultText,
            TMP_Text runtimeSummaryText)
        {
            turnInfoText = runtimeTurnInfoText;
            playerStatsText = runtimePlayerStatsText;
            enemyStatsText = runtimeEnemyStatsText;
            currentDiceResultText = runtimeCurrentDiceResultText;
            summaryText = runtimeSummaryText;
        }

        public void Refresh(BattleSystem battleSystem, BattleTurnReport latestReport = null)
        {
            if (battleSystem == null)
            {
                return;
            }

            if (turnInfoText != null)
            {
                turnInfoText.text = battleSystem.MaxTurns > 0
                    ? $"턴 {battleSystem.CurrentTurn}/{battleSystem.MaxTurns}"
                    : $"턴 {battleSystem.CurrentTurn}";
            }

            if (playerStatsText != null && battleSystem.Player != null)
            {
                playerStatsText.text = battleSystem.Player.GetStatsText();
            }

            if (enemyStatsText != null && battleSystem.Enemy != null)
            {
                enemyStatsText.text = battleSystem.Enemy.GetStatsText();
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

        private static string BuildDiceResultText(BattleTurnReport latestReport)
        {
            if (latestReport == null)
            {
                return "이번 턴 주사위 결과\n아직 굴린 면이 없습니다.";
            }

            var builder = new StringBuilder();
            builder.AppendLine("이번 턴 주사위 결과");

            if (latestReport.PlayerFaces.Count > 0)
            {
                builder.Append("플레이어: ");
                AppendFaces(builder, latestReport.PlayerFaces);
                builder.AppendLine();
            }

            if (latestReport.EnemyFaces.Count > 0)
            {
                builder.Append("적: ");
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
    }
}
