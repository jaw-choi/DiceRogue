using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DiceRogue
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private UIStateController stateController;
        [SerializeField] private string battleStateId = "Battle";
        [SerializeField] private string resultStateId = "Result";
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text enemyStatsText;
        [SerializeField] private TMP_Text playerDiceText;
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Toggle autoBattleToggle;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backToMenuButton;

        private GameRunManager runManager;
        private Coroutine autoBattleRoutine;
        private readonly StringBuilder logBuilder = new StringBuilder();

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();

            if (runManager.BattleSystem == null)
            {
                var availableNodes = runManager.MapSystem.GetAvailableNodes();
                if (availableNodes.Count > 0)
                {
                    runManager.PrepareBattle(availableNodes[0].Index);
                }
            }

            if (autoBattleToggle != null)
            {
                autoBattleToggle.isOn = runManager.AutoBattleEnabled;
                autoBattleToggle.onValueChanged.AddListener(OnAutoToggleChanged);
            }

            if (rollButton != null)
            {
                rollButton.onClick.AddListener(ResolveOneTurn);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(runManager.CompleteBattleAndAdvance);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(runManager.Config.MainMenuSceneName));
            }
        }

        private void OnEnable()
        {
            stateController?.Show(battleStateId);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(runManager.BattleSystem != null && runManager.BattleSystem.IsFinished);
            }
            Render();
            RefreshAutoFlow();
        }

        private void OnDisable()
        {
            if (autoBattleRoutine != null)
            {
                StopCoroutine(autoBattleRoutine);
                autoBattleRoutine = null;
            }
        }

        private void OnAutoToggleChanged(bool isOn)
        {
            runManager.AutoBattleEnabled = isOn;
            RefreshAutoFlow();
        }

        private void RefreshAutoFlow()
        {
            if (rollButton != null)
            {
                rollButton.interactable = !runManager.AutoBattleEnabled && !runManager.BattleSystem.IsFinished;
            }

            if (runManager.AutoBattleEnabled && !runManager.BattleSystem.IsFinished)
            {
                if (autoBattleRoutine == null)
                {
                    autoBattleRoutine = StartCoroutine(AutoResolveRoutine());
                }
            }
            else if (autoBattleRoutine != null)
            {
                StopCoroutine(autoBattleRoutine);
                autoBattleRoutine = null;
            }
        }

        private IEnumerator AutoResolveRoutine()
        {
            while (!runManager.BattleSystem.IsFinished && runManager.AutoBattleEnabled)
            {
                ResolveOneTurn();
                yield return new WaitForSeconds(runManager.Config.AutoTurnDelay);
            }

            autoBattleRoutine = null;
        }

        private void ResolveOneTurn()
        {
            if (runManager.BattleSystem.IsFinished)
            {
                return;
            }

            var report = runManager.BattleSystem.ResolveNextTurn();
            if (logBuilder.Length > 0)
            {
                logBuilder.AppendLine();
                logBuilder.AppendLine("----------------");
            }

            logBuilder.Append(report.GetJoinedLog());
            Render();

            if (runManager.BattleSystem.IsFinished)
            {
                stateController?.Show(resultStateId);

                if (resultText != null)
                {
                    resultText.text = runManager.BattleSystem.BattleResult == BattleResultType.Victory
                        ? "Victory. Continue to reward or result flow."
                        : "Defeat. Return to the main menu.";
                }

                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                }
            }
        }

        private void Render()
        {
            var battleSystem = runManager.BattleSystem;
            if (battleSystem == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = $"Battle Scene / {battleSystem.Player.DisplayName} vs {battleSystem.Enemy.DisplayName}";
            }

            if (turnText != null)
            {
                turnText.text = $"Turn {battleSystem.CurrentTurn}/{battleSystem.MaxTurns} | Auto: {runManager.AutoBattleEnabled}";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = battleSystem.Player.GetStatsText();
            }

            if (enemyStatsText != null)
            {
                enemyStatsText.text = battleSystem.Enemy.GetStatsText();
            }

            if (playerDiceText != null)
            {
                playerDiceText.text = $"Player Dice\n{battleSystem.Player.GetDiceText()}";
            }

            if (battleLogText != null)
            {
                battleLogText.text = logBuilder.Length == 0 ? "No turns resolved yet." : logBuilder.ToString();
            }
        }
    }
}
